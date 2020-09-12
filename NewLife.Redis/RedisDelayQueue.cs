using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Data;

namespace NewLife.Caching
{
    /// <summary>Redis延迟队列，key放在zset，消息体放在kv</summary>
    /// <remarks>
    /// 延迟Redis队列，每次生产操作1次Redis，消费操作2次Redis。
    /// </remarks>
    public class RedisDelayQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>个数</summary>
        public Int32 Count => Execute(rc => rc.Execute<Int32>("ZCARD", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        /// <summary>消息体过期时间，默认10*24*3600</summary>
        public Int32 BodyExpire { get; set; } = 10 * 24 * 3600;

        /// <summary>默认延迟时间。单位，秒</summary>
        public Int32 Delay { get; set; }
        #endregion

        #region 实例化
        /// <summary>实例化延迟队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisDelayQueue(Redis redis, String key) : base(redis, key) { }
        #endregion

        #region 核心方法
        /// <summary>添加延迟消息</summary>
        /// <param name="value"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public Int32 Add(T value, Int32 delay)
        {
            var source = DateTime.Now.ToInt() + delay;
            var rs = Execute(rc => rc.Execute<Int32>("ZADD", Key, source, value), true);

            return rs;
        }

        /// <summary>批量生产</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(params T[] values)
        {
            Redis.StartPipeline();

            foreach (var item in values)
            {
                Add(item, Delay);
            }

            var rs = Redis.StopPipeline();

            return rs.Cast<Int32>().Sum();
        }

        /// <summary>获取一批</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            var source = DateTime.Now.ToInt();
            var rs = Execute(r => r.Execute<Object[]>("ZRANGEBYSCORE", Key, 0, source, "LIMIT", 0, count));
            if (rs == null || rs.Length == 0) yield break;

            foreach (var item in rs)
            {
                // 删除作为抢夺
                var rs2 = Execute(r => r.Execute<Int32>("ZREM", Key, item), true);
                if (rs2 > 0 && item is Packet pk) yield return (T)Redis.Encoder.Decode(pk, typeof(T));
            }
        }

        /// <summary>获取一个</summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0)
        {
            var source = DateTime.Now.ToInt();
            var rs = Execute(r => r.Execute<Object[]>("ZRANGEBYSCORE", Key, 0, source, "LIMIT", 0, 1));
            if (rs == null || rs.Length == 0) return default;

            // 删除作为抢夺
            var rs2 = Execute(r => r.Execute<Int32>("ZREM", Key, rs[0]), true);
            if (rs2 <= 0) return default;

            if (rs[0] is Packet pk) return (T)Redis.Encoder.Decode(pk, typeof(T));

            return default;
        }

        /// <summary>异步获取一个</summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<T> TakeOneAsync(Int32 timeout = 0)
        {
            var source = DateTime.Now.ToInt();
            var rs = await ExecuteAsync(r => r.ExecuteAsync<Object[]>("ZRANGEBYSCORE", new Object[] { Key, 0, source, "LIMIT", 0, 1 }));
            if (rs == null || rs.Length == 0) return default;

            // 删除作为抢夺
            var rs2 = await ExecuteAsync(r => r.ExecuteAsync<Int32>("ZREM", new Object[] { Key, rs[0] }), true);
            if (rs2 <= 0) return default;

            if (rs[0] is Packet pk) return (T)Redis.Encoder.Decode(pk, typeof(T));

            return default;
        }

        Int32 IProducerConsumer<T>.Acknowledge(params String[] keys) => throw new NotImplementedException();
        #endregion

        #region 辅助方法
        /// <summary>获取最近一个消息的到期时间，便于上层控制调度器</summary>
        /// <returns></returns>
        public KeyValuePair<String, Double> GetNext()
        {
            var source = DateTime.Now.AddYears(1).ToInt();
            var rs = Execute(r => r.Execute<Object[]>("ZRANGE", Key, 0, 0, "WITHSCORES"));
            if (rs == null || rs.Length < 2) return default;

            var item = (rs[0] as Packet).ToStr();
            var source2 = (rs[1] as Packet).ToStr().ToDouble();
            return new KeyValuePair<String, Double>(item, source2);
        }
        #endregion
    }
}