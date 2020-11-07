using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching.Models;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
#if !NET40
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Caching
{
    /// <summary>可靠Redis队列，左进右出</summary>
    /// <remarks>
    /// 严格模式下消费，弹出消息的同时插入Ack队列，消费者处理成功后将从ACK队列删除该消息，若处理失败，则将延迟消费Ack消息；
    /// 
    /// 可信队列对象不是线程安全，要求每个线程独享队列对象。
    /// 为了让严格模式支持多线程消费，确认队列AckKey构造为 Key:Ack:Rand16 的格式，每一个消费者都将有自己完全独一无二的确认队列。
    /// 消费者每30秒（RetryInterval）清理一次确认队列的死信（未确认消息），重新投入主队列。
    /// 应用异常退出时，可能产生一些死信，在应用启动首次消费时通过TakeAllAck消费清理所有Ack队列。
    /// 由于引入状态队列，清理不活跃消费者时，不会影响正常消费者。
    /// 
    /// 设计要点：
    /// 1，消费时，RPOPLPUSH从Key弹出并备份到AckKey，消息处理完成后，再从AckKey删除
    /// 2，AckKey设计为Key:Ack:ukey，ukey=Rand16，让每个实例都有专属的Ack确认队列
    /// 3，消费时，每60秒更新一次状态到Key:Status:ukey，表明ukey还在消费
    /// 4，全局定期扫描Key:Status:ukey，若不活跃，回滚它的Ack消息
    /// 
    /// 消费者要慎重处理错误消息，有可能某条消息一直处理失败，如果未确认，队列会反复把消息送回主队列。
    /// 建议用户自己处理并确认消费，通过消息体或者redisKey计数。
    /// 
    /// 高级队列技巧：
    /// 1，按kv写入消息体，然后key作为消息键写入队列并消费，成功消费后从kv删除；
    /// 2，消息键key自定义，随时可以查看或删除消息体，也可以避免重复生产；
    /// 3，Redis队列确保至少消费一次，消息体和消息键分离后，可以做到有且仅有一次，若有二次消费，再也拿不到数据内容；
    /// 4，同一个消息被重复生产时，尽管队列里面有两条消息键，但由于消息键相同，消息体只有一份，从而避免重复消费；
    /// 
    /// 可信Redis队列，每次生产操作1次Redis，消费操作2次Redis；
    /// 高级Redis队列，每次生产操作3次Redis，消费操作4次Redis；
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class RedisReliableQueue<T> : RedisBase, IProducerConsumer<T>, IDisposable
    {
        #region 属性
        /// <summary>用于确认的列表</summary>
        public String AckKey { get; set; }

        /// <summary>重新处理确认队列中死信的间隔。默认60s</summary>
        public Int32 RetryInterval { get; set; } = 60;

        /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
        public Int32 MinPipeline { get; set; } = 3;

        /// <summary>消息体过期时间，仅用于高级消息生产和消费，默认10*24*3600</summary>
        public Int32 BodyExpire { get; set; } = 10 * 24 * 3600;

        /// <summary>消息去重的时间。该时间内不处理相同msgId的消息，默认0s不启用</summary>
        public Int32 DuplicateExpire { get; set; }

        /// <summary>是否在消息报文中自动注入TraceId。TraceId用于跨应用在生产者和消费者之间建立调用链，默认true</summary>
        public Boolean AttachTraceId { get; set; } = true;

        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("LLEN", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        /// <summary>消费状态</summary>
        public RedisQueueStatus Status => _Status;

        private readonly String _Key;
        private readonly String _StatusKey;
        private readonly RedisQueueStatus _Status;

        private RedisDelayQueue<T> _delay;
        private CancellationTokenSource _source;
        private Task _delayTask;
        #endregion

        #region 构造
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisReliableQueue(Redis redis, String key) : base(redis, key)
        {
            _Key = key;
            _Status = CreateStatus();
            AckKey = $"{key}:Ack:{_Status.Key}";
            _StatusKey = $"{key}:Status:{_Status.Key}";
        }

        /// <summary>析构</summary>
        ~RedisReliableQueue() => Dispose(false);

        /// <summary>释放</summary>
        public void Dispose() => Dispose(true);

        /// <summary>释放</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (_delay != null)
            {
                _delay = null;
                _delayTask = null;
                _source?.Cancel();
            }
        }
        #endregion

        #region 核心方法
        /// <summary>批量生产添加</summary>
        /// <param name="values">消息集合</param>
        /// <returns></returns>
        public Int32 Add(params T[] values)
        {
            if (values == null || values.Length == 0) return 0;

            using var span = Redis.Tracer?.NewSpan($"redismq:AddReliable:{Key}", values);

            var args = new List<Object> { Key };
            foreach (var item in values)
            {
                if (AttachTraceId)
                    args.Add(Redis.AttachTraceId(item));
                else
                    args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("LPUSH", args.ToArray()), true);
        }

        /// <summary>消费获取，从Key弹出并备份到AckKey，支持阻塞</summary>
        /// <remarks>假定前面获取的消息已经确认，因该方法内部可能回滚确认队列，避免误杀</remarks>
        /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0)
        {
            RetryAck();

            var rs = timeout >= 0 ?
                Execute(rc => rc.Execute<T>("BRPOPLPUSH", Key, AckKey, timeout), true) :
                Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);

            if (rs != null) _Status.Consumes++;

            return rs;
        }

        /// <summary>异步消费获取</summary>
        /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public async Task<T> TakeOneAsync(Int32 timeout = 0, CancellationToken cancellationToken = default)
        {
#if NET4
            throw new NotSupportedException();
#else
            RetryAck();

            var rs = (timeout < 0) ?
                await ExecuteAsync(rc => rc.ExecuteAsync<T>("RPOPLPUSH", new Object[] { Key, AckKey }, cancellationToken), true) :
                await ExecuteAsync(rc => rc.ExecuteAsync<T>("BRPOPLPUSH", new Object[] { Key, AckKey, timeout }, cancellationToken), true);

            if (rs != null) _Status.Consumes++;

            return rs;
#endif
        }

        /// <summary>异步消费获取</summary>
        /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
        /// <returns></returns>
        Task<T> IProducerConsumer<T>.TakeOneAsync(Int32 timeout) => TakeOneAsync(timeout, default);

        /// <summary>批量消费获取，从Key弹出并备份到AckKey</summary>
        /// <remarks>假定前面获取的消息已经确认，因该方法内部可能回滚确认队列，避免误杀</remarks>
        /// <param name="count">要消费的消息个数</param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            RetryAck();

            // 借助管道支持批量获取
            if (count >= MinPipeline)
            {
                var rds = Redis;
                rds.StartPipeline();

                for (var i = 0; i < count; i++)
                {
                    Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);
                }

                var rs = rds.StopPipeline(true);
                foreach (var item in rs)
                {
                    if (!Equals(item, default(T)))
                    {
                        _Status.Consumes++;
                        yield return (T)item;
                    }
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var value = Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);
                    if (Equals(value, default(T))) break;

                    _Status.Consumes++;
                    yield return value;
                }
            }
        }

        /// <summary>确认消费，从AckKey中删除</summary>
        /// <param name="keys"></param>
        public Int32 Acknowledge(params String[] keys)
        {
            var rs = 0;

            _Status.Acks += keys.Length;

            // 管道支持
            if (keys.Count() >= MinPipeline)
            {
                var rds = Redis;
                rds.StartPipeline();

                foreach (var item in keys)
                {
                    Execute(r => r.Execute<Int32>("LREM", AckKey, 1, item), true);
                }

                var rs2 = rds.StopPipeline(true);
                foreach (var item in rs2)
                {
                    rs += (Int32)item;
                }
            }
            else
            {
                foreach (var item in keys)
                {
                    rs += Execute(r => r.Execute<Int32>("LREM", AckKey, 1, item), true);
                }
            }

            return rs;
        }
        #endregion

        #region 高级队列
        /// <summary>初始化延迟队列功能。生产者自动初始化，消费者最好能够按队列初始化一次</summary>
        /// <remarks>
        /// 该功能是附加功能，需要消费者主动调用，每个队列的多消费者开一个即可。
        /// 核心工作是启动延迟队列的TransferAsync大循环，每个进程内按队列开一个最合适，多了没有用反而形成争夺。
        /// </remarks>
        public RedisDelayQueue<T> InitDelay()
        {
            if (_delay == null)
            {
                lock (this)
                {
                    if (_delay == null)
                    {
                        _delay = new RedisDelayQueue<T>(Redis, $"{Key}:Delay", false);
                    }
                }
            }
            if (_delayTask == null || _delayTask.IsCompleted)
            {
                lock (this)
                {
                    if (_delayTask == null || _delayTask.IsCompleted)
                    {
                        _source = new CancellationTokenSource();
                        _delayTask = TaskEx.Run(() => _delay.TransferAsync(this, null, _source.Token));
                    }
                }
            }

            return _delay;
        }

        /// <summary>添加延迟消息</summary>
        /// <param name="value"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public Int32 AddDelay(T value, Int32 delay)
        {
            InitDelay();

            return _delay.Add(value, delay);
        }

        ///// <summary>添加延迟消息</summary>
        ///// <param name="value"></param>
        ///// <param name="delay"></param>
        ///// <returns></returns>
        //[Obsolete("=>AddDelay")]
        //public Int32 Add(T value, Int32 delay) => AddDelay(value, delay);

        /// <summary>高级生产消息。消息体和消息键分离，业务层指定消息键，可随时查看或删除，同时避免重复生产</summary>
        /// <remarks>
        /// Publish 必须跟 ConsumeAsync 配对使用。
        /// </remarks>
        /// <param name="messages"></param>
        /// <returns></returns>
        public Int32 Publish(IDictionary<String, T> messages)
        {
            // 消息体写入kv
            Redis.SetAll(messages, BodyExpire);

            // 消息键写入队列
            var args = new List<Object> { Key };
            foreach (var item in messages)
            {
                args.Add(item.Key);
            }
            var rs = Execute(rc => rc.Execute<Int32>("LPUSH", args.ToArray()), true);

            return rs;
        }

        /// <summary>高级消费消息。消息处理成功后，自动确认并删除消息体</summary>
        /// <remarks>
        /// Publish 必须跟 ConsumeAsync 配对使用。
        /// </remarks>
        /// <param name="func"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<TResult> ConsumeAsync<TResult>(Func<T, Task<TResult>> func, Int32 timeout = 0)
        {
#if NET4
            throw new NotSupportedException();
#else
            RetryAck();

            // 取出消息键
            var msgId = (timeout < 0) ?
                await ExecuteAsync(rc => rc.ExecuteAsync<String>("RPOPLPUSH", Key, AckKey), true) :
                await ExecuteAsync(rc => rc.ExecuteAsync<String>("BRPOPLPUSH", Key, AckKey, timeout), true);
            if (msgId.IsNullOrEmpty()) return default;

            _Status.Consumes++;

            // 取出消息。如果重复消费，或者业务层已经删除消息，此时将拿不到
            //var message = Redis.Get<T>(msgId);
            //if (Equals(message, default)) return 0;
            if (!Redis.TryGetValue(msgId, out T messge))
            {
                // 拿不到消息体，直接确认消息键
                Acknowledge(msgId);
                return default;
            }

            // 处理消息。如果消息已被删除，此时调用func将受到空引用
            var rs = await func(messge);

            // 确认并删除消息
            Redis.Remove(msgId);
            Acknowledge(msgId);

            return rs;
#endif
        }
        #endregion

        #region 死信处理
        /// <summary>从确认列表弹出消息，用于消费中断后，重新恢复现场时获取</summary>
        /// <remarks>理论上Ack队列只存储极少数数据</remarks>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<String> TakeAck(Int32 count = 1)
        {
            if (count <= 0) yield break;

            for (var i = 0; i < count; i++)
            {
                var value = Execute(rc => rc.Execute<String>("RPOP", AckKey), true);
                //if (Equals(value, default(T))) break;
                if (value == null) break;

                yield return value;
            }
        }

        /// <summary>清空所有Ack队列。危险操作！！！</summary>
        /// <returns></returns>
        public Int32 ClearAllAck()
        {
            var rds = Redis as FullRedis;

            // 先找到所有Key
            var keys = rds.Search($"{_Key}:Ack:*", 1000).ToArray();
            return keys.Length > 0 ? rds.Remove(keys) : 0;
        }

        /// <summary>回滚指定AckKey内的消息到Key</summary>
        /// <param name="key"></param>
        /// <param name="ackKey"></param>
        /// <returns></returns>
        private List<String> RollbackAck(String key, String ackKey)
        {
            // 消费所有数据
            var list = new List<String>();
            while (true)
            {
                var value = Execute(rc => rc.Execute<String>("RPOPLPUSH", ackKey, key), true);
                if (value == null) break;

                list.Add(value);
            }

            return list;
        }

        /// <summary>全局回滚死信，一般由单一线程执行，避免干扰处理中数据</summary>
        /// <returns></returns>
        public Int32 RollbackAllAck()
        {
            var rds = Redis as FullRedis;

            // 先找到所有Key
            var count = 0;
            var acks = new List<String>();
            foreach (var key in rds.Search($"{_Key}:Status:*", 1000))
            {
                var ackKey = $"{_Key}:Ack:{key.TrimStart($"{_Key}:Status:")}";
                acks.Add(ackKey);

                var st = rds.Get<RedisQueueStatus>(key);
                if (st != null && st.LastActive.AddSeconds(RetryInterval * 10) < DateTime.Now)
                {
                    if (rds.ContainsKey(ackKey))
                    {
                        XTrace.WriteLine("发现死信队列：{0}", ackKey);

                        var list = RollbackAck(_Key, ackKey);
                        foreach (var item in list)
                        {
                            XTrace.WriteLine("全局回滚死信：{0}", item);
                        }

                        count += list.Count;
                    }

                    // 删除状态
                    rds.Remove(key);
                    XTrace.WriteLine("删除队列状态：{0} {1}", key, st.ToJson());
                }
            }

            // 清理已经失去Status的Ack
            foreach (var key in rds.Search($"{_Key}:Ack:*", 1000))
            {
                if (!acks.Contains(key))
                {
                    var queue = rds.GetList<String>(key) as RedisList<String>;
                    var msgs = queue.GetAll();
                    XTrace.WriteLine("全局清理死信：{0} {1}", key, msgs.ToJson());
                    rds.Remove(key);
                }
            }

            return count;
        }

        private DateTime _nextRetry;
        /// <summary>处理未确认的死信，重新放入队列</summary>
        private void RetryAck()
        {
            var now = DateTime.Now;
            // 一定间隔处理当前ukey死信
            if (_nextRetry < now)
            {
                _nextRetry = now.AddSeconds(RetryInterval);

                // 拿到死信，重新放入队列
                var list = RollbackAck(_Key, AckKey);
                foreach (var item in list)
                {
                    XTrace.WriteLine("定时回滚死信：{0}", item);
                }

                // 更新状态
                UpdateStatus();

                // 处理其它消费者遗留下来的死信，需要抢夺全局清理权，减少全局扫描次数
                if (Redis.Add($"{_Key}:AllStatus", _Status, RetryInterval)) RollbackAllAck();
            }
        }
        #endregion

        #region 状态
        private static readonly RedisQueueStatus _def = new RedisQueueStatus
        {
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            ProcessId = Process.GetCurrentProcess().Id,
            Ip = NetHelper.MyIP() + "",
        };
        private RedisQueueStatus CreateStatus()
        {
            return new RedisQueueStatus
            {
                Key = Rand.NextString(8),
                MachineName = _def.MachineName,
                UserName = _def.UserName,
                ProcessId = _def.ProcessId,
                Ip = _def.Ip,
                CreateTime = DateTime.Now,
                LastActive = DateTime.Now,
            };
        }

        private void UpdateStatus()
        {
            // 更新状态，7天过期
            _Status.LastActive = DateTime.Now;
            Redis.Set(_StatusKey, _Status, 7 * 24 * 3600);
        }
        #endregion
    }
}