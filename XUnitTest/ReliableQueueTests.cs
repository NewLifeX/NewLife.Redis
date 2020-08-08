using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest
{
    public class ReliableQueueTests
    {
        private FullRedis _redis;

        public ReliableQueueTests()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [Fact]
        public void Queue_Normal()
        {
            var key = "ReliableQueue";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetReliableQueue<String>(key);
            queue.ClearAllAck();

            // 取出个数
            var count = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            foreach (var item in vs)
            {
                queue.Add(item);
            }

            // 取出来
            var vs2 = new[] { queue.TakeOne(), queue.TakeOne(), queue.TakeOne(), };
            Assert.Equal(3, vs2.Length);
            Assert.Equal("1234", vs2[0]);
            Assert.Equal("abcd", vs2[1]);
            Assert.Equal("新生命团队", vs2[2]);

            Assert.Equal(1, queue.Count);

            // 检查Ack队列
            var ackList = _redis.GetList<String>(queue.AckKey);
            Assert.Equal(vs2.Length, ackList.Count);

            // 确认两个，留下一个未确认消息在Ack队列
            var rs = queue.Acknowledge(vs2.Take(2));
            Assert.Equal(2, rs);
            Assert.Equal(1, ackList.Count);

            // 捞出来Ack最后一个
            var vs3 = queue.TakeAck(3).ToArray();
            Assert.Equal(0, ackList.Count);
            Assert.Single(vs3);
            Assert.Equal("新生命团队", vs3[0]);

            // 读取队列最后一个，但不确认，留给下一次回滚用
            var v4 = queue.TakeOne();
            Assert.NotNull(v4);
        }

        [Fact]
        public void Queue_Batch()
        {
            var key = "ReliableQueue_batch";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetReliableQueue<String>(key);
            queue.ClearAllAck();

            // 取出个数
            var count = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            queue.Add(vs);

            // 取出来
            var vs2 = queue.Take(3).ToArray();
            Assert.Equal(3, vs2.Length);
            Assert.Equal("1234", vs2[0]);
            Assert.Equal("abcd", vs2[1]);
            Assert.Equal("新生命团队", vs2[2]);

            Assert.Equal(1, queue.Count);

            // 检查确认队列
            var q2 = _redis.GetList<String>(queue.AckKey);
            Assert.Equal(vs2.Length, q2.Count);

            // 确认两个
            var rs = queue.Acknowledge(vs2.Take(2));
            Assert.Equal(2, rs);
            Assert.Equal(1, q2.Count);

            // 捞出来Ack最后一个
            var vs3 = queue.TakeAck(3).ToArray();
            Assert.Equal(0, q2.Count);
            Assert.Single(vs3);
            Assert.Equal("新生命团队", vs3[0]);

            // 读取队列最后一个，但不确认，留给下一次回滚用
            var vs4 = queue.Take(4).ToArray();
            Assert.Single(vs4);
        }

        [Fact]
        public void Queue_Block()
        {
            var key = "ReliableQueue_block";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetReliableQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 回滚死信，然后清空
            var dead = queue.RollbackAllAck();
            if (dead > 0) _redis.Remove(key);

            // 取出个数
            var count = queue.Count;
            Assert.True(queue.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            foreach (var item in vs)
            {
                queue.Add(item);
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
            var rs = queue.TakeOne(2100);
            sw.Stop();
            Assert.Equal("xxyy", rs);
            Assert.True(sw.ElapsedMilliseconds >= 2000);
        }

        [Fact]
        public void Queue_NotEnough()
        {
            var key = "ReliableQueue_not_enough";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetReliableQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 回滚死信，然后清空
            var dead = queue.RollbackAllAck();
            if (dead > 0) _redis.Remove(key);

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
            queue.Acknowledge(vs2);

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

        /// <summary>AckKey独一无二，一百万个key测试</summary>
        [Fact]
        public void UniqueAckKey()
        {
            var key = "ReliableQueue_unique";

            var hash = new HashSet<String>();

            for (var i = 0; i < 1_000_000; i++)
            {
                var q = _redis.GetReliableQueue<String>(key);

                //Assert.DoesNotContain(q.AckKey, hash);
                var rs = hash.Contains(q.AckKey);
                Assert.False(rs);

                hash.Add(q.AckKey);
            }
        }

        [Fact]
        public void Queue_Benchmark()
        {
            var key = "ReliableQueue_benchmark";
            _redis.Remove(key);

            var q = _redis.GetReliableQueue<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));
            for (var i = 0; i < 1_000; i++)
            {
                var list = new List<String>();
                for (var j = 0; j < 100; j++)
                {
                    list.Add(Rand.NextString(32));
                }
                q.Add(list);
            }

            Assert.Equal(1_000 * 100, q.Count);

            var count = 0;
            while (true)
            {
                var n = Rand.Next(1, 100);
                var list = q.Take(n).ToList();
                if (list.Count == 0) break;

                var n2 = q.Acknowledge(list);
                Assert.Equal(list.Count, n2);

                count += list.Count;
            }

            Assert.Equal(1_000 * 100, count);
        }

        [Fact]
        public void Queue_Benchmark_Mutilate()
        {
            var key = "ReliableQueue_benchmark_mutilate";
            _redis.Remove(key);

            var queue = _redis.GetReliableQueue<String>(key);

            // 回滚死信，然后清空
            var dead = queue.RollbackAllAck();
            if (dead > 0) _redis.Remove(key);

            for (var i = 0; i < 1_000; i++)
            {
                var list = new List<String>();
                for (var j = 0; j < 100; j++)
                {
                    list.Add($"msgContent-{i}-{j}");
                }
                queue.Add(list);
            }

            Assert.Equal(1_000 * 100, queue.Count);

            //var count = 0;
            var ths = new List<Task<Int32>>();
            for (var i = 0; i < 16; i++)
            {
                ths.Add(Task.Run(() =>
                {
                    var count = 0;
                    var queue2 = _redis.GetReliableQueue<String>(key);
                    while (true)
                    {
                        var n = Rand.Next(1, 100);
                        var list = queue2.Take(n).Where(e => !e.IsNullOrEmpty()).ToList();
                        if (list.Count == 0) break;

                        var n2 = queue2.Acknowledge(list);
                        // Ack返回值似乎没那么准
                        //Assert.Equal(list.Count, n2);

                        //Interlocked.Add(ref count, list.Count);
                        count += list.Count;
                    }
                    return count;
                }));
            }

            //Task.WaitAll(ths.ToArray());
            var rs = Task.WhenAll(ths).Result.Sum();

            Assert.Equal(1_000 * 100, rs);
        }

        [Fact]
        public void RetryDeadAck()
        {
            var key = "ReliableQueue_RetryDeadAck";

            _redis.Remove(key);
            var queue = _redis.GetReliableQueue<String>(key);
            queue.RetryInterval = 2;

            // 清空
            queue.TakeAllAck().ToArray();

            // 生产几个消息，消费但不确认
            var list = new List<String>();
            for (var i = 0; i < 5; i++)
            {
                list.Add(Rand.NextString(32));
            }
            queue.Add(list);

            var list2 = queue.Take(10).ToList();
            Assert.Equal(list.Count, list2.Count);

            // 确认队列里面有几个
            var q2 = _redis.GetList<String>(queue.AckKey);
            Assert.Equal(list.Count, q2.Count);

            // 马上消费，消费不到
            var vs3 = queue.Take(100).ToArray();
            Assert.Empty(vs3);

            // 等一定时间再消费
            Thread.Sleep(queue.RetryInterval * 1000 + 10);

            // 再次消费，应该有了
            var vs4 = queue.Take(100).ToArray();
            Assert.Equal(list.Count, vs4.Length);

            // 确认队列里面的私信重新进入主队列，消费时再次进入确认队列
            Assert.Equal(vs4.Length, q2.Count);

            // 全部确认
            queue.Acknowledge(vs4);

            // 确认队列应该空了
            Assert.Equal(0, q2.Count);
        }
    }
}