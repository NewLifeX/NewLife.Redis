using NewLife.Caching;
using NewLife.Log;

namespace XUnitTest;

/// <summary>集成测试基类。仅提供 Redis 连接，不包含测试方法，避免派生类重复执行基类用例</summary>
public abstract class RedisTestBase
{
    protected readonly FullRedis _redis;

    protected RedisTestBase()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Db = 2;
        _redis.Retry = 0;
        _redis.Timeout = 2_000;
        _redis.Log = XTrace.Log;

        if (BasicTest.EnableClientLog) _redis.ClientLog = XTrace.Log;
    }
}
