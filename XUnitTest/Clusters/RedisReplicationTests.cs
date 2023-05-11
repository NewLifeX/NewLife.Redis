using NewLife.Caching;
using NewLife.Caching.Clusters;
using NewLife.Log;
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
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

#if DEBUG
    [Fact]
#else
    [Fact(Skip = "No Cluster")]
#endif
    public void GetNodes()
    {
        var cluster = new RedisReplication(_redis);
        cluster.GetNodes();

        var rep = cluster.Replication;
        Assert.NotNull(rep);
        Assert.Equal("master", rep.Role);
        Assert.NotNull(rep.Slaves);

        Assert.NotNull(cluster.Nodes);
        Assert.Equal(1 + rep.Slaves.Length, cluster.Nodes.Length);
    }
}
