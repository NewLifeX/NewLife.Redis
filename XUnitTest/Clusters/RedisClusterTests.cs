using NewLife.Caching;
using NewLife.Caching.Clusters;
using NewLife.Log;
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
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    [Fact]
    public void ParseNodes()
    {
        var str = """
            1d30dccb6ef7daedd79884d4c4fd93cfaf848c17 172.16.10.32:6379@16379 myself,master - 0 1551270675000 1 connected 0-4095 [12222-<-0655825d6cb9148d5bfb9f68bdfb4e1651fac62e] [14960-<-eb2da2a40037265b9f21022d2c6e2ba00e91b67c]
            2c24ef8cbe72ac2f987fb15a08d017c6aefe9fab 172.16.10.34:7000@17000 slave 9412d9fba8f7cb5c99f3a29f2abdda44dce8b506 0 1551270679509 8 connected
            eb2da2a40037265b9f21022d2c6e2ba00e91b67c 172.16.10.32:7000@17000 master - 0 1551270680511 2 connected 12288-16383
            0655825d6cb9148d5bfb9f68bdfb4e1651fac62e 172.16.10.34:6379@16379 master - 0 1551270678000 7 connected 8192-12287
            88d1256ee4142e220516508b29b8ebdee80521a0 172.16.10.34:7001@17001 slave 1d30dccb6ef7daedd79884d4c4fd93cfaf848c17 0 1551270677504 9 connected
            9412d9fba8f7cb5c99f3a29f2abdda44dce8b506 172.16.10.33:6379@16379 master - 0 1551270677000 4 connected 4096-8191
            71cd78e1f4aeee042ea99925c72f0a943a061ed4 172.16.10.32:7001@17001 slave 0655825d6cb9148d5bfb9f68bdfb4e1651fac62e 0 1551270676000 7 connected
            8b5e17020b0fcc742341c583518c2ab247b34afa 172.16.10.33:7001@17001 slave eb2da2a40037265b9f21022d2c6e2ba00e91b67c 0 1551270680000 6 connected
            220fd8bd50d5329c5ac5b867991df12237f102ed 172.16.10.33:7000@17000 slave 1d30dccb6ef7daedd79884d4c4fd93cfaf848c17 0 1551270676000 5 connected

            """;

        var cluster = new RedisCluster(null);
        cluster.ParseNodes(str);
    }
}
