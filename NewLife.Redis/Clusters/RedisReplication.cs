using NewLife.Log;
using NewLife.Net;
using NewLife.Threading;

namespace NewLife.Caching.Clusters;

/// <summary>Redis主从复制</summary>
public class RedisReplication : RedisBase, IRedisCluster, IDisposable
{
    #region 属性
    /// <summary>节点集合</summary>
    IList<IRedisNode> IRedisCluster.Nodes => Nodes.Select(x => (IRedisNode)x).ToList();

    /// <summary>节点改变事件</summary>
    public event EventHandler? NodeChanged;

    /// <summary>集群节点</summary>
    public RedisNode[]? Nodes { get; protected set; }

    /// <summary>主从信息</summary>
    public ReplicationInfo? Replication { get; protected set; }

    /// <summary>是否根据解析得到的节点列表去设置外部Redis的节点地址</summary>
    public Boolean SetHostServer { get; set; }

    private TimerX? _timer;
    private ICache _cache = new MemoryCache();
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    public RedisReplication(Redis redis) : base(redis, null!) { }

    /// <summary>销毁</summary>
    public void Dispose() => _timer.TryDispose();
    #endregion

    #region 方法
    /// <summary>开始监控节点</summary>
    public void StartMonitor()
    {
        //GetNodes();
        // 先填充已有地址，供外部使用
        var servers = Redis.GetServices().ToList();
        var list = new List<RedisNode>();
        foreach (var server in servers)
        {
            list.Add(new RedisNode
            {
                Owner = Redis,
                EndPoint = server.ToString(),
            });
        }
        Nodes = list.ToArray();

        if (SetHostServer)
        {
            // 异步刷新节点信息，最多等待100秒
            var task = Task.Run(GetNodes);
            task.Wait(100);
        }

        // 定时刷新集群节点列表
        _timer ??= new TimerX(s => GetNodes(), null, 60_000, 60_000) { Async = true };
    }

    private Int32 _initNodes;
    /// <summary>分析主从节点</summary>
    public virtual IList<RedisNode> GetNodes()
    {
        var showLog = _initNodes++ == 0;
        if (showLog) WriteLog("分析[{0}]主从节点：", Redis.Name);

        // 可能配置了多个地址，主从混合，需要探索式查找
        var servers = Redis.GetServices().ToList();
        var (reps, nodes) = GetReplications(Redis, servers);
        if (reps != null && reps.Count > 0) Replication = reps[0];

        SetNodes(nodes);

        return nodes;
    }

