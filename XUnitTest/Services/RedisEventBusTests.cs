using System;
using System.Threading.Tasks;
using NewLife;
using NewLife.Caching;
using NewLife.Caching.Services;
using NewLife.Messaging;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Services;

/// <summary>Redis事件总线测试。验证消息携带发布者标识并透传到订阅上下文</summary>
public class RedisEventBusTests
{
    [RedisFact]
    public async Task PublishAndSubscribe_ClientId()
    {
        var redis = new FullRedis();
        redis.Init(BasicTest.GetConfig());

        var topic = "rb_" + Rand.NextString(8);
        try
        {
            using var bus = new RedisEventBus<Int32>(redis, topic, "test-group");

            var source = new TaskCompletionSource<Int32>(TaskCreationOptions.RunContinuationsAsynchronously);
            String? clientId = null;
            bus.Subscribe((evt, ctx, ct) =>
            {
                clientId = (ctx as EventContext)?.ClientId;
                source.TrySetResult(evt);
                return Task.CompletedTask;
            });

            // 稍等消费循环就绪后连续发布，规避“从最后位置消费”的时序竞态
            await Task.Delay(1_000);
            await bus.PublishAsync(1);
            await bus.PublishAsync(2);
            await bus.PublishAsync(3);

            var completed = await Task.WhenAny(source.Task, Task.Delay(10_000));
            Assert.Same(source.Task, completed);

            Assert.Contains(await source.Task, new[] { 1, 2, 3 });
            Assert.Equal(Runtime.ClientId, clientId);
        }
        finally
        {
            redis.Remove(topic);
            redis.Dispose();
        }
    }

    class OnlineEvent
    {
        public String? UserName { get; set; }
        public Int32 Level { get; set; }
    }

    [RedisFact]
    public async Task PublishAndSubscribe_ClientId_Complex()
    {
        var redis = new FullRedis();
        redis.Init(BasicTest.GetConfig());

        var topic = "rb_" + Rand.NextString(8);
        try
        {
            using var bus = new RedisEventBus<OnlineEvent>(redis, topic, "test-group");

            var source = new TaskCompletionSource<OnlineEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
            String? clientId = null;
            bus.Subscribe((evt, ctx, ct) =>
            {
                clientId = (ctx as EventContext)?.ClientId;
                source.TrySetResult(evt);
                return Task.CompletedTask;
            });

            // 稍等消费循环就绪后发布，规避“从最后位置消费”的时序竞态
            await Task.Delay(1_000);
            await bus.PublishAsync(new OnlineEvent { UserName = "smartStone", Level = 9 });

            var completed = await Task.WhenAny(source.Task, Task.Delay(10_000));
            Assert.Same(source.Task, completed);

            var evt = await source.Task;
            Assert.Equal("smartStone", evt.UserName);
            Assert.Equal(9, evt.Level);
            Assert.Equal(Runtime.ClientId, clientId);
        }
        finally
        {
            redis.Remove(topic);
            redis.Dispose();
        }
    }
}
