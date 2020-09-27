using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NewLife.Caching.Models;
using NewLife.Data;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Caching
{
    /// <summary>增强版Redis</summary>
    public class FullRedis : Redis
    {
        #region 静态
        //static FullRedis()
        //{
        //    ObjectContainer.Current.AutoRegister<Redis, FullRedis>();
        //}

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
        /// <summary>实例化增强版Redis</summary>
        public FullRedis() : base() { }

        /// <summary>实例化增强版Redis</summary>
        /// <param name="server"></param>
        /// <param name="password"></param>
        /// <param name="db"></param>
        public FullRedis(String server, String password, Int32 db) : base(server, password, db) { }

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

        /// <summary>获取队列，快速LIST结构，无需确认</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetQueue<T>(String key) => new RedisQueue<T>(this, key);

        /// <summary>获取可靠队列，消息需要确认</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public RedisReliableQueue<T> GetReliableQueue<T>(String key) => new RedisReliableQueue<T>(this, key);

        /// <summary>获取延迟队列</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public RedisDelayQueue<T> GetDelayQueue<T>(String key) => new RedisDelayQueue<T>(this, key);

        /// <summary>获取栈</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetStack<T>(String key) => new RedisStack<T>(this, key);

        /// <summary>获取Set</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override ICollection<T> GetSet<T>(String key) => new RedisSet<T>(this, key);

        /// <summary>获取消息流</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public RedisStream<T> GetStream<T>(String key) => new RedisStream<T>(this, key);

        /// <summary>获取有序集合</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public RedisSortedSet<T> GetSortedSet<T>(String key) => new RedisSortedSet<T>(this, key);
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

        ///// <summary>模糊搜索，支持?和*</summary>
        ///// <param name="pattern"></param>
        ///// <returns></returns>
        //public virtual String[] Search(String pattern) => Execute(null, r => r.Execute<String[]>("KEYS", pattern));

        /// <summary>模糊搜索，支持?和*</summary>
        /// <param name="model">搜索模型</param>
        /// <returns></returns>
        public virtual IEnumerable<String> Search(SearchModel model)
        {
            var count = model.Count;
            while (count > 0)
            {
                var p = model.Position;
                var rs = Execute(null, r => r.Execute<Object[]>("SCAN", p, "MATCH", model.Pattern + "", "COUNT", count));
                if (rs == null || rs.Length != 2) break;

                model.Position = (rs[0] as Packet).ToStr().ToInt();

                var ps = rs[1] as Object[];
                foreach (Packet item in ps)
                {
                    if (count-- > 0) yield return item.ToStr();
                }

                if (model.Position == 0) break;
            }
        }

        /// <summary>模糊搜索，支持?和*</summary>
        /// <param name="pattern">匹配表达式</param>
        /// <param name="count">返回个数</param>
        /// <returns></returns>
        public virtual IEnumerable<String> Search(String pattern, Int32 count) => Search(new SearchModel { Pattern = pattern, Count = count });
        #endregion

        #region 常用原生命令
        /// <summary>向列表末尾插入</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public virtual Int32 RPUSH<T>(String key, params T[] values)
        {
            var args = new List<Object>
            {
                key
            };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(key, rc => rc.Execute<Int32>("RPUSH", args.ToArray()), true);
        }

        /// <summary>向列表头部插入</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public virtual Int32 LPUSH<T>(String key, params T[] values)
        {
            var args = new List<Object>
            {
                key
            };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(key, rc => rc.Execute<Int32>("LPUSH", args.ToArray()), true);
        }

        /// <summary>从列表末尾弹出一个元素</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual T RPOP<T>(String key) => Execute(key, rc => rc.Execute<T>("RPOP", key), true);

        /// <summary>从列表末尾弹出一个元素并插入到另一个列表头部</summary>
        /// <remarks>适用于做安全队列</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">源列表名称</param>
        /// <param name="destination">元素后写入的新列表名称</param>
        /// <returns></returns>
        public virtual T RPOPLPUSH<T>(String source, String destination) => Execute(source, rc => rc.Execute<T>("RPOPLPUSH", source, destination), true);

        /// <summary>
        /// 从列表中弹出一个值，将弹出的元素插入到另外一个列表中并返回它； 如果列表没有元素会阻塞列表直到等待超时或发现可弹出元素为止。
        /// 适用于做安全队列(通过secTimeout决定阻塞时长)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">源列表名称</param>
        /// <param name="destination">元素后写入的新列表名称</param>
        /// <param name="secTimeout">设置的阻塞时长，单位为秒。设置前请确认该值不能超过FullRedis.Timeout 否则会出现异常</param>
        /// <returns></returns>
        public virtual T BRPOPLPUSH<T>(String source, String destination, Int32 secTimeout) => Execute(null, rc => rc.Execute<T>("BRPOPLPUSH", source, destination, secTimeout), true);

        /// <summary>从列表头部弹出一个元素</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual T LPOP<T>(String key) => Execute(key, rc => rc.Execute<T>("LPOP", key), true);

        /// <summary>从列表末尾弹出一个元素，阻塞</summary>
        /// <remarks>
        /// RPOP 的阻塞版本，因为这个命令会在给定list无法弹出任何元素的时候阻塞连接。
        /// 该命令会按照给出的 key 顺序查看 list，并在找到的第一个非空 list 的尾部弹出一个元素。
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <param name="secTimeout"></param>
        /// <returns></returns>
        public virtual Tuple<String, T> BRPOP<T>(String[] keys, Int32 secTimeout = 0)
        {
            var sb = new StringBuilder();
            foreach (var item in keys)
            {
                if (sb.Length <= 0)
                    sb.Append($"{item}");
                else
                    sb.Append($" {item}");
            }
            var rs = Execute(null, rc => rc.Execute<String[]>("BRPOP", sb.ToString(), secTimeout), true);
            if (rs == null || rs.Length != 2) return null;
            return new Tuple<String, T>(rs[0], rs[1].ToJsonEntity<T>());
        }

        /// <summary>从列表末尾弹出一个元素，阻塞</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="secTimeout"></param>
        /// <returns></returns>
        public virtual T BRPOP<T>(String key, Int32 secTimeout = 0)
        {
            var rs = BRPOP<T>(new[] { key }, secTimeout);
            return rs == null ? default : rs.Item2;
        }

        /// <summary>从列表头部弹出一个元素，阻塞</summary>
        /// <remarks>
        /// 命令 LPOP 的阻塞版本，这是因为当给定列表内没有任何元素可供弹出的时候，连接将被 BLPOP 命令阻塞。
        /// 当给定多个 key 参数时，按参数 key 的先后顺序依次检查各个列表，弹出第一个非空列表的头元素。
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <param name="secTimeout"></param>
        /// <returns></returns>
        public virtual Tuple<String, T> BLPOP<T>(String[] keys, Int32 secTimeout = 0)
        {
            var sb = new StringBuilder();
            foreach (var item in keys)
            {
                if (sb.Length <= 0)
                    sb.Append($"{item}");
                else
                    sb.Append($" {item}");
            }
            var rs = Execute(null, rc => rc.Execute<String[]>("BLPOP", sb.ToString(), secTimeout), true);
            if (rs == null || rs.Length != 2) return null;
            return new Tuple<String, T>(rs[0], rs[1].ToJsonEntity<T>()); //.ChangeType<T>());
        }

        /// <summary>从列表头部弹出一个元素，阻塞</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="secTimeout"></param>
        /// <returns></returns>
        public virtual T BLPOP<T>(String key, Int32 secTimeout = 0)
        {
            var rs = BLPOP<T>(new[] { key }, secTimeout);
            return rs == null ? default : rs.Item2;
        }

        /// <summary>向集合添加多个元素</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        public virtual Int32 SADD<T>(String key, params T[] members)
        {
            var args = new List<Object>
            {
                key
            };
            foreach (var item in members)
            {
                args.Add(item);
            }
            return Execute(key, rc => rc.Execute<Int32>("SADD", args.ToArray()), true);
        }

        /// <summary>向集合删除多个元素</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        public virtual Int32 SREM<T>(String key, params T[] members)
        {
            var args = new List<Object>
            {
                key
            };
            foreach (var item in members)
            {
                args.Add(item);
            }
            return Execute(key, rc => rc.Execute<Int32>("SREM", args.ToArray()), true);
        }

        /// <summary>获取所有元素</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual T[] SMEMBERS<T>(String key) => Execute(key, r => r.Execute<T[]>("SMEMBERS", key));

        /// <summary>返回集合元素个数</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Int32 SCARD(String key) => Execute(key, rc => rc.Execute<Int32>("SCARD", key));

        /// <summary>成员 member 是否是存储的集合 key的成员</summary>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual Boolean SISMEMBER<T>(String key, T member) => Execute(key, rc => rc.Execute<Boolean>("SISMEMBER", key, member));

        /// <summary>将member从source集合移动到destination集合中</summary>
        /// <param name="key"></param>
        /// <param name="dest"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual T[] SMOVE<T>(String key, String dest, T member) => Execute(key, r => r.Execute<T[]>("SMOVE", key, dest, member), true);

        /// <summary>随机获取多个</summary>
        /// <param name="key"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public virtual T[] SRANDMEMBER<T>(String key, Int32 count) => Execute(key, r => r.Execute<T[]>("SRANDMEMBER", key, count));

        /// <summary>随机获取并弹出</summary>
        /// <param name="key"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public virtual T[] SPOP<T>(String key, Int32 count) => Execute(key, r => r.Execute<T[]>("SPOP", key, count), true);
        #endregion
    }
}