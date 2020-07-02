using System;
using System.Collections.Generic;

namespace NewLife.Caching
{
    /// <summary>Redis栈，右进右出</summary>
    /// <typeparam name="T"></typeparam>
    public class RedisStack<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("LLEN", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
        public Int32 MinPipeline { get; set; } = 3;
        #endregion

        #region 构造
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisStack(Redis redis, String key) : base(redis, key) { }
        #endregion

        /// <summary>批量生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(IEnumerable<T> values)
        {
            var args = new List<Object> { Key };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("RPUSH", args.ToArray()), true);
        }

        /// <summary>批量消费获取</summary>
        /// <param name="count"></param>
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
                    yield return (T)item;
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