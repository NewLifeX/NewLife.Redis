using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Caching;
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
            var key = "qkey_normal";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            var queue = q as RedisQueue<String>;
            Assert.NotNull(queue);

            // 取出个数
            var count = q.Count;
            Assert.True(q.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            q.Add(vs);

            // 对比个数
            var count2 = q.Count;
            Assert.False(q.IsEmpty);
            Assert.Equal(count + vs.Length, count2);

            // 取出来
            var vs2 = q.Take(2).ToArray();
            Assert.Equal(2, vs2.Length);
            Assert.Equal("1234", vs2[0]);
            Assert.Equal("abcd", vs2[1]);

            // 管道批量获取
            var q2 = q as RedisQueue<String>;
            q2.MinPipeline = 4;
            var vs3 = q.Take(5).ToArray();
            Assert.Equal(2, vs3.Length);
            Assert.Equal("新生命团队", vs3[0]);
            Assert.Equal("ABEF", vs3[1]);

            // 对比个数
            var count3 = q.Count;
            Assert.True(q.IsEmpty);
            Assert.Equal(count, count3);
        }

        [Fact]
        public void Queue_Strict()
        {
            var key = "qkey_strict";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            var queue = q as RedisQueue<String>;
            Assert.NotNull(queue);

            _redis.Remove(queue.AckKey);
            queue.Strict = true;
            //Assert.Equal(key + "_ack", queue.AckKey);
            Assert.StartsWith(key + ":Ack:", queue.AckKey);

            // 取出个数
            var count = q.Count;
            Assert.True(q.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            q.Add(vs);

            // 取出来
            var vs2 = q.Take(3).ToArray();
            Assert.Equal(3, vs2.Length);
            Assert.Equal("1234", vs2[0]);
            Assert.Equal("abcd", vs2[1]);
            Assert.Equal("新生命团队", vs2[2]);

            // 确认队列
            var q2 = _redis.GetQueue<String>(queue.AckKey) as RedisQueue<String>;
            Assert.Equal(vs2.Length, q2.Count);

            // 确认两个
            var rs = queue.Acknowledge(vs2.Take(2));
            Assert.Equal(2, rs);
            Assert.Equal(1, q2.Count);

            // 捞出来最后一个
            var vs3 = queue.TakeAck(3).ToArray();
            Assert.Equal(0, q2.Count);
            Assert.Single(vs3);
            Assert.Equal("新生命团队", vs3[0]);
        }

        [Fact]
        public void Queue_NotEnough()
        {
            var key = "qkey_not_enough";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            var queue = q as RedisQueue<String>;
            Assert.NotNull(queue);

            // 取出个数
            var count = q.Count;
            Assert.True(q.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd" };
            q.Add(vs);

            // 取出来
            var vs2 = q.Take(3).ToArray();
            Assert.Equal(2, vs2.Length);
            Assert.Equal("1234", vs2[0]);
            Assert.Equal("abcd", vs2[1]);

            // 再取，这个时候已经没有元素
            var vs4 = q.Take(3).ToArray();
            Assert.Empty(vs4);

            // 管道批量获取
            var vs3 = q.Take(5).ToArray();
            Assert.Empty(vs3);

            // 对比个数
            var count3 = q.Count;
            Assert.True(q.IsEmpty);
            Assert.Equal(count, count3);
        }

        /// <summary>AckKey独一无二，一百万个key测试</summary>
        [Fact]
        public void UniqueAckKey()
        {
            var key = "qkey_unique";

            var hash = new HashSet<String>();

            for (var i = 0; i < 1_000_000; i++)
            {
                var q = _redis.GetQueue<String>(key) as RedisQueue<String>;

                //Assert.DoesNotContain(q.AckKey, hash);
                Assert.False(hash.Contains(q.AckKey));

                hash.Add(q.AckKey);
            }
        }
    }
}