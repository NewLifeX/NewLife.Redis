using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class RedisReplicationTests
{
    public FullRedis _redis { get; set; }

    public RedisReplicationTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Db = 2;

#if DEBUG
        _redis.Log = NewLife.Log.XTrace.Log;
#endif
    }

    [Fact]
    public void ParseMasterSlaveNodes()
    {
        var cluster = new RedisReplication(_redis);
        cluster.ParseMasterSlaveNodes();

        var rep = cluster.Replication;
        Assert.NotNull(rep);
        Assert.Equal("master", rep.Role);
        Assert.NotNull(rep.Slaves);

        Assert.NotNull(cluster.Nodes);
        Assert.Equal(1 + rep.Slaves.Length, cluster.Nodes.Length);
    }
}
