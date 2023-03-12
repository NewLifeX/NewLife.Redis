using NewLife.Log;
using NewLife.Net;

namespace NewLife.Caching.Clusters;

/// <summary>Redis哨兵</summary>
public class RedisSentinel : RedisReplication
{
    #region 属性
    RedisReplication _replication;
    RedisCluster _cluster;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    public RedisSentinel(Redis redis) : base(redis) { }
    #endregion

    #region 方法
    String _lastServers;
    /// <summary>分析主从节点</summary>
    public override IList<RedisNode> GetNodes()
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
    #endregion
}