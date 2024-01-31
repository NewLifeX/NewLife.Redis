using System.Net.Sockets;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Caching.Clusters;

/// <summary>Redis集群</summary>
public class RedisCluster : RedisBase, IRedisCluster, IDisposable
{
    #region 属性

    /// <summary>
    /// redis nodes
    /// </summary>
    public List<IRedisNode> RedisNodes => Nodes.Select(x => (IRedisNode)x).ToList();

    /// <summary>集群节点</summary>
    public ClusterNode[]? Nodes { get; private set; }

    private TimerX? _timer;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    public RedisCluster(Redis redis) : base(redis, "") { }

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

    /// <summary>获取节点</summary>
    public void GetNodes()
    {
        if (Redis is not FullRedis rds) return;

        var rs = Redis.Execute(r => r.Execute<String>("Cluster", "Nodes"));
        if (rs.IsNullOrEmpty()) return;

        ParseNodes(rs);
    }

    private String? _lastNodes;
    /// <summary>分析节点</summary>
    /// <param name="nodes"></param>
    public void ParseNodes(String nodes)
    {
        var showLog = Nodes == null;
        if (showLog) WriteLog("分析[{0}]集群节点：", Redis.Name);

        var list = new List<ClusterNode>();
        foreach (var item in nodes.Split("\r", "\n"))
        {
            if (!item.IsNullOrEmpty())
            {
                var node = new ClusterNode
                {
                    Owner = Redis
                };

                if (showLog) XTrace.WriteLine("{0}", item);

                node.Parse(item);
                list.Add(node);

                //XTrace.WriteLine("[{0}]节点：{1}", Redis.Name, node);
            }
        }

        var str = list.Join("\n", n => n + " " + n?.Slots.Join(","));
        if (str != _lastNodes)
        {
            if (!showLog) WriteLog("分析[{0}]集群节点：", Redis.Name);
            showLog = true;
            _lastNodes = str;
        }

        //list = list.OrderBy(e => e.EndPoint).ToList();
        list = SortNodes(list);

        foreach (var node in list)
        {
            if (showLog) WriteLog("节点：{0} {1} {2}", node, node.Flags, node.Slots.Join(" "));

            if (node.Slaves != null)
            {
                foreach (var item in node.Slaves)
                {
                    if (showLog) XTrace.WriteLine("      {0} {1}", item, item.Flags);
                }
            }
        }
        Nodes = list.ToArray();
    }

    private List<ClusterNode> SortNodes(List<ClusterNode> list)
    {
        // 主节点按照数据槽排序
        var masters = list.Where(e => e.Master == "-").OrderBy(e => e.Slots.Min(x => x.From)).ToList();
        var slaves = list.Where(e => e.Master != "-").ToList();

        // 从节点插入主节点
        foreach (var node in masters)
        {
            var ns = slaves.Where(e => e.Master == node.ID).OrderBy(e => e.EndPoint).ToList();
            if (ns.Count > 0) node.Slaves = ns;
        }

        return masters;
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
        var ns = Nodes?.Where(e => e.LinkState == 1).ToArray();

        var slot = key.GetBytes().Crc16() % 16384;
        ns = ns?.Where(e => e.Contain(slot)).ToArray();
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

        throw new XException($"集群[{Redis.Name}]的[{slot}]槽没有可用节点（key={key}）");
        //return null;
    }

    /// <summary>根据异常重选节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <param name="node"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public IRedisNode? ReselectNode(String key, Boolean write, IRedisNode node, Exception exception)
    {
        using var span = Redis.Tracer?.NewSpan("redis:ReselectNode", new { key, (node as RedisNode)?.EndPoint });

        // 屏蔽旧节点一段时间
        var now = DateTime.Now;

        // 处理MOVED和ASK指令
        var msg = exception.Message;
        if (msg.StartsWithIgnoreCase("MOVED", "ASK"))
        {
            // 取出地址，找到新的节点
            var endpoint = msg.Substring(" ");
            if (!endpoint.IsNullOrEmpty())
            {
                span?.AppendTag(endpoint);

                return Map(endpoint, key);
            }
        }

        // 读取指令网络异常时，换一个节点
        if (exception is SocketException or IOException)
        {
            // 屏蔽旧节点一段时间
            if (node is RedisNode redisNode && ++redisNode.Error >= Redis.Retry)
            {
                redisNode.NextTime = now.AddSeconds(Redis.ShieldingTime);
                msg = $"屏蔽 {redisNode.EndPoint} 到 {redisNode.NextTime.ToFullString()}";
                span?.AppendTag(msg);
                Redis.WriteLog(msg);
            }
        }

        return SelectNode(key, write);
    }

