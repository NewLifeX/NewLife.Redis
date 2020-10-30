using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Reflection;
#if !NET40
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Caching
{
    /// <summary>Redis延迟队列</summary>
    /// <remarks>
    /// 延迟Redis队列，每次生产操作1次Redis，消费操作4次Redis。
    /// </remarks>
    public class RedisDelayQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>用于确认的列表</summary>
        public String AckKey { get; }

        /// <summary>重新处理确认队列中死信的间隔。默认60s</summary>
        public Int32 RetryInterval { get; set; } = 60;

        /// <summary>转移延迟消息到主队列的间隔。默认10s</summary>
        public Int32 TransferInterval { get; set; } = 10;

        /// <summary>个数</summary>
        public Int32 Count => _sort?.Count ?? 0;

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        /// <summary>默认延迟时间。默认60秒</summary>
        public Int32 Delay { get; set; } = 60;

        private readonly RedisSortedSet<T> _sort;
        private readonly RedisSortedSet<T> _ack;
        #endregion

        #region 实例化
        /// <summary>实例化延迟队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        /// <param name="useAck"></param>
        public RedisDelayQueue(Redis redis, String key, Boolean useAck = true) : base(redis, key)
        {
            _sort = new RedisSortedSet<T>(redis, key);

            if (useAck)
            {
                AckKey = $"{key}:Ack";
                _ack = new RedisSortedSet<T>(redis, AckKey);
            }
        }
        #endregion

        #region 核心方法
        /// <summary>添加延迟消息</summary>
        /// <param name="value"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public Int32 Add(T value, Int32 delay)
        {
            using var span = Redis.Tracer?.NewSpan($"redismq:AddDelay:{Key}", value);

            return _sort.Add(value, DateTime.Now.ToInt() + delay);
        }

        /// <summary>批量生产</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(params T[] values)
        {
            if (values == null || values.Length == 0) return 0;

            using var span = Redis.Tracer?.NewSpan($"redismq:AddDelay:{Key}", values);

            return _sort.Add(values, DateTime.Now.ToInt() + Delay);
        }

        /// <summary>删除项</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Int32 Remove(T value) => _sort.Remove(value);

        /// <summary>获取一个</summary>
        /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0)
        {
            RetryAck();

            // 最长等待
            if (timeout == 0) timeout = 60;

            while (true)
            {
                var score = DateTime.Now.ToInt();
                var rs = _sort.RangeByScore(0, score, 0, 1);
                if (rs != null && rs.Length > 0 && TryPop(rs[0])) return rs[0];

                // 是否需要等待
                if (timeout <= 0) break;

                Thread.Sleep(1000);
                timeout--;
            }

            return default;
        }

        /// <summary>异步获取一个</summary>
        /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public async Task<T> TakeOneAsync(Int32 timeout = 0, CancellationToken cancellationToken = default)
        {
            RetryAck();

            // 最长等待
            if (timeout == 0) timeout = 60;

            while (true)
            {
                var score = DateTime.Now.ToInt();
                var rs = await _sort.RangeByScoreAsync(0, score, 0, 1, cancellationToken);
                if (rs != null && rs.Length > 0 && TryPop(rs[0])) return rs[0];

                // 是否需要等待
                if (timeout <= 0) break;

                await TaskEx.Delay(1000, cancellationToken);
                timeout--;
            }

            return default;
        }

        /// <summary>异步消费获取</summary>
        /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
        /// <returns></returns>
        Task<T> IProducerConsumer<T>.TakeOneAsync(Int32 timeout) => TakeOneAsync(timeout, default);

        /// <summary>获取一批</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            RetryAck();

            var score = DateTime.Now.ToInt();
            var rs = _sort.RangeByScore(0, score, 0, count);
            if (rs == null || rs.Length == 0) yield break;

            foreach (var item in rs)
            {
                // 争夺消费
                if (TryPop(item)) yield return item;
            }
        }

        /// <summary>争夺消费，只有一个线程能够成功删除，作为抢到的标志。同时备份到Ack队列</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private Boolean TryPop(T value)
        {
            if (_ack != null)
            {
                // 先备份，再删除。备份到Ack队列
                var score = DateTime.Now.ToInt() + RetryInterval;
                _ack.Add(value, score);
            }

            // 删除作为抢夺
            return _sort.Remove(value) > 0;
        }

        /// <summary>确认删除</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Int32 Acknowledge(params T[] keys) => _ack?.Remove(keys) ?? -1;

        /// <summary>确认删除</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        Int32 IProducerConsumer<T>.Acknowledge(params String[] keys) => _ack?.Remove(keys.Select(e => e.ChangeType<T>()).ToArray()) ?? -1;
        #endregion

        #region 死信处理
        /// <summary>回滚指定AckKey内的消息到Key</summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private List<T> RollbackAck(DateTime time)
        {
            if (_ack == null) return null;

            // 消费所有数据
            var score = time.ToInt();
            var list = new List<T>();
            while (true)
            {
                var rs = _ack.RangeByScore(0, score, 0, 100);
                if (rs == null || rs.Length == 0) break;

                // 加入原始队列
                _sort.Add(rs, score);
                _ack.Remove(rs);

                list.AddRange(rs);

                if (rs.Length < 100) break;
            }

            return list;
        }

        private DateTime _nextRetry;
        /// <summary>处理未确认的死信，重新放入队列</summary>
        private Int32 RetryAck()
        {
            if (_ack == null) return 0;

            var now = DateTime.Now;
            // 一定间隔处理死信
            if (_nextRetry < now)
            {
                _nextRetry = now.AddSeconds(RetryInterval);

                // 拿到死信，重新放入队列
                var list = RollbackAck(now);
                foreach (var item in list)
                {
                    XTrace.WriteLine("定时回滚死信：{0}", item);
                }
                return list.Count;
            }

            return 0;
        }

        /// <summary>全局回滚死信，一般由单一线程执行，避免干扰处理中数据</summary>
        /// <returns></returns>
        public Int32 RollbackAllAck() => RollbackAck(DateTime.Today.AddDays(1)).Count;
        #endregion

        #region 消息交换
        /// <summary>异步转移消息，已到期消息转移到目标队列</summary>
        /// <param name="queue">队列</param>
        /// <param name="onException">异常处理</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public async Task TransferAsync(IProducerConsumer<T> queue, Action<Exception> onException = null, CancellationToken cancellationToken = default)
        {
            // 大循环之前，打断性能追踪调用链
            DefaultSpan.Current = null;

            // 超时时间，用于阻塞等待
            //var timeout = Redis.Timeout / 1000 - 1;
            var topic = Key;
            var tracer = Redis.Tracer;

            while (!cancellationToken.IsCancellationRequested)
            {
                ISpan span = null;
                try
                {
                    // 异步阻塞消费
                    var score = DateTime.Now.ToInt();
                    var msgs = await _sort.RangeByScoreAsync(0, score, 0, 10, cancellationToken);
                    if (msgs != null && msgs.Length > 0)
                    {
                        // 删除消息后直接进入目标队列，无需进入Ack
                        span = tracer?.NewSpan($"redismq:Transfer:{topic}", msgs);

                        // 逐个删除，多线程争夺可能失败
                        var list = new List<T>();
                        for (var i = 0; i < msgs.Length; i++)
                        {
                            if (Remove(msgs[i]) > 0) list.Add(msgs[i]);
                        }

                        // 转移消息
                        if (list.Count > 0) queue.Add(list.ToArray());
                    }
                    else
                    {
                        // 没有消息，歇一会
                        await TaskEx.Delay(TransferInterval * 1000, cancellationToken);
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch (Exception ex)
                {
                    span?.SetError(ex, null);

                    onException?.Invoke(ex);
                }
                finally
                {
                    span?.Dispose();
                }
            }
        }
        #endregion
    }
}