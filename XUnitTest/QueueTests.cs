using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest
{
    public class QueueTests
    {
        private FullRedis _redis;

        public QueueTests()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [Fact]
        public void Queue_Normal()
        {
            var key = "Queue_normal";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            Assert.NotNull(q as RedisQueue<String>);

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
        public void Queue_Block()
        {
            var key = "Queue_block";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            Assert.NotNull(q as RedisQueue<String>);

            // 取出个数
            var count = q.Count;
            Assert.True(q.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            foreach (var item in vs)
            {
                q.Add(item);
            }

            // 对比个数
            var count2 = q.Count;
            Assert.False(q.IsEmpty);
            Assert.Equal(vs.Length, count2);

            // 取出来
            Assert.Equal(vs[0], q.TakeOne());
            Assert.Equal(vs[1], q.TakeOne());
            Assert.Equal(vs[2], q.TakeOne());
            Assert.Equal(vs[3], q.TakeOne());

            // 延迟2秒生产消息
            ThreadPool.QueueUserWorkItem(s => { Thread.Sleep(2000); q.Add("xxyy"); });
            var sw = Stopwatch.StartNew();
            var rs = q.TakeOne(3);
            sw.Stop();
            Assert.Equal("xxyy", rs);
            Assert.True(sw.ElapsedMilliseconds >= 2000);
        }

        [Fact]
        public void Queue_NotEnough()
        {
            var key = "Queue_not_enough";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            Assert.NotNull(q as RedisQueue<String>);

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

        [Fact]
        public void Queue_Benchmark()
        {
            var key = "Queue_benchmark";

            var q = _redis.GetQueue<String>(key);
            for (var i = 0; i < 1_000; i++)
            {
                var list = new List<String>();
                for (var j = 0; j < 100; j++)
                {
                    list.Add(Rand.NextString(32));
                }
                q.Add(list.ToArray());
            }

            Assert.Equal(1_000 * 100, q.Count);

            var count = 0;
            while (true)
            {
                var n = Rand.Next(1, 100);
                var list = q.Take(n).ToList();
                if (list.Count == 0) break;

                count += list.Count;
            }

            Assert.Equal(1_000 * 100, count);
        }

        [Fact]
        public void Queue_Benchmark_Mutilate()
        {
            var key = "Queue_benchmark_mutilate";
            _redis.Remove(key);

            var q = _redis.GetQueue<String>(key);
            for (var i = 0; i < 1_000; i++)
            {
                var list = new List<String>();
                for (var j = 0; j < 100; j++)
                {
                    list.Add(Rand.NextString(32));
                }
                q.Add(list.ToArray());
            }

            Assert.Equal(1_000 * 100, q.Count);

            var count = 0;
            var ths = new List<Task>();
            for (var i = 0; i < 16; i++)
            {
                ths.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        var n = Rand.Next(1, 100);
                        var list = q.Take(n).ToList();
                        if (list.Count == 0) break;

                        Interlocked.Add(ref count, list.Count);
                    }
                }));
            }

            Task.WaitAll(ths.ToArray());

            Assert.Equal(1_000 * 100, count);
        }

        [Fact]
        public async void Queue_Async()
        {
            var key = "Queue_Async";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            q.Add(vs);

            // 取出来
            Assert.Equal("1234", await q.TakeOneAsync(0));
            Assert.Equal("abcd", await q.TakeOneAsync(0));
            Assert.Equal("新生命团队", await q.TakeOneAsync(0));
            Assert.Equal("ABEF", await q.TakeOneAsync(0));

            // 空消息
            var sw = Stopwatch.StartNew();
            var rs = await q.TakeOneAsync(2);
            sw.Stop();
            Assert.Null(rs);
            Assert.True(sw.ElapsedMilliseconds >= 2000);

            // 延迟2秒生产消息
            ThreadPool.QueueUserWorkItem(s => { Thread.Sleep(2000); q.Add("xxyy"); });
            sw = Stopwatch.StartNew();
            rs = await q.TakeOneAsync(3);
            sw.Stop();
            Assert.Equal("xxyy", rs);
            Assert.True(sw.ElapsedMilliseconds >= 2000);
        }

        [Fact]
        public void Queue_NoAck()
        {
            var key = "Queue_NoAck";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetQueue<String>(key);

            //Assert.Throws<NotSupportedException>(() => q.Acknowledge(""));
            var rs = q.Acknowledge("");
            Assert.Equal(-1, rs);
        }
    }
}