    /// <summary>把Key映射到指定地址的节点</summary>
    /// <param name="endpoint"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual ClusterNode? Map(String endpoint, String key)
    {
        var node = Nodes.FirstOrDefault(e => e.EndPoint == endpoint);
        if (node == null) return null;

        if (!key.IsNullOrEmpty())
        {
            var slot = key.GetBytes().Crc16() % 16384;
            AddSlots(node, slot);
        }

        return node;
    }

    /// <summary>重置节点。设置成功状态</summary>
    /// <param name="node"></param>
    public virtual void ResetNode(IRedisNode node)
    {
        if (node is RedisNode redisNode) redisNode.Error = 0;
    }

    /// <summary>向集群添加新节点</summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    public virtual void Meet(String ip, Int32 port) => Execute((r, k) => r.Execute("CLUSTER", "MEET", ip, port));

    /// <summary>向节点增加槽</summary>
    /// <param name="node"></param>
    /// <param name="slots"></param>
    /// <returns></returns>
    public virtual void AddSlots(ClusterNode node, params Int32[] slots)
    {
        var pool = (Redis as FullRedis)!.GetPool(node);
        var client = pool.Get();
        try
        {
            var args = new List<Object>(slots.Length + 1) { "ADDSLOTS" };
            args.AddRange(slots.Cast<Object>());

            client.Execute("CLUSTER", args.ToArray());
        }
        catch (Exception ex)
        {
            Redis.Log.Error(ex.Message);
        }
        finally
        {
            pool.Put(client);
        }
    }

    /// <summary>从节点删除槽</summary>
    /// <param name="node"></param>
    /// <param name="slots"></param>
    /// <returns></returns>
    public virtual void DeleteSlots(ClusterNode node, params Int32[] slots)
    {
        var pool = (Redis as FullRedis)!.GetPool(node);
        var client = pool.Get();
        try
        {
            var args = new List<Object>(slots.Length + 1) { "DELSLOTS" };
            args.AddRange(slots.Cast<Object>());

            client.Execute("CLUSTER", args.ToArray());

            //foreach (var item in slots)
            //{
            //    try
            //    {
            //        client.Execute("CLUSTER", "DELSLOTS", item);
            //    }
            //    catch (Exception ex)
            //    {
            //        Redis.Log.Error(ex.Message);
            //    }
            //}
        }
        catch (Exception ex)
        {
            Redis.Log.Error(ex.Message);
        }
        finally
        {
            pool.Put(client);
        }
    }

    /// <summary>重新负载均衡</summary>
    /// <remarks>
    /// 节点迁移太负责，直接干掉原来的分配，重新全局分配
    /// </remarks>
    public virtual Boolean Rebalance()
    {
        GetNodes();

        var ns = Nodes?.ToList();
        if (ns == null || ns.Count == 0) return false;

        // 全部有效节点
        ns = ns.Where(e => e.LinkState == 1 && !e.Slave).ToList();
        if (ns.Count == 0) return false;

        //!!! 节点迁移太复杂，直接干掉原来的分配，重新全局分配
        foreach (var item in ns)
        {
            var sts = item.GetSlots();
            if (sts == null || sts.Length == 0) continue;

            DeleteSlots(item, sts);
        }

        // 地址排序，然后分配
        ns = ns.OrderBy(e => e.EndPoint).ToList();

        // 平均分
        var size = 16384 / ns.Count;
        var y = 16384 % ns.Count;
        var start = 0;
        var k = 0;
        foreach (var item in ns)
        {
            item.Slots.Clear();

            var to = start + size;
            // 前面y个可以多分一个
            if (k++ < y) to++;
            item.Slots.Add(new Slot
            {
                From = start,
                To = to - 1,
            });

            // 执行命令
            AddSlots(item, Enumerable.Range(start, to - start).ToArray());

            start = to;
        }

        return true;
    }
    #endregion

    #region 辅助
    /// <summary>日志</summary>
    public ILog Log { get; set; } = XTrace.Log;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);
    #endregion
}