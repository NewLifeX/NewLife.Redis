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

// 队列落在 DB4，与 Basic 家族的 SwapDB/Move 整库操作串行，避免互踩
[Collection("Basic")]
public class StatTests
{
    private readonly FullRedis _redis;

    public StatTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Log = XTrace.Log;

        if (BasicTest.EnableClientLog) _redis.ClientLog = XTrace.Log;
    }

    [RedisFact]
    public void Test1()
    {
        using var saved = new ManualResetEventSlim(false);
        String savedKey = null;
        IDictionary<String, Int32> savedData = null;

        using var st = new RedisStat(_redis, "SiteHourStat")
        {
            OnSave = (key, data) =>
            {
                savedKey = key;
                savedData = data;
                saved.Set();
            }
        };

        // 降低延迟队列间隔
        var q = st.GetValue("_queue") as RedisReliableQueue<String>;
        var dq = q.InitDelay();
        dq.TransferInterval = 1;

        // 计算key
        var date = DateTime.Today;
        var key = $"SiteHourStat:2743-{date:MMdd}";

        // 累加统计
        st.Increment(key, "Total", 1);
        if (Rand.Next(2) > 0)
            st.Increment(key, "Error", 1);
        st.Increment(key, "Cost", Rand.Next(10, 100));

        // 变动key进入延迟队列，等待回调完成而不是固定睡眠
        st.AddDelayQueue(key, 1);

        Assert.True(saved.Wait(TimeSpan.FromSeconds(10)), "等待统计保存超时");
        Assert.Equal(key, savedKey);
        Assert.Equal(1, savedData["Total"]);
        Assert.InRange(savedData["Cost"], 10, 99);
        XTrace.WriteLine("Test1 finished");
    }
}