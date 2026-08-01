using System;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

/// <summary>RedLock 多实例分布式锁测试</summary>
[Collection("Basic")]
public class RedisRedLockTests
{
    private readonly FullRedis _redis;

    public RedisRedLockTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Retry = 0;
        _redis.Timeout = 2_000;
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    [RedisFact(DisplayName = "RedLock正常申请释放")]
    public void AcquireRelease()
    {
        var key = "redlock:normal:" + Guid.NewGuid().ToString("N");
        try
        {
            using (var rl = _redis.AcquireRedLock([], key, 1000, 5000))
            {
                Assert.NotNull(rl);

                // 已被加锁，单实例 quorum=1，再次申请同一key必然失败
                var rl2 = _redis.AcquireRedLock([], key, 200, 5000);
                Assert.Null(rl2);
            }

            // 释放后可重新加锁
            using var rl3 = _redis.AcquireRedLock([], key, 1000, 5000);
            Assert.NotNull(rl3);
        }
        finally
        {
            _redis.Remove(key);
        }
    }

    [RedisFact(DisplayName = "RedLock毫秒级锁自动过期")]
    public void MsExpire()
    {
        var key = "redlock:ms:" + Guid.NewGuid().ToString("N");
        try
        {
            // msExpire=500 小于1秒，历史上整数除法后过期时间为0，导致锁永不过期
            using var rl = _redis.AcquireRedLock([], key, 1000, 500);
            Assert.NotNull(rl);

            // 锁必须带过期时间（PTTL毫秒>0），不能是永不过期（-1）
            // TTL秒级精度对500ms过期会返回0，必须用PTTL毫秒级判断
            var pttl = _redis.Execute(key, (rc, k) => rc.Execute<Int32>("PTTL", k));
            Assert.True(pttl > 0, $"毫秒级锁应设置过期时间，实际PTTL={pttl}ms，锁可能永不过期");
        }
        finally
        {
            _redis.Remove(key);
        }
    }
}
