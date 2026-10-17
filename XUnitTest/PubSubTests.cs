using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest
{
    public class PubSubTests
    {
        private readonly FullRedis _redis;

        public PubSubTests()
        {
            var config = BasicTest.GetConfig();

            _redis = new FullRedis();
            _redis.Init(config);
            _redis.Log = XTrace.Log;

            if (BasicTest.EnableClientLog) _redis.ClientLog = XTrace.Log;
        }

        /// <summary>循环发布直到订阅方收到指定数量的消息。订阅命令异步生效，固定睡眠在高负载下不稳定</summary>
        private static void PublishUntil(Action publish, Func<Int32> received, Int32 expected, Int32 timeoutMs = 5_000)
        {
            var sw = Stopwatch.StartNew();
            while (received() < expected && sw.ElapsedMilliseconds < timeoutMs)
            {
                publish();
                Thread.Sleep(20);
            }

            SpinWait.SpinUntil(() => received() >= expected, 1_000);
        }

        [RedisFact(DisplayName = "单通道定义")]
        public void Subscribe()
        {
            var pb = new PubSub(_redis, "pb_test");

            var source = new CancellationTokenSource(2_000);

            var count = 0;
            Task.Run(() => pb.SubscribeAsync((t, s) =>
            {
                Interlocked.Increment(ref count);
                XTrace.WriteLine("Consume: [{0}] {1}", t, s);
            }, source.Token));

            PublishUntil(() => pb.Publish("test"), () => Volatile.Read(ref count), 3);

            Assert.True(count >= 3, $"收到消息数不足：{count}");
        }

        [RedisFact(DisplayName = "多通道定义")]
        public void Subscribes()
        {
            var pb = new PubSub(_redis, "pb_t1,pb_t2");

            var source = new CancellationTokenSource(2_000);

            var count = 0;
            Task.Run(() => pb.SubscribeAsync((t, s) =>
            {
                Interlocked.Increment(ref count);
                XTrace.WriteLine("Consume: [{0}] {1}", t, s);
            }, source.Token));

            var pb1 = new PubSub(_redis, "pb_t1");
            var pb2 = new PubSub(_redis, "pb_t2");
            PublishUntil(() =>
            {
                pb1.Publish("test");
                pb2.Publish("test2");
            }, () => Volatile.Read(ref count), 3);

            Assert.True(count >= 3, $"收到消息数不足：{count}");
        }

        [RedisFact]
        public void Test1()
        {
            var pb = new PubSub(_redis, "pb_test1");
            var rs = pb.Publish("test");
            Assert.Equal(0, rs);
        }

        [RedisFact(DisplayName = "模式订阅-单模式")]
        public void PSubscribe()
        {
            var pb = new PubSub(_redis, "pb_ps_*");

            var source = new CancellationTokenSource(2_000);

            var count = 0;
            Task.Run(() => pb.PSubscribeAsync((pattern, channel, msg) =>
            {
                count++;
                XTrace.WriteLine("Consume: [{0}] [{1}] {2}", pattern, channel, msg);
            }, source.Token));

            // 模式订阅无法通过服务端自省精确判断就绪，改为循环发布直到收到
            var pb1 = new PubSub(_redis, "pb_ps_test");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (count < 2 && sw.ElapsedMilliseconds < 5_000)
            {
                pb1.Publish("hello");
                Thread.Sleep(20);
            }
            SpinWait.SpinUntil(() => count >= 2, 1_000);

            Assert.True(count >= 2, $"收到消息数不足：{count}");
        }

        [RedisFact(DisplayName = "PubSub自省-活跃频道")]
        public void PubSubChannels()
        {
            var pb = new PubSub(_redis, "pb_introspect");

            // 先订阅一个频道使其活跃
            var count = 0;
            var source = new CancellationTokenSource(2_000);
            Task.Run(() => pb.SubscribeAsync((t, s) => Interlocked.Increment(ref count), source.Token));
            PublishUntil(() => pb.Publish("probe"), () => Volatile.Read(ref count), 1);

            var channels = pb.PubSubChannels("pb_introspect");
            Assert.NotNull(channels);
            Assert.Contains("pb_introspect", channels);
        }

        [RedisFact(DisplayName = "PubSub自省-订阅者数量")]
        public void PubSubNumSub()
        {
            var pb = new PubSub(_redis, "pb_numsub");

            // 先订阅
            var count = 0;
            var source = new CancellationTokenSource(2_000);
            Task.Run(() => pb.SubscribeAsync((t, s) => Interlocked.Increment(ref count), source.Token));
            PublishUntil(() => pb.Publish("probe"), () => Volatile.Read(ref count), 1);

            var rs = pb.PubSubNumSub("pb_numsub");
            Assert.NotNull(rs);
        }

        [RedisFact(DisplayName = "PubSub自省-模式订阅数量")]
        public void PubSubNumPat()
        {
            var pb = new PubSub(_redis, "pb_numpat");

            var count = pb.PubSubNumPat();
            // 可能为0（无模式订阅）或正数
            Assert.True(count >= 0);
        }

        [RedisFact(DisplayName = "分片订阅-单频道")]
        public void SSubscribe()
        {
            var pb = new PubSub(_redis, "pb_shard");

            var source = new CancellationTokenSource(2_000);

            var count = 0;
            Task.Run(() => pb.SSubscribeAsync((channel, msg) =>
            {
                count++;
                XTrace.WriteLine("ShardConsume: [{0}] {1}", channel, msg);
            }, source.Token));

            // 分片订阅没有对应的就绪自省命令，循环发布直到收到
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (count < 1 && sw.ElapsedMilliseconds < 5_000)
            {
                pb.SPublish("shard_msg");
                Thread.Sleep(20);
            }
            SpinWait.SpinUntil(() => count >= 1, 1_000);

            Assert.True(count >= 1, $"收到消息数不足：{count}");
        }
    }
}