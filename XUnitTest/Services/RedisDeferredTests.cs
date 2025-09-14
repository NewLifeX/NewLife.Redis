using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NewLife.Caching;
using NewLife.Caching.Services;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Services;

public class RedisDeferredTests
{
    private readonly FullRedis _redis;

    public RedisDeferredTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Expire = 10 * 60;
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    [Fact]
    public void Test1()
    {
        String[] keys = null;
        IDictionary<String, Int32> vs = null;

        // 删除旧数据
        var olds = _redis.Search("SiteDayStat:*", 0, 100).ToArray();
        if (olds != null && olds.Length > 0) _redis.Remove(olds);

        var vv = new[] { Rand.Next(10000), Rand.Next(10000), Rand.Next(10, 1000) };

        using var st = new RedisDeferred(_redis, "SiteDayStat");
        st.Period = 2000;
        st.Process += (s, e) =>
        {
            keys = e.Keys;
            for (var i = 0; i < keys.Length; i++)
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

        for (var i = 0; i < 30; i++)
        {
            if (keys != null) break;

            Thread.Sleep(1_000);
        }

        Assert.NotNull(keys);
        Assert.Equal(2, keys.Length);
        //Assert.Equal(key, keys[0]);

        //Thread.Sleep(60_000);
        key = keys[^1];
        Assert.Equal(3, vs.Count);
        Assert.Equal(vv[0], vs[$"{key}:Total"]);
        Assert.Equal(vv[1], vs[$"{key}:Error"]);
        Assert.Equal(vv[2], vs[$"{key}:Cost"]);
    }
}