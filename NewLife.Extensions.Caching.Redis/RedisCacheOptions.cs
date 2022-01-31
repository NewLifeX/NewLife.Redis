using System;
using Microsoft.Extensions.Options;

namespace NewLife.Extensions.Caching.Redis
{
    /// <summary>
    /// Redis缓存选项
    /// </summary>
    public class RedisCacheOptions : IOptions<RedisCacheOptions>
    {
        /// <summary>
        /// 配置字符串。例如：server=127.0.0.1:6379;password=123456;db=3;timeout=3000
        /// </summary>
        public String Configuration { get; set; }

        /// <summary>
        /// 实例名
        /// </summary>
        public String InstanceName { get; set; }

        RedisCacheOptions IOptions<RedisCacheOptions>.Value => this;
    }
}