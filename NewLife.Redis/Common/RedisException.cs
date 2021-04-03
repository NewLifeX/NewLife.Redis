using System;

namespace NewLife.Caching.Common
{
    /// <summary>Redis异常</summary>
    public class RedisException : XException
    {
        /// <summary>实例化Redis异常</summary>
        /// <param name="message"></param>
        public RedisException(String message) : base(message) { }

        /// <summary>实例化Redis异常</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public RedisException(String message,Exception innerException) : base(message, innerException) { }
    }
}