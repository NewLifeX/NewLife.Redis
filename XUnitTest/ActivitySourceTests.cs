using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

/// <summary>OpenTelemetry ActivitySource 原生埋点测试</summary>
[Collection("Basic")]
public class ActivitySourceTests : RedisTestBase
{
    [RedisFact(DisplayName = "原生ActivitySource输出Redis命令标签")]
    public void EmitActivity()
    {
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateListener(activities);

        var key = "otel_activity_test";
        _redis.Set(key, "hello");
        var value = _redis.Get<String>(key);
        Assert.Equal("hello", value);
        _redis.Remove(key);

        var list = activities.ToArray();
        Assert.Contains(list, a => a.DisplayName == "SET");
        Assert.Contains(list, a => a.DisplayName == "GET");

        var set = list.First(a => a.DisplayName == "SET");
        Assert.Equal("redis", set.GetTagItem("db.system"));
        Assert.Equal(_redis.Db.ToString(), set.GetTagItem("db.namespace")?.ToString());
        Assert.False(String.IsNullOrEmpty(set.GetTagItem("server.address")?.ToString()));
        Assert.Equal(ActivityKind.Client, set.Kind);
    }

    [RedisFact(DisplayName = "ActivitySource记录命令异常状态")]
    public void EmitErrorActivity()
    {
        var activities = new ConcurrentBag<Activity>();
        using var listener = CreateListener(activities);

        Assert.ThrowsAny<RedisException>(() => _redis.Execute(rc => rc.Execute<Object>("NL_OTEL_UNKNOWN_COMMAND")));

        var act = activities.FirstOrDefault(a => a.DisplayName == "NL_OTEL_UNKNOWN_COMMAND");
        Assert.NotNull(act);
        Assert.Equal(ActivityStatusCode.Error, act!.Status);
        Assert.False(String.IsNullOrEmpty(act.GetTagItem("error.type")?.ToString()));
    }

    private static ActivityListener CreateListener(ConcurrentBag<Activity> activities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == Redis.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
