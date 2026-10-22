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
        // 计算key。先清理上一轮遗留：去重标记会保持10分钟、统计Hash与队列消息也可能残留，
        // 不清理会让本轮拿不到新消息或读到累加后的旧数据（共享测试Redis上的典型互相污染）
        var date = DateTime.Today;
        var key = $"SiteHourStat:2743-{date:MMdd}";
        _redis.Remove($"exists:{key}");
        _redis.Remove(key);

        using var db2 = _redis.CreateSub(_redis.Db + 1) as FullRedis;
        db2.Remove("SiteHourStat");
        db2.Remove("SiteHourStat:Delay");
        foreach (var item in db2.Search("SiteHourStat:Status:*", 0, 1000)) db2.Remove(item);
        foreach (var item in db2.Search("SiteHourStat:Ack:*", 0, 1000)) db2.Remove(item);

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

        // 累加统计
        st.Increment(key, "Total", 1);
        if (Rand.Next(2) > 0)
            st.Increment(key, "Error", 1);
        st.Increment(key, "Cost", Rand.Next(10, 100));

        // 变动key进入延迟队列，等待回调完成而不是固定睡眠。共享测试Redis在并行用例下往返延迟会放大，
        // 这里等的是“最终一定发生”的保存动作而不是时延指标，因此留足等待时间避免误报
        st.AddDelayQueue(key, 1);

        Assert.True(saved.Wait(TimeSpan.FromSeconds(30)), "等待统计保存超时");
        Assert.Equal(key, savedKey);
        Assert.Equal(1, savedData["Total"]);
        Assert.InRange(savedData["Cost"], 10, 99);
        XTrace.WriteLine("Test1 finished");
    }
}