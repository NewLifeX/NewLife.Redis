using NewLife.Caching;
using NewLife.Caching.Clusters;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Clusters;

public class RedisSentinelTests
{
    public FullRedis _redis { get; set; }

    public RedisSentinelTests()
    {
        var config = BasicTest.GetConfig();
#if DEBUG
        config = "server=127.0.0.1:7002,127.0.0.1:7003,127.0.0.1:7004";
#endif

        _redis = new FullRedis();
        _redis.Init(config);
        //_redis.Db = 2;
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
        var cluster = new RedisSentinel(_redis);
        cluster.GetNodes();

        var rep = cluster.Replication;
        Assert.NotNull(rep);
        //Assert.Equal("sentinel", rep.Role);
        Assert.NotNull(rep.Masters);

        Assert.NotNull(cluster.Nodes);
        //Assert.Equal(rep.Masters.Length, cluster.Nodes.Length);
        Assert.Equal(4, cluster.Nodes.Length);
    }
}
