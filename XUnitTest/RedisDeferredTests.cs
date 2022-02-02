using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest
{
    public class RedisDeferredTests
    {
        private readonly FullRedis _redis;

        public RedisDeferredTests()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 5);
            _redis.Expire = 10 * 60;
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [Fact]
        public void Test1()
        {
            String[] keys = null;
            IDictionary<String, Int32> vs = null;

            using var st = new RedisDeferred(_redis, "SiteDayStat");
            st.Period = 2000;
            st.Process += (s, e) =>
            {
                keys = e.Keys;

                // 模拟业务操作，取回数据，然后删除
                var ks = new[] { $"{keys[0]}:Total", $"{keys[0]}:Error", $"{keys[0]}:Cost" };
                vs = _redis.GetAll<Int32>(ks);
                var rs = _redis.Remove(ks);
                Assert.Equal(3, rs);
            };

            // 计算key
            var date = DateTime.Today;
            var key = $"SiteDayStat:2743-{date:MMdd}";

            // 累加统计
            var vv = new[] { Rand.Next(10000), Rand.Next(10000), Rand.Next(10, 1000) };
            _redis.Increment($"{key}:Total", vv[0]);
            _redis.Increment($"{key}:Error", vv[1]);
            _redis.Increment($"{key}:Cost", vv[2]);

            // 变动key进入延迟队列
            st.Add(key);

            Thread.Sleep(3_000);

            Assert.Single(keys);
            Assert.Equal(key, keys[0]);

            Assert.Equal(3, vs.Count);
            Assert.Equal(vv[0], vs[$"{key}:Total"]);
            Assert.Equal(vv[1], vs[$"{key}:Error"]);
            Assert.Equal(vv[2], vs[$"{key}:Cost"]);
        }
    }
}