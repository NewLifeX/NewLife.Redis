using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Caching.Clusters;

/// <summary>Redis哨兵</summary>
public class RedisSentinel : RedisBase, IRedisCluster, IDisposable
{
    #region 属性
    /// <summary>集群节点</summary>
    public ClusterNode[] Nodes { get; private set; }

    /// <summary>主从信息</summary>
    public ReplicationInfo Replication { get; private set; }

    private TimerX _timer;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    public RedisSentinel(Redis redis) : base(redis, null) { }

    /// <summary>销毁</summary>
    public void Dispose() => _timer.TryDispose();
    #endregion

    #region 方法
    /// <summary>开始监控节点</summary>
    public void StartMonitor()
    {
        GetNodes();

        // 定时刷新集群节点列表
        if (Nodes != null) _timer = new TimerX(s => GetNodes(), null, 60_000, 600_000) { Async = true };
    }

    /// <summary>分析主从节点</summary>
    public void GetNodes()
    {
        var showLog = Nodes == null;
        if (showLog) XTrace.WriteLine("分析[{0}]哨兵节点：", Redis?.Name);

        var rs = Execute(r => r.Execute<String>("INFO", "Sentinel"));
        if (rs.IsNullOrEmpty()) return;

        var inf = rs.SplitAsDictionary(":", "\r\n");
        var rep = new ReplicationInfo();
        rep.Load(inf);

        Replication = rep;

        var list = new List<ClusterNode>();
        foreach (var item in rep.Slaves)
        {
            if (item.IP.IsNullOrEmpty()) continue;

            var node = new ClusterNode
            {
                EndPoint = $"{item.IP}:{item.Port}",
                Slave = true,
            };
            list.Add(node);
        }

        // Master节点
        if (rep.Role == "master")
        {
            var node = new ClusterNode
            {
                Slave = false,
                Slaves = list,
            };

            Nodes = new[] { node };
        }
        else
        {
            var node = new ClusterNode
            {
                Slave = false,
                Slaves = list,
            };

            list.Add(node);
            Nodes = list.ToArray();
        }
    }

    /// <summary>根据Key选择节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <returns></returns>
    public virtual IRedisNode SelectNode(String key, Boolean write)
    {
        if (key.IsNullOrEmpty()) return null;

        var slot = key.GetBytes().Crc16() % 16384;
        var ns = Nodes.Where(e => e.LinkState == 1).ToList();

        // 找主节点
        foreach (var node in ns)
        {
            if (!node.Slave && node.Contain(slot)) return node;
        }

        // 找从节点
        foreach (var node in ns)
        {
            if (node.Contain(slot)) return node;
        }

        return null;
    }

    /// <summary>根据异常重选节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public IRedisNode ReselectNode(String key, Boolean write, Exception exception)
    {
        // 处理MOVED和ASK指令
        var msg = exception.Message;
        if (msg.StartsWithIgnoreCase("MOVED", "ASK"))
        {
            // 取出地址，找到新的节点
            var endpoint = msg.Substring(" ");
            if (!endpoint.IsNullOrEmpty())
            {
                return Map(endpoint, key);
            }
        }

        return null;
    }

    /// <summary>把Key映射到指定地址的节点</summary>
    /// <param name="endpoint"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual ClusterNode Map(String endpoint, String key)
    {
        var node = Nodes.FirstOrDefault(e => e.EndPoint == endpoint);
        if (node == null) return null;

        return node;
    }
    #endregion
}