using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest
{
    [Collection("Basic")]
    public class PubSubTests
    {
        private readonly FullRedis _redis;

        public PubSubTests()
        {
            var config = BasicTest.GetConfig();

            _redis = new FullRedis();
            _redis.Init(config);
            _redis.Log = XTrace.Log;

#if DEBUG
            _redis.ClientLog = XTrace.Log;
#endif
        }

        [Fact(DisplayName = "单通道定义")]
        public void Subscribe()
        {
            var pb = new PubSub(_redis, "pb_test");

            var source = new CancellationTokenSource(2_000);

            var count = 0;
            Task.Run(() => pb.SubscribeAsync((t, s) =>
            {
                count++;
                XTrace.WriteLine("Consume: [{0}] {1}", t, s);
            }, source.Token));

            Thread.Sleep(100);

            var rs = pb.Publish("test");
            Assert.Equal(1, rs);

            Thread.Sleep(100);
            pb.Publish("test2");

            Thread.Sleep(100);
            pb.Publish("test3");

            Thread.Sleep(100);

            Assert.Equal(3, count);
        }

        [Fact(DisplayName = "多通道定义")]
        public void Subscribes()
        {
            var pb = new PubSub(_redis, "pb_t1,pb_t2");

            var source = new CancellationTokenSource(2_000);

            var count = 0;
            Task.Run(() => pb.SubscribeAsync((t, s) =>
            {
                count++;
                XTrace.WriteLine("Consume: [{0}] {1}", t, s);
            }, source.Token));

            Thread.Sleep(100);

            var pb1 = new PubSub(_redis, "pb_t1");
            var rs = pb1.Publish("test");
            Assert.Equal(1, rs);

            Thread.Sleep(100);
            var pb2 = new PubSub(_redis, "pb_t2");
            pb2.Publish("test2");

            Thread.Sleep(100);
            pb2.Publish("test3");

            Thread.Sleep(100);

            Assert.Equal(3, count);
        }

        [Fact]
        public void Test1()
        {
            var pb = new PubSub(_redis, "pb_test1");
            var rs = pb.Publish("test");
            Assert.Equal(0, rs);
        }

        [Fact(DisplayName = "模式订阅-单模式")]
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

            Thread.Sleep(100);

            // 发布到匹配模式的频道
            var pb1 = new PubSub(_redis, "pb_ps_test");
            var rs = pb1.Publish("hello");
            Assert.Equal(1, rs);

            Thread.Sleep(100);
            pb1.Publish("world");

            Thread.Sleep(100);

            Assert.Equal(2, count);
        }

        [Fact(DisplayName = "PubSub自省-活跃频道")]
        public void PubSubChannels()
        {
            var pb = new PubSub(_redis, "pb_introspect");

            // 先订阅一个频道使其活跃
            var source = new CancellationTokenSource(2_000);
            Task.Run(() => pb.SubscribeAsync((t, s) => { }, source.Token));
            Thread.Sleep(100);

            var channels = pb.PubSubChannels("pb_introspect");
            Assert.NotNull(channels);
            Assert.Contains("pb_introspect", channels);
        }

        [Fact(DisplayName = "PubSub自省-订阅者数量")]
        public void PubSubNumSub()
        {
            var pb = new PubSub(_redis, "pb_numsub");

            // 先订阅
            var source = new CancellationTokenSource(2_000);
            Task.Run(() => pb.SubscribeAsync((t, s) => { }, source.Token));
            Thread.Sleep(100);

            var rs = pb.PubSubNumSub("pb_numsub");
            Assert.NotNull(rs);
        }

        [Fact(DisplayName = "PubSub自省-模式订阅数量")]
        public void PubSubNumPat()
        {
            var pb = new PubSub(_redis, "pb_numpat");

            var count = pb.PubSubNumPat();
            // 可能为0（无模式订阅）或正数
            Assert.True(count >= 0);
        }
    }
}