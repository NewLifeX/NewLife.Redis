using NewLife.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace XUnitTest
{
    public class QueueTests
    {
        private FullRedis _redis;

        public QueueTests()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 2);
        }

        [Fact]
        public void Queue_Normal()
        {
            var key = "qkey";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            var queue = q as RedisQueue<String>;
            Assert.NotNull(queue);

            // 取出个数
            var count = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            queue.Add(vs);

            // 对比个数
            var count2 = queue.Count;
            Assert.False(queue.IsEmpty);
            Assert.Equal(count + vs.Length, count2);

            // 取出来
            var vs2 = queue.Take(2).ToArray();
            Assert.Equal(2, vs2.Length);
            Assert.Equal(vs[0], vs2[0]);
            Assert.Equal(vs[1], vs2[1]);

            var vs3 = q.Take(2).ToArray();
            Assert.Equal(2, vs3.Length);
            Assert.Equal(vs[2], vs3[0]);
            Assert.Equal(vs[3], vs3[1]);

            // 对比个数
            var count3 = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(count, count3);
        }
    }
}