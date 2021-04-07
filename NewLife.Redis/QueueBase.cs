using System;

namespace NewLife.Caching
{
    /// <summary>消息队列基类</summary>
    public abstract class QueueBase : RedisBase
    {
        #region 属性
        /// <summary>追踪名。默认Key，主要用于解决动态Topic导致产生大量埋点的问题</summary>
        public String TraceName { get; set; }

        /// <summary>是否在消息报文中自动注入TraceId。TraceId用于跨应用在生产者和消费者之间建立调用链，默认true</summary>
        public Boolean AttachTraceId { get; set; } = true;

        /// <summary>失败时抛出异常。默认false</summary>
        public Boolean ThrowOnFailure { get; set; } = false;

        /// <summary>消息队列主题</summary>
        public String Topic => Key;
        #endregion

        #region 构造
        /// <summary>实例化延迟队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public QueueBase(Redis redis, String key) : base(redis, key) => TraceName = key;
        #endregion
    }
}