using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class RedisSentinelTests
{
    public FullRedis _redis { get; set; }

    public RedisSentinelTests()
    {
        var config = BasicTest.GetConfig();
#if DEBUG
        config = "server=127.0.0.1:7001";
#endif

        _redis = new FullRedis();
        _redis.Init(config);
        //_redis.Db = 2;

#if DEBUG
        _redis.Log = NewLife.Log.XTrace.Log;
#endif
    }

    [Fact]
    public void GetNodes()
    {
        var cluster = new RedisSentinel(_redis);
        cluster.GetNodes();

        var rep = cluster.Replication;
        Assert.NotNull(rep);
        Assert.Equal("sentinel", rep.Role);
        Assert.NotNull(rep.Masters);

        Assert.NotNull(cluster.Nodes);
        Assert.Equal(rep.Masters.Length, cluster.Nodes.Length);
    }
}
