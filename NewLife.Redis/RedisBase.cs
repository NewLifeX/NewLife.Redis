using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Caching
{
    /// <summary>基础结构</summary>
   public abstract class RedisBase
    {
        #region 属性
        /// <summary>客户端对象</summary>
        public Redis Redis { get; }

        /// <summary>键</summary>
        public String Key { get; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisBase(Redis redis, String key) { Redis = redis; Key = key; }
        #endregion

        #region 方法
        /// <summary>执行命令</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public virtual T Execute<T>(Func<RedisClient, T> func) => Redis.Execute(func);
        #endregion
    }
}