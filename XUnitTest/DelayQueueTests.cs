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
    public class DelayQueueTests
    {
        private readonly FullRedis _redis;

        public DelayQueueTests()
        {
            var rds = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            rds.Log = NewLife.Log.XTrace.Log;
#endif
            _redis = rds;
        }

        [Fact]
        public void Queue_Normal()
        {
            var key = "DelayQueue_normal";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetDelayQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 发现回滚
            var rcount = queue.RetryDeadAck();
            if (rcount > 0)
            {
                XTrace.WriteLine("回滚：{0}", rcount);

                Assert.Equal(rcount, queue.Count);
                var rcount2 = _redis.Remove(key);
                Assert.Equal(1, rcount2);
            }

            // 取出个数
            var count = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            queue.Add("1234", 3);
            queue.Add("abcd", 2);
            var vs = new[] { "新生命团队", "ABEF" };
            queue.Add(vs);

            // 对比个数
            var count2 = queue.Count;
            Assert.False(queue.IsEmpty);
            Assert.Equal(count + 2 + vs.Length, count2);

            // 取出来
            var v1 = queue.TakeOne();
            Assert.Equal("ABEF", v1);

            // 批量获取
            var vs2 = queue.Take(5).ToArray();
            Assert.Single(vs2);
            Assert.Equal("新生命团队", vs2[0]);

            // 延迟获取
            Thread.Sleep(2000);
            var vs3 = queue.Take(5).ToArray();
            Assert.Single(vs3);
            Assert.Equal("abcd", vs3[0]);

            // 延迟获取
            Thread.Sleep(1000);
            var vs4 = queue.Take(5).ToArray();
            Assert.Single(vs4);
            Assert.Equal("1234", vs4[0]);

            // 对比个数
            var count3 = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(count, count3);

            // 检查Ack队列
            var ackList = _redis.GetSortedSet(queue.AckKey);
            Assert.Equal(2 + vs.Length, ackList.Count);
        }

        [Fact]
        public void Queue_Block()
        {
            var key = "DelayQueue_block";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetDelayQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 回滚死信，然后清空
            var dead = queue.RetryDeadAck();
            if (dead > 0) _redis.Remove(key);

            // 取出个数
            var count = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "ABEF", "abcd", "新生命团队" };
            foreach (var item in vs)
            {
                queue.Add(item, 3);
            }

            // 对比个数
            var count2 = queue.Count;
            Assert.False(queue.IsEmpty);
            Assert.Equal(vs.Length, count2);

            // 取出来
            Assert.Equal(vs[0], queue.TakeOne());
            Assert.Equal(vs[1], queue.TakeOne());
            Assert.Equal(vs[2], queue.TakeOne());
            Assert.Equal(vs[3], queue.TakeOne());
            queue.Acknowledge(vs);

            // 延迟2秒生产消息
            ThreadPool.QueueUserWorkItem(s => { Thread.Sleep(2000); queue.Add("xxyy"); });
            var sw = Stopwatch.StartNew();
            var rs = queue.TakeOne(3);
            sw.Stop();
            Assert.Equal("xxyy", rs);
            Assert.True(sw.ElapsedMilliseconds >= 2000);
        }

        [Fact]
        public void Queue_NotEnough()
        {
            var key = "DelayQueue_not_enough";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetDelayQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

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
            var key = "DelayQueue_benchmark";

            var q = _redis.GetDelayQueue<String>(key);
            for (var i = 0; i < 1_000; i++)
            {
                var list = new List<String>();
                for (var j = 0; j < 20; j++)
                {
                    list.Add(Rand.NextString(32));
                }
                q.Add(list.ToArray());
            }

            Assert.Equal(1_000 * 20, q.Count);

            var count = 0;
            while (true)
            {
                var n = Rand.Next(1, 100);
                var list = q.Take(n).ToList();
                if (list.Count == 0) break;

                count += list.Count;
            }

            Assert.Equal(1_000 * 20, count);
        }

        [Fact]
        public void Queue_Benchmark_Mutilate()
        {
            var key = "DelayQueue_benchmark_mutilate";
            _redis.Remove(key);

            var q = _redis.GetDelayQueue<String>(key);
            for (var i = 0; i < 1_000; i++)
            {
                var list = new List<String>();
                for (var j = 0; j < 20; j++)
                {
                    list.Add(Rand.NextString(32));
                }
                q.Add(list.ToArray());
            }

            Assert.Equal(1_000 * 20, q.Count);

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

            Assert.Equal(1_000 * 20, count);
        }

        [Fact]
        public async void Queue_Async()
        {
            var key = "DelayQueue_Async";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetDelayQueue<String>(key);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            q.Add(vs);

            // 取出来
            Assert.Equal("1234", await q.TakeOneAsync(0));
            Assert.Equal("ABEF", await q.TakeOneAsync(0));
            Assert.Equal("abcd", await q.TakeOneAsync(0));
            Assert.Equal("新生命团队", await q.TakeOneAsync(0));

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
        public void GetNext()
        {
            var key = "DelayQueue_Async";

            // 删除已有
            _redis.Remove(key);
            var q = _redis.GetDelayQueue<String>(key);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            for (var i = 0; i < vs.Length; i++)
            {
                q.Add(vs[i], 10 - i + 1);
            }

            // 取出来
            var kv = q.GetNext();
            Assert.Equal("ABEF", kv.Key);
            Assert.Equal(DateTime.Now.ToInt() + 10 - 3 + 1, kv.Value);
        }
    }
}