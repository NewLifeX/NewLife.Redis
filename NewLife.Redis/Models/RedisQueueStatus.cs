using System;

namespace NewLife.Caching.Models
{
    /// <summary>Redis队列状态</summary>
    public class RedisQueueStatus
    {
        /// <summary>标识消费者的唯一Key</summary>
        public String Key { get; set; }

        /// <summary>机器名</summary>
        public String MachineName { get; set; }

        /// <summary>用户名</summary>
        public String UserName { get; set; }

        /// <summary>进程</summary>
        public Int32 ProcessId { get; set; }

        /// <summary>IP地址</summary>
        public String Ip { get; set; }

        /// <summary>开始时间</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>最后活跃时间</summary>
        public DateTime LastActive { get; set; }

        /// <summary>消费消息数</summary>
        public Int64 Consumes { get; set; }

        /// <summary>确认消息数</summary>
        public Int64 Acks { get; set; }
    }
}