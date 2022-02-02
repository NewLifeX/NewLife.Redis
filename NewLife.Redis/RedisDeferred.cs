using NewLife.Caching.Models;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Caching
{
    /// <summary>Redis延迟计算，常用于统计中的凑批行为</summary>
    /// <remarks>
    /// 凑批计算逻辑：
    /// 1，对key下的统计标量进行计算（如累加）
    /// 2，有变化的key进入SETS集合去重
    /// 3，定时从SETS集合中取出key，进行延迟计算（如取统计数据并写入数据库）
    /// 本类负责二三步行为，第一步由外部业务逻辑决定。
    /// </remarks>
    public class RedisDeferred : DisposeBase
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; }

        /// <summary>周期。默认10_000毫秒</summary>
        public Int32 Period { get; set; } = 10_000;

        /// <summary>批大小。默认10</summary>
        public Int32 BatchSize { get; set; } = 10;

        /// <summary>批处理事件</summary>
        public EventHandler<BatchEventArgs> Process;

        private readonly RedisSet<String> _set;
        private TimerX _timer;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="redis"></param>
        /// <param name="name"></param>
        public RedisDeferred(FullRedis redis, String name)
        {
            Name = name;

            _set = redis.GetSet<String>(name) as RedisSet<String>;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _timer.TryDispose();
        }
        #endregion

        #region 方法
        /// <summary>放入队列。后面定时批量处理</summary>
        /// <param name="keys"></param>
        public Int32 Add(params String[] keys)
        {
            StartTimer();

            return _set.SAdd(keys);
        }

        private void StartTimer()
        {
            if (_timer == null)
            {
                lock (this)
                {
                    if (_timer == null)
                    {
                        var p = Period > 100 ? Period : 1000;
                        _timer = new TimerX(DoWork, null, p, p) { Async = true };
                    }
                }
            }
        }

        private void DoWork(Object state)
        {
            var count = _set.Count;
            if (count == 0) return;

            DefaultSpan.Current = null;

            // 逐个弹出处理
            for (var i = 0; i < count; i++)
            {
                var keys = _set.Pop(BatchSize);
                if (keys != null && keys.Length > 0)
                {
                    using var span = _set.Redis.Tracer?.NewSpan("redis:Deferred", $"Name={Name} keys={keys.Join()}");
                    try
                    {
                        OnProcess(keys);
                    }
                    catch (Exception ex)
                    {
                        span?.SetError(ex, null);

                        // 处理失败，重新写回去集合
                        _set.SAdd(keys);

                        throw;
                    }
                }
            }

            if (Period > 0) _timer.Period = Period;
        }

        /// <summary>批量处理</summary>
        /// <param name="keys"></param>
        protected virtual void OnProcess(String[] keys) => Process?.Invoke(this, new BatchEventArgs { Keys = keys });
        #endregion
    }
}