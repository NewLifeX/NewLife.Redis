using NewLife.Caching;
using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class RedisClusterTests
{
    public FullRedis _redis { get; set; }

    public RedisClusterTests()
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
        var cluster = new RedisCluster(_redis);
        //cluster.ParseMasterSlaveNodes();

        //var rep = cluster.Replication;
        //Assert.NotNull(rep);
        //Assert.Equal("master", rep.Role);
        //Assert.NotNull(rep.Slaves);

        //Assert.NotNull(cluster.Nodes);
        //Assert.Equal(1 + rep.Slaves.Length, cluster.Nodes.Length);
    }
}
