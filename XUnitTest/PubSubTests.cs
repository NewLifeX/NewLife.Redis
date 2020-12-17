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
            var rds = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            rds.Log = NewLife.Log.XTrace.Log;
#endif
            _redis = rds;
        }

        [Fact]
        public void Subscribe()
        {
            var pb = new PubSub(_redis, "pb_test1");

            var source = new CancellationTokenSource();

            Task.Run(() => pb.SubscribeAsync((m, t, s) => XTrace.WriteLine("Consume: {0}[{1}] {2}", m, t, s), source.Token));

            Thread.Sleep(100);

            var rs = pb.Publish("test");
            Assert.Equal(1, rs);

            Thread.Sleep(100);
        }

        [Fact]
        public void Test1()
        {
            var pb = new PubSub(_redis, "pb_test1");
            var rs = pb.Publish("test");
            Assert.Equal(1, rs);
        }
    }
}