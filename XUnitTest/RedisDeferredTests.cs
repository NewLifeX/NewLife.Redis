using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.Caching;
using NewLife.Security;
using Xunit;

namespace XUnitTest;

public class RedisDeferredTests
{
    private readonly FullRedis _redis;

    public RedisDeferredTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
#if DEBUG
        _redis.Log = NewLife.Log.XTrace.Log;
#endif
        _redis.Expire = 10 * 60;
    }

    [Fact]
    public void Test1()
    {
        String[] keys = null;
        IDictionary<String, Int32> vs = null;

        var vv = new[] { Rand.Next(10000), Rand.Next(10000), Rand.Next(10, 1000) };

        using var st = new RedisDeferred(_redis, "SiteDayStat");
        st.Period = 2000;
        st.Process += (s, e) =>
        {
            keys = e.Keys;
            for (int i = 0; i < keys.Length; i++)
            {
                // 模拟业务操作，取回数据，然后删除
                var ks = new[] { $"{keys[i]}:Total", $"{keys[i]}:Error", $"{keys[i]}:Cost" };
                vs = _redis.GetAll<Int32>(ks);
                var rs = _redis.Remove(ks);
                Assert.Equal(3, rs);
            }
        };

        // 计算key
        var date = DateTime.Today;
        var key = $"SiteDayStat:1234-{date:MMdd}";

        // 累加统计
        _redis.Increment($"{key}:Total", vv[0]);
        _redis.Increment($"{key}:Error", vv[1]);
        _redis.Increment($"{key}:Cost", vv[2]);

        // 变动key进入延迟队列
        st.Add(key);

        var key2 = $"SiteDayStat:5678-{date:MMdd}";

        // 累加统计
        _redis.Increment($"{key2}:Total", vv[0]);
        _redis.Increment($"{key2}:Error", vv[1]);
        _redis.Increment($"{key2}:Cost", vv[2]);

        // 变动key进入延迟队列
        st.Add(key2);

        Thread.Sleep(3_000);

        Assert.NotNull(keys);
        Assert.Equal(2, keys.Length);
        Assert.Equal(key2, keys[0]);

        Assert.Equal(3, vs.Count);
        Assert.Equal(vv[0], vs[$"{key}:Total"]);
        Assert.Equal(vv[1], vs[$"{key}:Error"]);
        Assert.Equal(vv[2], vs[$"{key}:Cost"]);
    }
}