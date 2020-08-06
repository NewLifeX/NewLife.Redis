using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Security;

namespace NewLife.Caching
{
    /// <summary>可靠Redis队列，左进右出</summary>
    /// <remarks>
    /// 严格模式下消费，弹出消息的同时插入ACK队列，消费者处理成功后将从ACK队列删除该消息，若处理失败，则将延迟消费ACK消息；
    /// 
    /// 为了让严格模式支持多线程消费，确认队列AckKey构造为 key:Ack:Rand16 的格式，每一个消费者都将有自己完全独一无二的确认队列。
    /// 消费者每30秒（RetryInterval）清理一次确认队列的死信（未确认消息），重新投入主队列。
    /// 应用异常退出时，可能产生一些死信，在应用启动时，应该由单一线程通过TakeAllAck消费清理所有确认队列，该操作有风险，多点部署时可能误杀正在使用的确认队列。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class RedisReliableQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
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
        public RedisReliableQueue(Redis redis, String key) : base(redis, key)
        {
            _Key = key;
            AckKey = $"{key}:Ack:{DateTime.Now:yyMMddHH}:{Rand.NextString(8)}";
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

        /// <summary>消费获取，支持阻塞</summary>
        /// <remarks>假定前面获取的消息已经确认，因该方法内部可能回滚确认队列，避免误杀</remarks>
        /// <param name="timeout">超时，0秒永远阻塞；负数表示直接返回，不阻塞。</param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0)
        {
            RetryDeadAck();

            if (timeout >= 0)
                return Execute(rc => rc.Execute<T>("BRPOPLPUSH", Key, AckKey, timeout), true);

            return Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);
        }

        /// <summary>批量消费获取</summary>
        /// <remarks>假定前面获取的消息已经确认，因该方法内部可能回滚确认队列，避免误杀</remarks>
        /// <param name="count">要消费的消息个数</param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            RetryDeadAck();

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
                    if (!Equals(item, default(T))) yield return (T)item;
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

            for (var i = 0; i < count; i++)
            {
                var value = Execute(rc => rc.Execute<T>("RPOP", AckKey), true);
                if (Equals(value, default(T))) break;

                yield return value;
            }
        }

        /// <summary>确认消费。仅用于严格消费</summary>
        /// <param name="values"></param>
        public Int32 Acknowledge(IEnumerable<T> values)
        {
            var rs = 0;

            // 管道支持
            if (values.Count() >= MinPipeline)
            {
                var rds = Redis;
                rds.StartPipeline();

                foreach (var item in values)
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
                foreach (var item in values)
                {
                    rs += Execute(r => r.Execute<Int32>("LREM", AckKey, 1, item), true);
                }
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
                XTrace.WriteLine("发现死信队列：{0}", key);

                // 消费所有数据
                while (true)
                {
                    var value = Execute(rc => rc.Execute<T>("RPOP", key), true);
                    if (Equals(value, default(T))) break;

                    yield return value;
                }

                //// 删除确认队列
                //rds.Remove(key);
            }
        }

        /// <summary>全局回滚死信，一般由单一线程执行，避免干扰处理中数据</summary>
        /// <returns></returns>
        public Int32 RollbackAck()
        {
            var count = 0;
            foreach (var item in TakeAllAck())
            {
                XTrace.WriteLine("全局回滚死信：{0}", item);
                Add(item);

                count++;
            }

            return count;
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
                if (_keysOfNoAck.TryAdd(_Key)) RollbackAck();
            }
            else if (_nextRetry < now)
            {
                _nextRetry = now.AddSeconds(RetryInterval);

                // 避免多线程进入。只能处理当前确认队列，避免误杀处理中消息
                lock (_keysOfNoAck)
                {
                    // 拿到死信，重新放入队列
                    foreach (var item in TakeAck(1000))
                    {
                        XTrace.WriteLine("定时回滚死信：{0}", item);
                        Add(item);
                    }
                }
            }
        }
        #endregion
    }
}