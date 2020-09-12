using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Caching
{
    /// <summary>Redis延迟队列，key放在zset，消息体放在kv</summary>
    /// <remarks>
    /// 延迟Redis队列，每次生产操作1次Redis，消费操作2次Redis。
    /// </remarks>
    public class RedisDelayQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>用于确认的列表</summary>
        public String AckKey { get; set; }

        /// <summary>重新处理确认队列中死信的间隔。默认60s</summary>
        public Int32 RetryInterval { get; set; } = 60;

        /// <summary>个数</summary>
        public Int32 Count => Execute(rc => rc.Execute<Int32>("ZCARD", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        /// <summary>默认延迟时间。单位，秒</summary>
        public Int32 Delay { get; set; }
        #endregion

        #region 实例化
        /// <summary>实例化延迟队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisDelayQueue(Redis redis, String key) : base(redis, key)
        {
            AckKey = $"{key}:Ack";
        }
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

        private Int32 Add(Object[] values, Int32 delay)
        {
            var args = new List<Object> { Key };

            foreach (var item in values)
            {
                args.Add(delay);
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("ZADD", args.ToArray()), true);
        }

        /// <summary>批量生产</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(params T[] values) => Add(values.Cast<Object>().ToArray(), Redis.Expire);

        /// <summary>删除项</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Int32 Remove(T value) => Execute(r => r.Execute<Int32>("ZREM", Key, value), true);

        /// <summary>获取一个</summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0)
        {
            RetryDeadAck();

            var source = DateTime.Now.ToInt();
            var rs = Execute(r => r.Execute<Object[]>("ZRANGEBYSCORE", Key, 0, source, "LIMIT", 0, 1));
            if (rs == null || rs.Length == 0) return default;

            // 争夺消费
            return TryPop(rs[0], out var result) ? result : default;
        }

        /// <summary>异步获取一个</summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<T> TakeOneAsync(Int32 timeout = 0)
        {
            RetryDeadAck();

            var source = DateTime.Now.ToInt();
            var rs = await ExecuteAsync(r => r.ExecuteAsync<Object[]>("ZRANGEBYSCORE", new Object[] { Key, 0, source, "LIMIT", 0, 1 }));
            if (rs == null || rs.Length == 0) return default;

            // 争夺消费
            return TryPop(rs[0], out var result) ? result : default;
        }

        /// <summary>获取一批</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            RetryDeadAck();

            var source = DateTime.Now.ToInt();
            var rs = Execute(r => r.Execute<Object[]>("ZRANGEBYSCORE", Key, 0, source, "LIMIT", 0, count));
            if (rs == null || rs.Length == 0) yield break;

            foreach (var item in rs)
            {
                // 争夺消费
                if (TryPop(item, out var result)) yield return result;
            }
        }

        /// <summary>争夺消费，只有一个线程能够成功删除，作为抢到的标志。同时备份到Ack队列</summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private Boolean TryPop(Object value, out T result)
        {
            // 先备份，再删除
            if (value is Packet pk)
            {
                // 备份到Ack队列
                var source = DateTime.Now.ToInt() + RetryInterval;
                Execute(rc => rc.Execute<Int32>("ZADD", AckKey, source, pk), true);

                // 删除作为抢夺
                if (Remove(new[] { value }) > 0)
                {
                    result = (T)Redis.Encoder.Decode(pk, typeof(T));
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>确认删除</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Int32 Acknowledge(params String[] keys) => Remove(keys);
        #endregion

        #region 死信处理
        /// <summary>回滚指定AckKey内的消息到Key</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private List<String> RollbackAck(String key)
        {
            // 消费所有数据
            var list = new List<String>();
            while (true)
            {
                var source = DateTime.Now.ToInt();
                var rs = Execute(r => r.Execute<String[]>("ZRANGEBYSCORE", key, 0, source, "LIMIT", 0, 100));
                if (rs == null || rs.Length == 0) break;

                // 加入原始队列
                Add(rs, 0);
                Remove(rs);

                list.AddRange(rs);
            }

            return list;
        }

        private DateTime _nextRetry;
        /// <summary>处理未确认的死信，重新放入队列</summary>
        private void RetryDeadAck()
        {
            var now = DateTime.Now;
            // 一定间隔处理死信
            if (_nextRetry < now)
            {
                _nextRetry = now.AddSeconds(RetryInterval);

                // 拿到死信，重新放入队列
                var list = RollbackAck(AckKey);
                foreach (var item in list)
                {
                    XTrace.WriteLine("定时回滚死信：{0}", item);
                }
            }
        }
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

        /// <summary>删除一批</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private Int32 Remove(Object[] values)
        {
            var args = new List<Object> { Key };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("ZREM", args.ToArray()), true);
        }
        #endregion
    }
}