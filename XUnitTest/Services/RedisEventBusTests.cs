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

            // 消费循环在后台创建消费组，固定延迟既慢又不稳定；消费组就绪前发布的消息会被从最后位置消费跳过，
            // 因此循环发布直到订阅方收到，正常情况数十毫秒内完成
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!source.Task.IsCompleted && DateTime.UtcNow < deadline)
            {
                await bus.PublishAsync(1);
                await bus.PublishAsync(2);
                await bus.PublishAsync(3);
                await Task.Delay(20);
            }

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

            // 同 PublishAndSubscribe_ClientId，循环发布直到消费组就绪并收到消息
            var deadline = DateTime.UtcNow.AddSeconds(10);
            var evtPublished = new OnlineEvent { UserName = "smartStone", Level = 9 };
            while (!source.Task.IsCompleted && DateTime.UtcNow < deadline)
            {
                await bus.PublishAsync(evtPublished);
                await Task.Delay(20);
            }

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
