using NewLife.Log;
using NewLife.Net;
using NewLife.Threading;

namespace NewLife.Caching.Clusters;

/// <summary>Redis哨兵</summary>
public class RedisSentinel : RedisBase, IRedisCluster, IDisposable
{
    #region 属性
    /// <summary>集群节点</summary>
    public RedisNode[] Nodes { get; protected set; }

    /// <summary>主从信息</summary>
    public ReplicationInfo Replication { get; protected set; }

    /// <summary>是否根据解析得到的节点列表去设置外部Redis的节点地址</summary>
    public Boolean SetHostServer { get; set; }

    private TimerX _timer;
    RedisReplication _replication;
    RedisCluster _cluster;
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
        if (SetHostServer && Nodes != null) _timer = new TimerX(s => GetNodes(), null, 60_000, 60_000) { Async = true };
    }

    String _lastServers;
    /// <summary>分析主从节点</summary>
    public virtual IList<RedisNode> GetNodes()
    {
        var showLog = Nodes == null;
        if (showLog) WriteLog("分析[{0}]哨兵节点：", Redis?.Name);

        var rs = Redis.Execute(r => r.Execute<String>("INFO", "Sentinel"));
        if (rs.IsNullOrEmpty()) return null;

        var rds = Redis as FullRedis;
        var inf = rs.SplitAsDictionary(":", "\r\n");
        var rep = new ReplicationInfo();
        rep.Load(inf);

        Replication = rep;

        var servers = new List<String>();
        if (rep.Masters != null)
        {
            foreach (var item in rep.Masters)
            {
                if (!item.IP.IsNullOrEmpty()) servers.Add(item.EndPoint);
            }
        }
        if (rep.Slaves != null)
        {
            foreach (var item in rep.Slaves)
            {
                if (!item.IP.IsNullOrEmpty()) servers.Add(item.EndPoint);
            }
        }

        // 根据地址查找全部主从复制节点。仅适用于主从复制
        //var (_, nodes) = GetReplications(Redis, servers.Select(e => new NetUri(e)).ToList());

        // 哨兵背后，可能是主从复制，也可能是集群
        var svrs = servers.Join();
        if (_replication == null && _cluster == null || svrs != _lastServers)
        {
            if (!_lastServers.IsNullOrEmpty())
                WriteLog("哨兵监测到节点改变，旧节点：{0}，新节点：{1}", _lastServers, svrs);

            _replication = null;
            _cluster = null;
            _lastServers = svrs;

            // 探测是主从复制还是集群模式
            rds = new FullRedis
            {
                Server = svrs,
                Password = Redis.Password,
                Log = Redis.Log,
            };
            if (rds.Info.TryGetValue("redis_mode", out var mode))
            {
                if (mode == "cluster")
                {
                    _cluster = new RedisCluster(rds);
                    _cluster.StartMonitor();
                }
                else if (mode.EqualIgnoreCase("standalone"))
                {
                    _replication = new RedisReplication(rds);
                    _replication.StartMonitor();
                }
            }
        }

        var nodes = _replication?.Nodes;
        if (nodes == null || nodes.Length == 0) nodes = _cluster?.Nodes;

        if (nodes != null) SetNodes(nodes);

        return nodes;
    }

    private String _lastNodes;
    /// <summary>设置节点</summary>
    /// <param name="nodes"></param>
    protected void SetNodes(IList<RedisNode> nodes)
    {
        var showLog = Nodes == null;

        // 排序，master优先
        nodes = nodes.OrderBy(e => e.Slave).ThenBy(e => e.EndPoint).ToList();
        Nodes = nodes.ToArray();

        if (SetHostServer)
        {
            var uris = new List<NetUri>();
            foreach (var node in nodes)
            {
                if (node.EndPoint.IsNullOrEmpty()) return;

                var uri = new NetUri(node.EndPoint);
                if (uri.Port == 0) uri.Port = 6379;
                uris.Add(uri);
            }
            if (uris.Count > 0) Redis.SetSevices(uris.ToArray());
        }

        var str = nodes.Join("\n", e => $"{e.EndPoint}-{e.Slave}");
        if (_lastNodes != str)
        {
            WriteLog("得到[{0}]节点：", Redis?.Name);
            showLog = true;
            _lastNodes = str;
        }
        foreach (var node in nodes)
        {
            if (showLog) WriteLog("节点：{0} {1}", node.Slave ? "slave" : "master", node.EndPoint);
        }
    }

    /// <summary>根据Key选择节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <returns></returns>
    public virtual IRedisNode SelectNode(String key, Boolean write) => null;

    /// <summary>根据异常重选节点</summary>
    /// <param name="key">键</param>
    /// <param name="write">可写</param>
    /// <param name="node"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    public virtual IRedisNode ReselectNode(String key, Boolean write, IRedisNode node, Exception exception) => null;
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