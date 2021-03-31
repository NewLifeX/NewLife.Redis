using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest
{
    public class RedisStatTests
    {
        private readonly FullRedis _redis;

        public RedisStatTests()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 5);
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [Fact]
        public void Test1()
        {
            var st = new RedisStat(_redis, "Site")
            {
                OnSave = OnSave
            };

            // 计算key
            var date = DateTime.Today;
            var key = $"2743-{date:MMdd}";

            // 累加统计
            st.Increment(key, "Total", 1);
            if (Rand.Next(2) > 0)
                st.Increment(key, "Error", 1);
            st.Increment(key, "Cost", Rand.Next(10, 100));

            // 变动key进入延迟队列
            st.AddDelayQueue(key, 2);

            Thread.Sleep(13_000);
        }

        private void OnSave(String key, IDictionary<String, Int32> data)
        {
            XTrace.WriteLine("保存：{0}，数据：{1}", key, data.ToJson());
        }
    }
}