using System;
using System.Collections.Generic;

namespace NewLife.Caching
{
    /// <summary>生产者消费者</summary>
    /// <typeparam name="T"></typeparam>
    class RedisQueue<T> : IProducerConsumer<T>
    {
        private Redis _Redis;
        private String _Key;

        public RedisQueue(Redis rds, String key)
        {
            _Redis = rds;
            _Key = key;
        }

        /// <summary>生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(IEnumerable<T> values)
        {
            var ps = new List<Object>
            {
                _Key
            };
            foreach (var item in values)
            {
                ps.Add(item);
            }
            return _Redis.Execute(rc => rc.Execute<Int32>("RPUSH", ps.ToArray()));
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
                var value = _Redis.Execute(rc => rc.Execute<T>("LPOP", _Key));
                if (Equals(value, default(T))) break;

                yield return value;
            }
        }
    }
}