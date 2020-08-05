using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Security;

namespace NewLife.Caching
{
    /// <summary>Redis队列，左进右出</summary>
    /// <remarks>
    /// 默认弹出消费，不需要确认，使用非常简单，但如果消费者处理失败，消息将会丢失；
    /// 严格模式下消费，弹出消息的同时插入ACK队列，消费者处理成功后将从ACK队列删除该消息，若处理失败，则将延迟消费ACK消息；
    /// 
    /// 为了让严格模式支持多线程消费，确认队列AckKey构造为 key:Ack:Rand16 的格式，每一个消费者都将有自己完全独一无二的确认队列。
    /// 消费者每30秒（RetryInterval）清理一次确认队列的死信（未确认消息），重新投入主队列。
    /// 应用异常退出时，可能产生一些死信，在应用启动时，应该由单一线程通过TakeAllAck消费清理所有确认队列，该操作有风险，多点部署时可能误杀正在使用的确认队列。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class RedisQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>严格的队列消费，需要确认机制，默认false</summary>
        public Boolean Strict { get; set; }

        /// <summary>消费时阻塞，直到有数据为止。仅支持TakeOne，不支持Take</summary>
        public Boolean Blocking { get; set; }

        /// <summary>用于确认的列表</summary>
        public String AckKey { get; set; }

        /// <summary>重新处理确认队列中死信的间隔。默认30s</summary>
        public Int32 RetryInterval { get; set; } = 30;

        /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
        public Int32 MinPipeline { get; set; } = 3;

        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("LLEN", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        private String _Key;
        #endregion

        #region 构造
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisQueue(Redis redis, String key) : base(redis, key)
        {
            _Key = key;
            AckKey = $"{key}:Ack:{Rand.NextString(16)}";
        }
        #endregion

        /// <summary>生产添加</summary>
        /// <param name="value">消息</param>
        /// <returns></returns>
        public Int32 Add(T value)
        {
            return Execute(rc => rc.Execute<Int32>("LPUSH", Key, value), true);
        }

        /// <summary>批量生产添加</summary>
        /// <param name="values">消息集合</param>
        /// <returns></returns>
        public Int32 Add(IEnumerable<T> values)
        {
            var args = new List<Object> { Key };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("LPUSH", args.ToArray()), true);
        }

        /// <summary>消费获取</summary>
        /// <param name="timeout">超时，秒</param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0)
        {
            // 严格模式
            if (Strict)
            {
                RetryDeadAck();

                if (Blocking)
                    return Execute(rc => rc.Execute<T>("BRPOPLPUSH", Key, AckKey, timeout), true);
                else
                    return Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);
            }
            else
            {
                if (!Blocking) return Execute(rc => rc.Execute<T>("RPOP", Key), true);

                var rs = Execute(rc => rc.Execute<Packet[]>("BRPOP", Key, timeout), true);
                return rs == null || rs.Length < 2 ? default : (T)Redis.Encoder.Decode(rs[1], typeof(T));
            }
        }

        /// <summary>批量消费获取</summary>
        /// <param name="count">要消费的消息个数</param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            // 严格模式
            if (Strict)
            {
                RetryDeadAck();

                foreach (var item in TakeStrict(count))
                {
                    yield return item;
                }
                yield break;
            }
            else
            {
                foreach (var item in RPOP(Key, count))
                {
                    yield return item;
                }
            }
        }

        private IEnumerable<T> RPOP(String key, Int32 count)
        {
            // 借助管道支持批量获取
            if (count >= MinPipeline)
            {
                var rds = Redis;
                rds.StartPipeline();

                for (var i = 0; i < count; i++)
                {
                    Execute(rc => rc.Execute<T>("RPOP", key), true);
                }

                var rs = rds.StopPipeline(true);
                foreach (var item in rs)
                {
                    if (item != null) yield return (T)item;
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var value = Execute(rc => rc.Execute<T>("RPOP", key), true);
                    if (Equals(value, default(T))) break;

                    yield return value;
                }
            }
        }

        /// <summary>严格消费获取，同时送入确认列表</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> TakeStrict(Int32 count = 1)
        {
            if (count <= 0) yield break;

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
                    if (item != null) yield return (T)item;
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var value = Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);
                    if (Equals(value, default(T))) break;

                    yield return value;
                }
            }
        }

        /// <summary>从确认列表消费获取，用于消费中断后，重新恢复现场时获取</summary>
        /// <remarks>理论上Ack队列只存储极少数数据</remarks>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> TakeAck(Int32 count = 1)
        {
            if (count <= 0) yield break;

            foreach (var item in RPOP(AckKey, count))
            {
                yield return item;
            }
        }

        /// <summary>确认消费。仅用于严格消费</summary>
        /// <param name="values"></param>
        public Int32 Acknowledge(IEnumerable<T> values)
        {
            var rs = 0;
            foreach (var item in values)
            {
                rs += Execute(r => r.Execute<Int32>("LREM", AckKey, 1, item), true);
            }

            return rs;
        }

        #region 死信处理
        /// <summary>消费所有确认队列中的遗留数据，一般在应用启动时由单一线程执行</summary>
        /// <returns></returns>
        public IEnumerable<T> TakeAllAck()
        {
            var rds = Redis as FullRedis;

            // 先找到所有Key
            foreach (var key in rds.Search($"{_Key}:Ack:*"))
            {
                // 数量
                var count = Execute(r => r.Execute<Int32>("LLEN", key));

                if (count > 0)
                {
                    // 消费所有数据
                    while (true)
                    {
                        var list = RPOP(key, 100).ToList();
                        if (list.Count > 0)
                        {
                            foreach (var item in list)
                            {
                                yield return item;
                            }
                        }

                        if (list.Count < 100) break;
                    }
                }

                // 删除确认队列
                rds.Remove(key);
            }
        }

        /// <summary>已经清理过死信的队列</summary>
        private static ConcurrentHashSet<String> _keysOfNoAck = new ConcurrentHashSet<String>();

        private DateTime _nextRetry;
        /// <summary>处理未确认的死信，重新放入队列</summary>
        private void RetryDeadAck()
        {
            var now = DateTime.Now;
            if (_nextRetry.Year < 2000)
            {
                _nextRetry = now.AddSeconds(RetryInterval);

                // 应用启动时，来一次全局清理死信
                if (_keysOfNoAck.TryAdd(_Key))
                {
                    var list = TakeAllAck().ToList();
                    if (list.Count > 0) Add(list);
                }
            }
            else if (_nextRetry < now)
            {
                // 避免多线程进入
                lock (_keysOfNoAck)
                {
                    // 拿到死信，重新放入队列
                    while (true)
                    {
                        var list = TakeAck(100).ToList();
                        if (list.Count > 0) Add(list);

                        if (list.Count < 100) break;
                    }
                }

                _nextRetry = now.AddSeconds(RetryInterval);
            }
        }
        #endregion
    }
}