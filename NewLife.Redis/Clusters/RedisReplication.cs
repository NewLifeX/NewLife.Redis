using System.Net.Sockets;
using System.Text;
using NewLife.Log;
using NewLife.Model;
using NewLife.Threading;

namespace NewLife.Caching.Clusters;

/// <summary>Redis主从复制</summary>
public class RedisReplication : RedisBase, IRedisCluster, IDisposable
{
    #region 属性
    /// <summary>集群节点</summary>
    public RedisNode[] Nodes { get; private set; }

    /// <summary>主从信息</summary>
    public ReplicationInfo Replication { get; private set; }

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
        if (Nodes != null) _timer = new TimerX(s => GetNodes(), null, 60_000, 600_000) { Async = true };
    }

    /// <summary>分析主从节点</summary>
    public void GetNodes()
    {
        var showLog = Nodes == null;
        if (showLog) XTrace.WriteLine("分析[{0}]主从节点：", Redis?.Name);

        // 从第一个连接获取一次信息，仅支持一主一从或一主多从的情况
        var rs = Redis.Execute(r => r.Execute<String>("INFO", "Replication"));
        if (rs.IsNullOrEmpty()) return;

        var inf = rs.SplitAsDictionary(":", "\r\n");
        var rep = new ReplicationInfo();
        rep.Load(inf);

        Replication = rep;

        var list = new List<RedisNode>();
        foreach (var item in rep.Slaves)
        {
            if (item.IP.IsNullOrEmpty()) continue;

            var node = new RedisNode
            {
                Owner = Redis,
                EndPoint = $"{item.IP}:{item.Port}",
                Slave = true,
            };
            list.Add(node);
        }

        // Master节点
        {
            var node = new RedisNode
            {
                Owner = Redis,
                EndPoint = Redis.Server,
                Slave = rep.Role != "master",
            };

            list.Insert(0, node);
        }

        foreach (var node in list)
        {
            var name = Redis?.Name + "";
            if (!name.IsNullOrEmpty()) name = $"[{name}]";

            if (showLog) XTrace.WriteLine("节点：{0} {1}", node.Slave ? "slave" : "master", node.EndPoint);
        }

        Nodes = list.ToArray();
    }

    /// <summary>根据Key选择节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <returns></returns>
    public virtual IRedisNode SelectNode(String key, Boolean write)
    {
        if (key.IsNullOrEmpty()) return null;

        var ns = Nodes;
        if (ns == null || ns.Length == 0) return null;

        // 先找主节点
        foreach (var item in Nodes)
        {
            if (!item.Slave) return item;
        }

        // 如果不是写入，也可以使用从节点
        if (!write) return Nodes[Interlocked.Increment(ref _idx) % ns.Length];

        return null;
    }

    /// <summary>根据异常重选节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public IRedisNode ReselectNode(String key, Boolean write, Exception exception)
    {
        var ns = Nodes;
        if (ns == null || ns.Length == 0) return null;

        // 读取指令网络异常时，换一个从节点
        if (exception is SocketException or IOException)
        {
            if (!write) return Nodes[Interlocked.Increment(ref _idx) % ns.Length];
        }

        return null;
    }
    #endregion
}