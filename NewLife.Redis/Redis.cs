using System;
using System.Collections.Generic;
using NewLife.Caching;
using ServiceStack.Redis;

namespace NewLife.Redis
{
    /// <summary>Redis缓存</summary>
    public class Redis : Cache
    {
        #region 静态
        /// <summary>创建</summary>
        /// <param name="server"></param>
        /// <param name="db"></param>
        /// <param name="poolSize"></param>
        /// <returns></returns>
        public static Redis Create(String server, Int32 db, Int32 poolSize = 200)
        {
            if (String.IsNullOrEmpty(server) || server == ".") server = "127.0.0.1";

            return Create("Redis://{0}?Db={1}&PoolSize={2}".F(server, db, poolSize)) as Redis;
        }
        #endregion

        #region 属性
        /// <summary>服务器</summary>
        public String Server { get; private set; }

        /// <summary>目标数据库。默认0</summary>
        public Int32 Db { get; set; }

        /// <summary>池大小</summary>
        public Int32 PoolSize { get; set; } = 200;
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="set"></param>
        protected override void Init(CacheSetting set)
        {
            var config = set?.Value;
            if (config.IsNullOrEmpty()) return;

            var p = config.IndexOf("?");
            if (p > 0)
            {
                var dic = config.Substring(p + 1).SplitAsDictionary("=", "?", "&");
                if (dic.Count > 0)
                {
                    Db = dic["Db"].ToInt();

                    var n = dic["PoolSize"].ToInt();
                    if (n > 0) PoolSize = n;
                }

                config = config.Substring(0, p);
            }

            Server = config;
        }
        #endregion

        #region 客户端池
        private PooledRedisClientManager _pool;
        /// <summary>缓存池</summary>
        public PooledRedisClientManager Pool
        {
            get
            {
                if (_pool != null) return _pool;
                lock (this)
                {
                    if (_pool != null) return _pool;

                    var cfg = new RedisClientManagerConfig
                    {
                        AutoStart = true,
                        MaxReadPoolSize = PoolSize,
                        MaxWritePoolSize = PoolSize,
                        DefaultDb = Db
                    };
                    return _pool = new PooledRedisClientManager(new List<String>() { Server }, new List<String>() { Server }, cfg);
                }
            }
            set { _pool = null; }
        }

        /// <summary>获取客户端</summary>
        /// <returns></returns>
        public IRedisClient GetClient() { return Pool.GetClient(); }
        #endregion

        #region 基础操作
        /// <summary>缓存个数</summary>
        public override Int32 Count
        {
            get
            {
                using (var redis = GetClient())
                {
                    return (Int32)redis.DbSize;
                }
            }
        }

        /// <summary>所有键</summary>
        public override ICollection<String> Keys
        {
            get
            {
                using (var redis = GetClient())
                {
                    return redis.GetAllKeys();
                }
            }
        }

        /// <summary>单个实体项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">超时时间，秒</param>
        public override Boolean Set<T>(String key, T value, Int32 expire = -1)
        {
            if (expire <= 0) expire = Expire;

            var client = Pool.GetCacheClient();
            return client.Set(key, value, new TimeSpan(0, 0, expire));
        }

        /// <summary>获取单体</summary>
        /// <param name="key">键</param>
        public override T Get<T>(String key)
        {
            using (var redis = Pool.GetReadOnlyCacheClient())
            {
                return redis.Get<T>(key);
            }
        }

        /// <summary>移除单体</summary>
        /// <param name="key">键</param>
        public override Boolean Remove(String key)
        {
            using (var redis = Pool.GetCacheClient())
            {
                return redis.Remove(key);
            }
        }

        /// <summary>是否存在</summary>
        /// <param name="key">键</param>
        public override Boolean ContainsKey(String key)
        {
            using (var redis = GetClient())
            {
                return redis.ContainsKey(key);
            }
        }

        /// <summary>获取列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IList<T> GetList<T>(String key)
        {
            //return Pool.GetClient().As<T>().Lists[key];
            return new RedisList<T>(this, key);
        }

        /// <summary>获取哈希</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IDictionary<String, T> GetDictionary<T>(String key)
        {
            return new RedisHash<String, T>(this, key);
        }
        #endregion
    }
}