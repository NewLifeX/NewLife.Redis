using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Data;
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
        /// <summary>模式</summary>
        public String Mode { get; private set; }

        /// <summary>集群</summary>
        public RedisCluster Cluster { get; set; }
        #endregion

        #region 构造
        /// <summary>初始化配置</summary>
        /// <param name="config"></param>
        public override void Init(String config)
        {
            base.Init(config);

            // 集群不支持Select
            if (Db == 0)
            {
                // 访问一次info信息，解析工作模式，以判断是否集群
                var info = GetInfo();
                if (info != null)
                {
                    if (info.TryGetValue("redis_mode", out var mode)) Mode = mode;

                    // 集群模式初始化节点
                    if (mode == "cluster") Cluster = new RedisCluster(this);
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>重载执行，支持集群</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="func"></param>
        /// <param name="write">是否写入操作</param>
        /// <returns></returns>
        public override T Execute<T>(String key, Func<RedisClient, T> func, Boolean write = false)
        {
            var node = Cluster?.SelectNode(key);

            // 如果不支持集群，直接返回
            if (node == null) return base.Execute(key, func, write);

            // 统计性能
            var sw = Counter?.StartCount();

            var i = 0;
            do
            {
                var pool = node.Pool;

                // 每次重试都需要重新从池里借出连接
                var client = pool.Get();
                try
                {
                    client.Reset();
                    var rs = func(client);

                    Counter?.StopCount(sw);

                    return rs;
                }
                catch (InvalidDataException)
                {
                    if (i++ >= Retry) throw;
                }
                catch (Exception ex)
                {
                    // 处理MOVED和ASK指令
                    var msg = ex.Message;
                    if (msg.StartsWithIgnoreCase("MOVED", "ASK"))
                    {
                        // 取出地址，找到新的节点
                        var endpoint = msg.Substring(" ");
                        if (!endpoint.IsNullOrEmpty())
                        {
                            // 使用新的节点
                            node = Cluster.Map(endpoint, key);
                            if (node != null) continue;
                        }
                    }

                    throw;
                }
                finally
                {
                    pool.Put(client);
                }
            } while (true);
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

        /// <summary>获取队列</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetQueue<T>(String key) => new RedisQueue<T>(this, key);

        /// <summary>获取Set</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override ICollection<T> GetSet<T>(String key) => new RedisSet<T>(this, key);
        #endregion

        #region 字符串操作
        /// <summary>附加字符串</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>返回字符串长度</returns>
        public virtual Int32 Append(String key, String value) => Execute(key, r => r.Execute<Int32>("APPEND", key, value), true);

        /// <summary>获取字符串区间</summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public virtual String GetRange(String key, Int32 start, Int32 end) => Execute(key, r => r.Execute<String>("GETRANGE", key, start, end));

        /// <summary>设置字符串区间</summary>
        /// <param name="key"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual String SetRange(String key, Int32 offset, String value) => Execute(key, r => r.Execute<String>("SETRANGE", key, offset, value), true);

        /// <summary>字符串长度</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Int32 StrLen(String key) => Execute(key, r => r.Execute<Int32>("STRLEN", key));
        #endregion

        #region 高级操作
        /// <summary>重命名指定键</summary>
        /// <param name="key"></param>
        /// <param name="newKey"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public virtual Boolean Rename(String key, String newKey, Boolean overwrite = true)
        {
            var cmd = overwrite ? "RENAME" : "RENAMENX";

            return Execute(key, r => r.Execute<Boolean>(cmd, key, newKey), true);
        }

        /// <summary>模糊搜索，支持?和*</summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public virtual String[] Search(String pattern) => Execute(null, r => r.Execute<String[]>("KEYS", pattern));

        /// <summary>模糊搜索，支持?和*</summary>
        /// <param name="pattern"></param>
        /// <param name="count"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual String[] Search(String pattern, Int32 count, ref Int32 position)
        {
            var p = position;
            var rs = Execute(null, r => r.Execute<Object[]>("SCAN", p, "MATCH", pattern + "", "COUNT", count));

            if (rs != null)
            {
                position = (rs[0] as Packet).ToStr().ToInt();

                var ps = rs[1] as Object[];
                var ss = ps.Select(e => (e as Packet).ToStr()).ToArray();
                return ss;
            }

            return null;
        }
        #endregion
    }
}