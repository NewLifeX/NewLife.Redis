using System.Net.Sockets;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Caching.Clusters;

/// <summary>Redis主从复制</summary>
public class RedisReplication : RedisBase, IRedisCluster, IDisposable
{
    #region 属性
    /// <summary>集群节点</summary>
    public RedisNode[] Nodes { get; protected set; }

    /// <summary>主从信息</summary>
    public ReplicationInfo Replication { get; protected set; }

    private TimerX _timer;
    private Int32 _idx;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    public RedisReplication(Redis redis) : base(redis, null) { }

    /// <summary>销毁</summary>
    public void Dispose() => _timer.TryDispose();
    #endregion

    #region 方法
    /// <summary>开始监控节点</summary>
    public void StartMonitor()
    {
        GetNodes();

        // 定时刷新集群节点列表
        if (Nodes != null) _timer = new TimerX(s => GetNodes(), null, 60_000, 60_000) { Async = true };
    }

    private String _lastNodes;
    /// <summary>分析主从节点</summary>
    public virtual void GetNodes()
    {
        var showLog = Nodes == null;
        if (showLog) XTrace.WriteLine("分析[{0}]主从节点：", Redis?.Name);

        // 可能配置了多个地址，主从混合，需要探索式查找
        var servers = Redis.GetServers().ToList();
        var (reps, nodes) = GetReplications(Redis, servers);
        if (reps != null && reps.Count > 0) Replication = reps[0];

        // 排序，master优先
        nodes = nodes.OrderBy(e => e.Slave).ThenBy(e => e.EndPoint).ToList();
        Nodes = nodes.ToArray();

        var str = nodes.Join("\n", e => $"{e.EndPoint}-{e.Slave}");
        if (_lastNodes != str)
        {
            if (!showLog) XTrace.WriteLine("分析[{0}]主从节点：", Redis?.Name);
            showLog = true;
            _lastNodes = str;
        }
        foreach (var node in nodes)
        {
            if (showLog) XTrace.WriteLine("节点：{0} {1}", node.Slave ? "slave" : "master", node.EndPoint);
        }
    }

    /// <summary>探索指定一批地址的主从复制信息</summary>
    /// <param name="redis"></param>
    /// <param name="servers"></param>
    /// <returns></returns>
    public static (IList<ReplicationInfo>, IList<RedisNode>) GetReplications(Redis redis, IList<NetUri> servers)
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

        return (reps, nodes);
    }

    /// <summary>探索指定地址的主从复制信息</summary>
    /// <param name="redis"></param>
    /// <param name="server"></param>
    /// <returns></returns>
    public static (ReplicationInfo, IList<RedisNode>) GetReplication(Redis redis, NetUri server)
    {
        using var span = redis.Tracer?.NewSpan(nameof(GetReplication), server);

        // 从第一个连接获取一次信息，仅支持一主一从或一主多从的情况
        //var rs = redis.Execute(r => r.Execute<String>("INFO", "Replication"));
        var rs = "";
        try
        {
            using var client = new RedisClient(redis, server) { Timeout = 5_000 };
            rs = client.Execute<String>("INFO", "Replication");
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteLine("探索[{0}]异常 {1}", server.EndPoint, ex.Message);
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
                if (item.IP.IsNullOrEmpty() || list.Any(e => e.EndPoint == item.EndPoint)) continue;

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
                if (item.IP.IsNullOrEmpty() || list.Any(e => e.EndPoint == item.EndPoint)) continue;

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
        if (!rep.MasterHost.IsNullOrEmpty())
        {
            var node = new RedisNode
            {
                Owner = redis,
                EndPoint = rep.EndPoint,
                Slave = false,
            };

            list.Insert(0, node);
        }

        // 当前节点
        {
            var node = new RedisNode
            {
                Owner = redis,
                EndPoint = server.EndPoint + "",
                Slave = rep.Role != "master",
            };

            list.Insert(0, node);
        }

        return (rep, list);
    }

    /// <summary>根据Key选择节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <returns></returns>
    public virtual IRedisNode SelectNode(String key, Boolean write)
    {
        if (key.IsNullOrEmpty()) return null;

        var now = DateTime.Now;
        var ns = Nodes?.Where(e => e.NextTime < now).ToArray();
        if (ns == null || ns.Length == 0) return null;

        // 先找主节点
        foreach (var item in ns)
        {
            if (!item.Slave) return item;
        }

        // 如果不是写入，也可以使用从节点
        if (!write)
        {
            foreach (var item in ns)
            {
                if (item.Slave) return item;
            }
        }

        return null;
    }

    /// <summary>根据异常重选节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <param name="node"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public virtual IRedisNode ReselectNode(String key, Boolean write, IRedisNode node, Exception exception)
    {
        // 屏蔽旧节点一段时间
        var now = DateTime.Now;
        var redisNode = node as RedisNode;

        var ns = Nodes?.Where(e => e.NextTime < now).ToArray();
        if (ns != null && redisNode != null) ns = ns.Where(e => e.EndPoint != redisNode.EndPoint).ToArray();
        if (ns == null || ns.Length == 0) return null;

        // 读取指令网络异常时，换一个节点
        if (exception is SocketException or IOException)
        {
            // 屏蔽旧节点一段时间
            if (redisNode != null) redisNode.NextTime = now.AddSeconds(Redis.ShieldingTime);

            if (!write) return ns[Interlocked.Increment(ref _idx) % ns.Length];
        }

        return null;
    }
    #endregion
}