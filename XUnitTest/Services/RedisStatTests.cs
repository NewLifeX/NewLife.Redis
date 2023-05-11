using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.Caching.Services;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Services;

public class StatTests
{
    private readonly FullRedis _redis;

    public StatTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    [Fact]
    public void Test1()
    {
        using var st = new RedisStat(_redis, "SiteHourStat")
        {
            OnSave = OnSave
        };

        // 降低延迟队列间隔
        var q = st.GetValue("_queue") as RedisReliableQueue<String>;
        var dq = q.InitDelay();
        dq.TransferInterval = 2;

        // 计算key
        var date = DateTime.Today;
        var key = $"SiteHourStat:2743-{date:MMdd}";

        // 累加统计
        st.Increment(key, "Total", 1);
        if (Rand.Next(2) > 0)
            st.Increment(key, "Error", 1);
        st.Increment(key, "Cost", Rand.Next(10, 100));

        // 变动key进入延迟队列
        st.AddDelayQueue(key, 2);

        Thread.Sleep(4_000);
        XTrace.WriteLine("Test1 finished");
    }

    private void OnSave(String key, IDictionary<String, Int32> data)
    {
        XTrace.WriteLine("保存：{0}，数据：{1}", key, data.ToJson());
    }
}