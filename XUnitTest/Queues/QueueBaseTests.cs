using System;
using NewLife.Caching;
using NewLife.Caching.Queues;
using Xunit;

namespace XUnitTest.Queues;

public class QueueBaseTests
{
    [Fact(DisplayName = "队列基类默认参数测试")]
    public void Defaults()
    {
        var redis = new FullRedis();
        redis.Init("server=127.0.0.1:6379");

        var queue = redis.GetQueue<String>("queue_base_defaults") as QueueBase;
        Assert.NotNull(queue);

        Assert.Equal("queue_base_defaults", queue.TraceName);
        Assert.True(queue.AttachTraceId);
        Assert.False(queue.ThrowOnFailure);
        Assert.Equal(3, queue.RetryTimesWhenSendFailed);
        Assert.Equal(1000, queue.RetryIntervalWhenSendFailed);
    }
}
