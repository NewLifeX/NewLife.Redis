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
            queue.Delay = 1;

            // 发现回滚
            var rcount = queue.RollbackAllAck();
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
            queue.Acknowledge(v1);

            // 批量获取
            var vs2 = queue.Take(5).ToArray();
            Assert.Single(vs2);
            Assert.Equal("新生命团队", vs2[0]);
            queue.Acknowledge(vs2[0]);

            // 延迟获取
            Thread.Sleep(1000);
            var vs3 = queue.Take(5).ToArray();
            Assert.Single(vs3);
            Assert.Equal("abcd", vs3[0]);
            queue.Acknowledge(vs3[0]);

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
            var ackList = _redis.GetSortedSet<String>(queue.AckKey);
            Assert.Equal(2 + vs.Length - 1 - 1 - 1, ackList.Count);
        }

        [Fact]
        public void Queue_Block()
        {
            var key = "DelayQueue_block";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetDelayQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));
            queue.Delay = 1;

            // 回滚死信，然后清空
            var dead = queue.RollbackAllAck();
            if (dead > 0) _redis.Remove(key);

            // 取出个数
            var count = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "ABEF", "abcd", "新生命团队" };
            foreach (var item in vs)
            {
                queue.Add(item, 2);
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
            var queue = _redis.GetDelayQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));
            queue.Delay = 2;

            // 取出个数
            var count = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd" };
            queue.Add(vs);

            // 取出来
            var vs2 = queue.Take(3).ToArray();
            Assert.Equal(2, vs2.Length);
            Assert.Equal("1234", vs2[0]);
            Assert.Equal("abcd", vs2[1]);

            // 再取，这个时候已经没有元素
            var vs4 = queue.Take(3).ToArray();
            Assert.Empty(vs4);

            // 管道批量获取
            var vs3 = queue.Take(5).ToArray();
            Assert.Empty(vs3);

            // 对比个数
            var count3 = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(count, count3);
        }

        [Fact]
        public void Queue_Benchmark()
        {
            var key = "DelayQueue_benchmark";
            _redis.Remove(key);

            var queue = _redis.GetDelayQueue<String>(key);
            queue.Delay = 2;

            // 回滚死信，然后清空
            var dead = queue.RollbackAllAck();
            if (dead > 0) _redis.Remove(key);

            for (var i = 0; i < 1_000; i++)
            {
                var list = new List<String>();
                for (var j = 0; j < 20; j++)
                {
                    list.Add(Rand.NextString(32));
                }
                queue.Add(list.ToArray());
            }

            Assert.Equal(1_000 * 20, queue.Count);
            Thread.Sleep(queue.Delay * 1000);

            var count = 0;
            while (true)
            {
                var n = Rand.Next(1, 100);
                var list = queue.Take(n).ToList();
                if (list.Count == 0) break;

                var n2 = queue.Acknowledge(list.ToArray());
                Assert.Equal(list.Count, n2);

                count += list.Count;
            }

            Assert.Equal(1_000 * 20, count);
        }

        [Fact]
        public void Queue_Benchmark_Mutilate()
        {
            var key = "DelayQueue_benchmark_mutilate";
            _redis.Remove(key);

            var queue = _redis.GetDelayQueue<String>(key);
            queue.Delay = 2;

            // 回滚死信，然后清空
            var dead = queue.RollbackAllAck();
            if (dead > 0) _redis.Remove(key);

            for (var i = 0; i < 1_000; i++)
            {
                var list = new List<String>();
                for (var j = 0; j < 20; j++)
                {
                    list.Add(Rand.NextString(32));
                }
                queue.Add(list.ToArray());
            }

            Assert.Equal(1_000 * 20, queue.Count);
            Thread.Sleep(queue.Delay * 1000);

            var count = 0;
            var ths = new List<Task>();
            for (var i = 0; i < 16; i++)
            {
                ths.Add(Task.Run(() =>
                {
                    var queue2 = _redis.GetDelayQueue<String>(key);
                    while (true)
                    {
                        var n = Rand.Next(1, 100);
                        var list = queue2.Take(n).ToList();
                        if (list.Count == 0) break;

                        var n2 = queue2.Acknowledge(list.ToArray());
                        Assert.Equal(list.Count, n2);

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
            var queue = _redis.GetDelayQueue<String>(key);
            queue.Delay = 1;

            // 发现回滚
            var rcount = queue.RollbackAllAck();
            if (rcount > 0)
            {
                XTrace.WriteLine("回滚：{0}", rcount);

                Assert.Equal(rcount, queue.Count);
                var rcount2 = _redis.Remove(key);
                Assert.Equal(1, rcount2);
            }

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            queue.Add(vs);

            // 取出来
            Assert.Equal("1234", await queue.TakeOneAsync(0));
            Assert.Equal("ABEF", await queue.TakeOneAsync(0));
            Assert.Equal("abcd", await queue.TakeOneAsync(0));
            Assert.Equal("新生命团队", await queue.TakeOneAsync(0));

            // 空消息
            var sw = Stopwatch.StartNew();
            var rs = await queue.TakeOneAsync(2);
            sw.Stop();
            Assert.Null(rs);
            Assert.True(sw.ElapsedMilliseconds >= 2000);

            // 延迟2秒生产消息
            ThreadPool.QueueUserWorkItem(s => { Thread.Sleep(2000); queue.Add("xxyy"); });
            sw = Stopwatch.StartNew();
            rs = await queue.TakeOneAsync(3);
            sw.Stop();
            Assert.Equal("xxyy", rs);
            Assert.True(sw.ElapsedMilliseconds >= 2000);
        }

        [Fact]
        public async void NoAck()
        {
            var key = "DelayQueue_NoAck";

            // 删除已有
            _redis.Remove(key);
            var queue = new RedisDelayQueue<String>(_redis, key, false);
            queue.Delay = 2;

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            queue.Add(vs);

            // 取出来
            Assert.Equal("1234", await queue.TakeOneAsync(0));
            Assert.Equal("ABEF", await queue.TakeOneAsync(0));
            Assert.Equal("abcd", await queue.TakeOneAsync(0));
            Assert.Equal("新生命团队", await queue.TakeOneAsync(0));
        }

        [Fact]
        public async void TransferAsync()
        {
            var key = "transfer_delay";
            var key2 = "transfer";

            // 删除已有
            _redis.Remove(key);
            _redis.Remove(key2);
            var queue = _redis.GetDelayQueue<String>(key);
            var queue2 = _redis.GetReliableQueue<String>(key2);
            queue2.ClearAllAck();

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            queue.Delay = 2;
            queue.TransferInterval = 2;
            queue.Add(vs);

            // 开始转移
            XTrace.WriteLine("开始转移");
            var source = new CancellationTokenSource(3000);
            var task = queue.TransferAsync(queue2, null, source.Token);

            // 可信队列消费
            var v1 = await queue2.TakeOneAsync(-1);
            Assert.Null(v1);

            // 到期以后
            XTrace.WriteLine("可信队列阻塞消费");
            var sw = Stopwatch.StartNew();
            var v2 = await queue2.TakeOneAsync(3);
            sw.Stop();
            Assert.Equal("1234", v2);
            Assert.True(sw.ElapsedMilliseconds <= 3000);
            queue2.Acknowledge(v2);

            //await task;
        }
    }
}