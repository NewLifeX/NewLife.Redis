using System;
using System.Diagnostics;
using System.Threading;
using NewLife.Caching;
using NewLife.Log;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

//[Collection("Basic")]
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class RedisLockTest
{
    private Redis _redis;

    public RedisLockTest()
    {
        var config = BasicTest.GetConfig();

        _redis = new Redis();
        _redis.Init(config);
#if DEBUG
        _redis.Log = XTrace.Log;
#endif
    }
    [TestOrder(50)]
    [Fact(DisplayName = "正常锁")]
    public void TestLock1()
    {
        var ic = _redis;

        var ck = ic.AcquireLock("lock:TestLock1", 3000);
        var k2 = ck as CacheLock;

        Assert.NotNull(k2);
        Assert.Equal("lock:TestLock1", k2.Key);

        // 实际上存在这个key
        Assert.True(ic.ContainsKey(k2.Key));

        // 取有效期
        var exp = ic.GetExpire(k2.Key);
        Assert.True(exp.TotalMilliseconds <= 3000);

        // 释放锁
        ck.Dispose();

        // 这个key已经不存在
        Assert.False(ic.ContainsKey(k2.Key));
    }

    [TestOrder(52)]
    [Fact(DisplayName = "抢锁失败")]
    public void TestLock2()
    {
        var ic = _redis;

        var ck1 = ic.AcquireLock("lock:TestLock2", 2000);
        // 故意不用using，验证GC是否能回收
        //using var ck1 = ic.AcquireLock("TestLock2", 3000);

        var sw = Stopwatch.StartNew();

        // 抢相同锁，不可能成功。超时时间必须小于3000，否则前面的锁过期后，这里还是可以抢到的
        Assert.Throws<InvalidOperationException>(() => ic.AcquireLock("lock:TestLock2", 1000));

        // 耗时必须超过有效期
        sw.Stop();
        XTrace.WriteLine("TestLock2 ElapsedMilliseconds={0}ms", sw.ElapsedMilliseconds);
        Assert.True(sw.ElapsedMilliseconds >= 1000);

        Thread.Sleep(2000 - 1000 + 100);

        // 那个锁其实已经不在了，缓存应该把它干掉
        Assert.False(ic.ContainsKey("lock:TestLock2"));
    }

    [TestOrder(54)]
    [Fact(DisplayName = "抢锁失败2")]
    public void TestLock22()
    {
        var ic = _redis;

        var ck1 = ic.AcquireLock("lock:TestLock2", 2000);
        // 故意不用using，验证GC是否能回收
        //using var ck1 = ic.AcquireLock("TestLock2", 3000);

        var sw = Stopwatch.StartNew();

        // 抢相同锁，不可能成功。超时时间必须小于3000，否则前面的锁过期后，这里还是可以抢到的
        var ck2 = ic.AcquireLock("lock:TestLock2", 1000, 1000, false);
        Assert.Null(ck2);

        // 耗时必须超过有效期
        sw.Stop();
        XTrace.WriteLine("TestLock2 ElapsedMilliseconds={0}ms", sw.ElapsedMilliseconds);
        Assert.True(sw.ElapsedMilliseconds >= 1000);

        Thread.Sleep(2000 - 1000 + 100);

        // 那个锁其实已经不在了，缓存应该把它干掉
        Assert.False(ic.ContainsKey("lock:TestLock2"));
    }

    [TestOrder(56)]
    [Fact(DisplayName = "抢死锁")]
    public void TestLock3()
    {
        var ic = _redis;

        using var ck = ic.AcquireLock("TestLock3", 1000);

        // 已经过了一点时间
        Thread.Sleep(500);

        // 循环多次后，可以抢到
        using var ck2 = ic.AcquireLock("TestLock3", 1000);
        Assert.NotNull(ck2);
    }

}