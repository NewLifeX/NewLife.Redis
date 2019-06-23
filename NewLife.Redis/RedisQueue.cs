using System;
using System.Collections.Generic;

namespace NewLife.Caching
{
    /// <summary>生产者消费者</summary>
    /// <typeparam name="T"></typeparam>
    public class RedisQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 实例化
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisQueue(Redis redis, String key) : base(redis, key) { }
        #endregion

        /// <summary>生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(IEnumerable<T> values)
        {
            var args = new List<Object>
            {
                Key
            };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("RPUSH", args.ToArray()), true);
        }

        /// <summary>消费获取</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            //return _Redis.Execute(rc => rc.Execute<T[]>("LRANGE", _Key, 0, count - 1));

            for (var i = 0; i < count; i++)
            {
                var value = Execute(rc => rc.Execute<T>("LPOP", Key), true);
                if (Equals(value, default(T))) break;

                yield return value;
            }
        }
    }
}