    private String? _lastNodes;
    /// <summary>设置节点</summary>
    /// <param name="nodes"></param>
    protected void SetNodes(IList<RedisNode> nodes)
    {
        var showLog = _initNodes++ <= 1;

        // 排序，master优先
        nodes = nodes.OrderBy(e => e.Slave).ThenBy(e => e.EndPoint).ToList();
        Nodes = nodes.ToArray();

        if (SetHostServer)
        {
            var uris = new List<NetUri>();
            foreach (var node in nodes)
            {
                if (node.EndPoint.IsNullOrEmpty()) return;

                var uri = new NetUri(node.EndPoint) { Type = NetType.Tcp };
                if (uri.Port == 0) uri.Port = 6379;
                uris.Add(uri);
            }
            if (uris.Count > 0) Redis.SetSevices(uris.ToArray());
        }

        var changed = false;
        var str = nodes.Join("\n", e => $"{e.EndPoint}-{e.Slave}");
        if (_lastNodes != str)
        {
            WriteLog("得到[{0}]主从节点：", Redis.Name);
            showLog = true;
            changed = true;
            _lastNodes = str;
        }
        foreach (var node in nodes)
        {
            if (showLog) WriteLog("节点：{0} {1}", node.Slave ? "slave" : "master", node.EndPoint);
        }

        if (changed) NodeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>探索指定一批地址的主从复制信息</summary>
    /// <param name="redis"></param>
    /// <param name="servers"></param>
    /// <returns></returns>
    public (IList<ReplicationInfo>, IList<RedisNode>) GetReplications(Redis redis, IList<NetUri> servers)
    {
        // 可能配置了多个地址，主从混合，需要探索式查找
        var hash = servers.Select(e => e.EndPoint + "").ToList();
        var reps = new List<ReplicationInfo>();
        var nodes = new List<RedisNode>();
        for (var i = 0; i < servers.Count; i++)
        {
            var svr = servers[i];
            var (rep, list) = GetReplication(redis, svr);

            if (rep != null) reps.Add(rep);

            if (list != null)
            {
                // 合并列表
                foreach (var item in list)
                {
                    if (!nodes.Any(e => e.EndPoint == item.EndPoint)) nodes.Add(item);

                    // 加入探索列表
                    if (!hash.Contains(item.EndPoint))
                    {
                        hash.Add(item.EndPoint);

                        var uri = new NetUri(item.EndPoint) { Type = NetType.Tcp };
                        if (uri.Port == 0) uri.Port = 6379;
                        servers.Add(uri);
                    }
                }
            }
        }

        // 排序，master优先
        nodes = nodes.OrderBy(e => e.Slave).ThenBy(e => e.EndPoint).ToList();
        reps = reps.OrderBy(e => e.Role != "master").ThenBy(e => e.EndPoint).ToList();

        return (reps, nodes);
    }

    /// <summary>探索指定地址的主从复制信息</summary>
    /// <param name="redis"></param>
    /// <param name="server"></param>
    /// <returns></returns>
    public (ReplicationInfo?, IList<RedisNode>?) GetReplication(Redis redis, NetUri server)
    {
        using var span = redis.Tracer?.NewSpan(nameof(GetReplication), server);

        // 屏蔽中
        var key = $"rep:{server.Address}-{server.Port}";
        var repNode = _cache.Get<RedisNode>(key);
        if (repNode != null && repNode.NextTime > DateTime.Now) return (null, null);

        var rs = "";
        try
        {
            using var client = new RedisClient(redis, server)
            {
                Name = $"{server.Address}-{server.Port}",
                Timeout = 5_000,
                Log = redis.ClientLog,
            };
            rs = client.Execute<String>("INFO", "Replication");
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteLine("探索[{0}]异常 {1}", server.EndPoint, ex.Message);

            // 目前节点不可达，可能是内网节点，屏蔽一段时间
            repNode ??= new RedisNode { EndPoint = server.EndPoint + "" };
            repNode.Error++;

            // 指数级增加屏蔽时间
            var exp = 60 * (1 << repNode.Error);
            if (exp > 60 * 60 * 4) exp = 60 * 60 * 4;
            repNode.NextTime = DateTime.Now.AddSeconds(exp);

            _cache.Add(key, repNode, exp);
        }
        if (rs.IsNullOrEmpty()) return (null, null);

        var inf = rs.SplitAsDictionary(":", "\r\n");
        var rep = new ReplicationInfo();
        rep.Load(inf);

        var list = new List<RedisNode>();
        if (rep.Masters != null)
        {
            foreach (var item in rep.Masters)
            {
                if (item.EndPoint.IsNullOrEmpty() || list.Any(e => e.EndPoint == item.EndPoint)) continue;

                var node = new RedisNode
                {
                    Owner = redis,
                    EndPoint = item.EndPoint,
                    Slave = false,
                };
                list.Add(node);
            }
        }
        if (rep.Slaves != null)
        {
            foreach (var item in rep.Slaves)
            {
                if (item.EndPoint.IsNullOrEmpty() || list.Any(e => e.EndPoint == item.EndPoint)) continue;

                var node = new RedisNode
                {
                    Owner = redis,
                    EndPoint = item.EndPoint,
                    Slave = true,
                };
                list.Add(node);
            }
        }

        // Master节点
        if (!rep.MasterHost.IsNullOrEmpty() && !rep.EndPoint.IsNullOrEmpty())
        {
            var node = new RedisNode
            {
                Owner = redis,
                EndPoint = rep.EndPoint,
                Slave = false,
            };

            if (!list.Any(e => e.EndPoint == node.EndPoint)) list.Insert(0, node);
        }

        // 当前节点
        {
            var node = new RedisNode
            {
                Owner = redis,
                EndPoint = server.EndPoint + "",
                Slave = rep.Role != "master",
            };

            if (!list.Any(e => e.EndPoint == node.EndPoint)) list.Insert(0, node);
        }

        return (rep, list);
    }

    /// <summary>根据Key选择节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <returns></returns>
    public virtual IRedisNode? SelectNode(String key, Boolean write)
    {
        if (key.IsNullOrEmpty()) return null;

        // 选择有效节点，剔除被屏蔽节点和未连接节点
        var now = DateTime.Now;
        var ns = Nodes?.ToArray();

        if (ns != null && ns.Length != 0)
        {
            // 找主节点
            foreach (var node in ns)
            {
                if (!node.Slave && node.NextTime < now) return node;
            }

            if (!write)
            {
                // 找从节点
                foreach (var node in ns)
                {
                    if (node.NextTime < now) return node;
                }
            }

            // 无视屏蔽情况，再来一次
            foreach (var node in ns)
            {
                if (!node.Slave) return node;
            }

            if (!write)
            {
                // 找从节点
                return ns[0];
            }
        }

        throw new XException($"主从[{Redis.Name}]没有可用节点（key={key}）");
        //return null;
    }

    /// <summary>根据异常重选节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <param name="node"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public virtual IRedisNode ReselectNode(String key, Boolean write, IRedisNode node, Exception exception)
    {
        using var span = Redis.Tracer?.NewSpan("redis:ReselectNode", new { key, (node as RedisNode)?.EndPoint });

        // 屏蔽旧节点一段时间
        var now = DateTime.Now;

        // 读取指令网络异常时，换一个节点
        if (Redis.NoDelay(exception))
        {
            // 屏蔽旧节点一段时间
            if (node is RedisNode redisNode && ++redisNode.Error >= Redis.Retry)
            {
                redisNode.NextTime = now.AddSeconds(Redis.ShieldingTime);
                var msg = $"屏蔽 {redisNode.EndPoint} 到 {redisNode.NextTime.ToFullString()}";
                span?.AppendTag(msg);
                Redis.WriteLog(msg);
            }
        }

        return SelectNode(key, write);
    }

    /// <summary>重置节点。设置成功状态</summary>
    /// <param name="node"></param>
    public virtual void ResetNode(IRedisNode node)
    {
        if (node is RedisNode redisNode) redisNode.Error = 0;
    }
    #endregion

    #region 辅助
    /// <summary>日志</summary>
    public ILog Log { get; set; } = XTrace.Log;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}