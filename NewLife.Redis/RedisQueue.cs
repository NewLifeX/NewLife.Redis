using System;
using System.Collections.Generic;

namespace NewLife.Caching
{
    /// <summary>Redis队列，左进右出</summary>
    /// <remarks>
    /// 默认弹出消费，不需要确认，使用非常简单，但如果消费者处理失败，消息将会丢失；
    /// 严格模式下消费，弹出消息的同时插入ACK队列，消费者处理成功后将从ACK队列删除该消息，若处理失败，则将延迟消费ACK消息；
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class RedisQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>严格的队列消费，需要确认机制，默认false</summary>
        public Boolean Strict { get; set; }

        /// <summary>用于确认的列表</summary>
        public String AckKey { get; set; }

        /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
        public Int32 MinPipeline { get; set; } = 3;
        #endregion

        #region 实例化
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisQueue(Redis redis, String key) : base(redis, key) => AckKey = key + "_ack";
        #endregion

        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("LLEN", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

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

        /// <summary>批量消费获取</summary>
        /// <param name="count">要消费的消息个数</param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            // 严格模式
            if (Strict)
            {
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
    }
}