using System;
using System.Collections.Generic;
using NewLife.Data;

namespace NewLife.Caching
{
    /// <summary>Redis队列，左进右出</summary>
    /// <remarks>
    /// 默认弹出消费，不需要确认，使用非常简单，但如果消费者处理失败，消息将会丢失；
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class RedisQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>消费时阻塞，直到有数据为止。仅支持TakeOne，不支持Take</summary>
        public Boolean Blocking { get; set; }

        /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
        public Int32 MinPipeline { get; set; } = 3;

        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("LLEN", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;
        #endregion

        #region 构造
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisQueue(Redis redis, String key) : base(redis, key) { }
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
            if (!Blocking) return Execute(rc => rc.Execute<T>("RPOP", Key), true);

            var rs = Execute(rc => rc.Execute<Packet[]>("BRPOP", Key, timeout), true);
            return rs == null || rs.Length < 2 ? default : (T)Redis.Encoder.Decode(rs[1], typeof(T));
        }

        /// <summary>批量消费获取</summary>
        /// <param name="count">要消费的消息个数</param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            // 借助管道支持批量获取
            if (count >= MinPipeline)
            {
                var rds = Redis;
                rds.StartPipeline();

                for (var i = 0; i < count; i++)
                {
                    Execute(rc => rc.Execute<T>("RPOP", Key), true);
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
                    var value = Execute(rc => rc.Execute<T>("RPOP", Key), true);
                    if (Equals(value, default(T))) break;

                    yield return value;
                }
            }
        }
    }
}