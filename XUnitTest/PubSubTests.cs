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
    public class PubSubTests
    {
        private readonly FullRedis _redis;

        public PubSubTests()
        {
            var config = BasicTest.GetConfig();

            _redis = new FullRedis();
            _redis.Init(config);
#if DEBUG
            _redis.Log = NewLife.Log.XTrace.Log;
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
    }
}