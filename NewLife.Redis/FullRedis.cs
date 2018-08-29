using System;
using System.Collections.Generic;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Caching
{
    /// <summary>Redis缓存</summary>
    public class FullRedis : Redis
    {
        #region 静态
        static FullRedis()
        {
            ObjectContainer.Current.AutoRegister<Redis, FullRedis>();
        }

        /// <summary>注册</summary>
        public static void Register() { }

        /// <summary>根据连接字符串创建</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static FullRedis Create(String config)
        {
            var rds = new FullRedis();
            rds.Init(config);

            return rds;
        }
        #endregion

        #region 属性
        /// <summary>性能计数器</summary>
        public PerfCounter Counter { get; set; } = new PerfCounter();
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>重载执行，统计性能</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public override T Execute<T>(Func<RedisClient, T> func)
        {
            var sw = Counter.StartCount();
            try
            {
                return base.Execute(func);
            }
            finally
            {
                Counter.StopCount(sw);
            }
        }
        #endregion

        #region 集合操作
        /// <summary>获取列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IList<T> GetList<T>(String key) => new RedisList<T>(this, key);

        /// <summary>获取哈希</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IDictionary<String, T> GetDictionary<T>(String key) => new RedisHash<String, T>(this, key);
        #endregion
    }
}