#if !NET40
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Security;

namespace NewLife.Caching
{
    /// <summary>保存指定key数据的委托</summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    public delegate void SaveDelegate(String key, IDictionary<String, Int32> data);

    /// <summary>Redis流式统计</summary>
    /// <remarks>
    /// 借助Redis进行流式增量计算。
    /// 1，HASH结构对Key下的统计标量进行累加
    /// 2，有变化的Key进入延迟队列，延迟一定时间后进入普通队列
    /// 3，消费队列，得到Key后进行Rename，然后把数据取回来写入数据库
    /// </remarks>
    public class RedisStat : DisposeBase
    {
        #region 属性
        /// <summary>统计名称。如Station等</summary>
        public String Name { get; }

        /// <summary>取回统计数据后的委托。一般用于保存到数据库</summary>
        public SaveDelegate OnSave { get; set; }

        private readonly FullRedis _redis;
        private readonly FullRedis _redis2;
        private readonly RedisReliableQueue<String> _queue;
        private readonly CancellationTokenSource _source;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="redis"></param>
        /// <param name="name"></param>
        public RedisStat(FullRedis redis, String name)
        {
            Name = name;

            // 存放数据和去重key的库
            _redis = redis;

            // 存放消息队列的库，不能跟数据key放在一起，否则影响队列的scan
            _redis2 = redis.CreateSub(redis.Db + 1) as FullRedis;
            _redis2.Log = _redis.Log;
            _queue = _redis2.GetReliableQueue<String>(name);
            _source = new CancellationTokenSource();
            Task.Factory.StartNew(() => _queue.ConsumeAsync<String>(OnProcess, _source.Token, null));
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _source.Cancel();
        }
        #endregion

        #region 方法
        /// <summary>增加指定标量的值</summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void Increment(String key, String field, Int32 value) => _redis.Execute(key, c => c.Execute<Int32>("HINCRBY", key, field, value), true);

        /// <summary>放入延迟队列</summary>
        /// <param name="key"></param>
        /// <param name="delay"></param>
        public void AddDelayQueue(String key, Int32 delay)
        {
            // 去重，避免重复加入队列
            var rs = _redis.Add($"exists:{key}", DateTime.Now, 600);
            if (rs) _queue.AddDelay(key, delay);
        }

        /// <summary>消费处理</summary>
        /// <param name="key"></param>
        protected virtual void OnProcess(String key)
        {
            /*
             * 1，在HASH中重命名该Key
             * 2，把HASH中该Key数据全部取回
             */

            var newKey = $"{key}:{Rand.NextString(16)}";
            if (!_redis.Rename(key, newKey, false)) return;

            _redis.Remove($"exists:{key}");
            var rs = _redis.Execute(newKey, r => r.Execute<Packet[]>("HGETALL", newKey));

            var dic = new Dictionary<String, Int32>();
            for (var i = 0; i < rs.Length; i++)
            {
                var k = rs[i].ToStr();
                var v = rs[++i].ToStr().ToInt();
                dic[k] = v;
            }

            OnSave(key, dic);

            _redis.Remove(newKey);
        }
        #endregion
    }
}
#endif