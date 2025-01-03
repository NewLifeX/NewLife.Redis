using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using NewLife.Caching.Clusters;
using NewLife.Caching.Models;
using NewLife.Caching.Queues;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;

namespace NewLife.Caching;

/// <summary>增强版Redis</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/redis
/// 
/// 强烈建议保持唯一的Redis对象供多次使用，Redis内部有连接池并且支持多线程并发访问。
/// 高级功能需要引用NewLife.Redis，然后实例化FullRedis类。
/// 
/// 网络层Broken pipe异常，可以在Server设置多个一样的地址（逗号隔开），让Redis客户端在遇到网络错误时进行重试。
/// </remarks>
public class FullRedis : Redis
{
    #region 静态
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
    /// <summary>键前缀</summary>
    public String? Prefix { get; set; }

    /// <summary>自动检测集群节点。默认false</summary>
    /// <remarks>
    /// 公有云Redis一般放在代理背后，主从架构，如果开启自动检测，将会自动识别主从，导致得到无法连接的内网主从库地址。
    /// </remarks>
    public Boolean AutoDetect { get; set; }

    /// <summary>模式</summary>
    public String? Mode { get; private set; }

    /// <summary>集群。包括集群、主从复制、哨兵，一旦存在则从Cluster获取节点进行连接，而不是当前实例的Pool池</summary>
    public IRedisCluster? Cluster { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化增强版Redis</summary>
    public FullRedis() : base() { }

    /// <summary>实例化增强版Redis</summary>
    /// <param name="server"></param>
    /// <param name="password"></param>
    /// <param name="db"></param>
    public FullRedis(String server, String password, Int32 db) : base(server, password, db) { }

    /// <summary>实例化增强版Redis</summary>
    /// <param name="server">服务器</param>
    /// <param name="userName">用户名。Redis6.0支持</param>
    /// <param name="password">密码</param>
    /// <param name="db"></param>
    public FullRedis(String server, String userName, String password, Int32 db) : base(server, userName, password, db) { }

    /// <summary>实例化增强版Redis</summary>
    /// <param name="options"></param>
    public FullRedis(RedisOptions options)
    {
        if (!options.InstanceName.IsNullOrEmpty())
            Name = options.InstanceName;

        Server = options.Server;
        UserName = options.UserName;
        Password = options.Password;
        Db = options.Db;
        if (options.Timeout > 0)
            Timeout = options.Timeout;
        Prefix = options.Prefix;

        if (!options.Configuration.IsNullOrEmpty())
            Init(options.Configuration);
    }

    /// <summary>按照配置服务实例化Redis，用于NETCore依赖注入</summary>
    /// <param name="provider">服务提供者，将要解析IConfigProvider</param>
    /// <param name="name">缓存名称，也是配置中心key</param>
    public FullRedis(IServiceProvider provider, String name) : base(provider, name) { }

    /// <summary>按照配置服务实例化Redis，用于NETCore依赖注入</summary>
    /// <param name="provider">服务提供者，将要解析IConfigProvider</param>
    /// <param name="options">Redis链接配置</param>
    public FullRedis(IServiceProvider provider, RedisOptions options) : this(options) => Tracer = provider.GetService<ITracer>();

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        Cluster.TryDispose();
    }

    private String? _configOld;
    /// <summary>初始化配置</summary>
    /// <param name="config"></param>
    public override void Init(String config)
    {
        if (config == _configOld) return;

        // 更换Redis连接字符串时，清空相关信息
        if (!_configOld.IsNullOrEmpty())
        {
            Mode = null;
            Cluster = null;
        }

        base.Init(config);

        _initCluster = false;

        if (config.IsNullOrEmpty()) return;

        var dic = ParseConfig(config);
        if (dic.Count > 0)
        {
            if (dic.TryGetValue("Prefix", out var str))
                Prefix = str;
            if (dic.TryGetValue("AutoDetect", out str))
                AutoDetect = str.ToBoolean();
        }

        _configOld = config;
    }
    #endregion

    #region 方法
    private Boolean _initCluster;
    /// <summary>初始化集群</summary>
    public void InitCluster()
    {
        if (_initCluster) return;
        lock (this)
        {
            if (_initCluster) return;

            // 访问一次info信息，解析工作模式，以判断是否集群
            var info = Info;
            if (info != null)
            {
                if (info.TryGetValue("redis_mode", out var mode)) Mode = mode;
                // 主从复制时，仅master有connected_slaves，因此Server地址必须把master节点放在第一位
                info.TryGetValue("role", out var role);
                info.TryGetValue("connected_slaves", out var connected_slaves);

                if (!AutoDetect)
                {
                    Cluster = null;
                }
                else
                {
                    WriteLog("Init[{0}]：mode={1}, role={2}", Name, mode, role);

                    // 集群模式初始化节点
                    if (mode == "cluster")
                    {
                        var cluster = new RedisCluster(this);
                        cluster.StartMonitor();
                        Cluster = cluster;
                    }
                    else if (mode.EqualIgnoreCase("sentinel"))
                    {
                        var cluster = new RedisSentinel(this) { SetHostServer = true };
                        cluster.StartMonitor();
                        Cluster = cluster;
                    }
                    else if (mode.EqualIgnoreCase("standalone") && (connected_slaves.ToInt() > 0 || role == "slave"))
                    {
                        var cluster = new RedisReplication(this) { SetHostServer = true };
                        cluster.StartMonitor();
                        Cluster = cluster;
                    }
                    // 特别支持kvrocks（底层RockDB）
                    else if (mode.IsNullOrEmpty() && role.EqualIgnoreCase("master", "slave"))
                    {
                        try
                        {
                            var cluster = new RedisCluster(this);
                            cluster.StartMonitor();
                            Cluster = cluster;
                        }
                        catch
                        {
                            var cluster = new RedisReplication(this) { SetHostServer = true };
                            cluster.StartMonitor();
                            Cluster = cluster;
                        }
                    }
                }
            }

            _initCluster = true;
        }
    }

    private ConcurrentDictionary<String, IPool<RedisClient>> _pools = new();
    /// <summary>获取指定节点的连接池</summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public IPool<RedisClient> GetPool(IRedisNode node)
    {
        return _pools.GetOrAdd(node.EndPoint, k =>
        {
            WriteLog("使用Redis节点：{0}", k);

            return CreatePool(() => new RedisClient(this, k) { Name = k.Replace(':', '-'), Log = ClientLog });
        });
    }

    /// <summary>获取经前缀处理后的键名</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public String GetKey(String key) => !Prefix.IsNullOrEmpty() ? key.EnsureStart(Prefix) : key;

    /// <summary>重载执行，支持集群</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">用于选择集群节点的key</param>
    /// <param name="func">命令函数</param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public override T Execute<T>(String key, Func<RedisClient, String, T> func, Boolean write = false)
    {
        InitCluster();

        key = GetKey(key);

        // 如果不支持集群，直接返回
        if (Cluster == null) return base.Execute(key, func, write);

        var node = Cluster.SelectNode(key, write);
        //?? throw new XException($"集群[{Name}]没有可用节点");
        if (node == null) return base.Execute(key, func, write);

        return ExecuteOnNode(key, func, write, Cluster, node);
    }

    /// <summary>在指定集群节点上执行命令</summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="key">用于选择集群节点的key</param>
    /// <param name="func">命令函数</param>
    /// <param name="write">是否写入操作</param>
    /// <param name="cluster">集群</param>
    /// <param name="node">选中的节点</param>
    /// <returns></returns>
    public virtual T ExecuteOnNode<T, TKey>(TKey key, Func<RedisClient, TKey, T> func, Boolean write, IRedisCluster cluster, IRedisNode node)
    {
        // 统计性能
        var sw = Counter?.StartCount();

        var i = 0;
        var delay = 500;
        do
        {
            var pool = GetPool(node);

            // 每次重试都需要重新从池里借出连接
            var client = pool.Get();
            try
            {
                client.Reset();
                var rs = func(client, key);

                cluster.ResetNode(node);

                return rs;
            }
            //catch (RedisException) { throw; }
            catch (Exception ex)
            {
                if (++i >= Retry) throw;

                // 销毁连接
                client.TryDispose();

                // 使用新的节点
                var k = key is IList<String> ks ? ks[0] : key + "";
                var node2 = cluster.ReselectNode(k, write, node, ex);
                if (node2 != null)
                    node = node2;
                else
                    Thread.Sleep(delay *= 2);
            }
            finally
            {
                pool.Return(client);

                Counter?.StopCount(sw);
            }
        } while (true);
    }

    /// <summary>在指定集群节点上执行命令</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys">用于选择集群节点的key</param>
    /// <param name="func">命令函数</param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public virtual T[] Execute<T>(String[] keys, Func<RedisClient, String[], T> func, Boolean write)
    {
        if (keys == null || keys.Length == 0) return [];

        InitCluster();

        keys = keys.Select(GetKey).ToArray();

        // 如果不支持集群，或者只有一个key，直接执行
        if (Cluster == null || keys.Length == 1) return [Execute(keys.FirstOrDefault(), (rds, k) => func(rds, keys), write)];

        // 计算每个key所在的节点
        var dic = new Dictionary<String, List<String>>();
        var nodes = new Dictionary<String, IRedisNode>();
        foreach (var key in keys)
        {
            var node = Cluster.SelectNode(key, true);
            if (node != null)
            {
                var k = node.EndPoint;
                if (!dic.TryGetValue(k, out var list))
                {
                    dic[k] = list = [];
                    nodes[k] = node;
                }

                if (!list.Contains(key)) list.Add(key);
            }
        }

        // 分组批量执行
        var rs = new List<T>();
        foreach (var item in dic)
        {
            if (nodes.TryGetValue(item.Key, out var node))
            {
                rs.Add(ExecuteOnNode<T, String[]>([.. item.Value], func, write, Cluster, node));
            }
        }

        return [.. rs];
    }

    /// <summary>直接执行命令，不考虑集群读写</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="func">回调函数</param>
    /// <param name="node">回调函数</param>
    /// <returns></returns>
    public virtual TResult Execute<TResult>(Func<RedisClient, TResult> func, IRedisNode? node)
    {
        // 每次重试都需要重新从池里借出连接
        var pool = node != null ? GetPool(node) : Pool;
        var client = pool.Get();
        try
        {
            client.Reset();
            return func(client);
        }
        catch (Exception ex)
        {
            if (ex is SocketException or IOException)
            {
                // 销毁连接
                client.TryDispose();
            }

            throw;
        }
        finally
        {
            pool.Return(client);
        }
    }

    /// <summary>重载执行，支持集群</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public override async Task<T> ExecuteAsync<T>(String key, Func<RedisClient, String, Task<T>> func, Boolean write = false)
    {
        InitCluster();

        key = GetKey(key);

        // 如果不支持集群，直接返回
        if (Cluster == null) return await base.ExecuteAsync<T>(key, func, write).ConfigureAwait(false);

        var node = Cluster.SelectNode(key, write);
        //?? throw new XException($"集群[{Name}]没有可用节点");
        if (node == null) return await base.ExecuteAsync<T>(key, func, write).ConfigureAwait(false);

        // 统计性能
        var sw = Counter?.StartCount();

        var i = 0;
        var delay = 500;
        do
        {
            var pool = GetPool(node);

            // 每次重试都需要重新从池里借出连接
            var client = pool.Get();
            try
            {
                client.Reset();
                var rs = await func(client, key).ConfigureAwait(false);

                return rs;
            }
            //catch (RedisException) { throw; }
            catch (Exception ex)
            {
                if (++i >= Retry) throw;

                // 销毁连接
                client.TryDispose();

                // 使用新的节点
                var node2 = Cluster.ReselectNode(key, write, node, ex);
                if (node2 != null)
                    node = node2;
                else
                    Thread.Sleep(delay *= 2);
            }
            finally
            {
                pool.Return(client);

                Counter?.StopCount(sw);
            }
        } while (true);
    }
    #endregion

    #region 子库
    /// <summary>为同一服务器创建不同Db的子级库</summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public override Redis CreateSub(Int32 db)
    {
        var rds = (base.CreateSub(db) as FullRedis)!;
        rds.AutoDetect = AutoDetect;
        rds.Prefix = Prefix;

        return rds;
    }
    #endregion

    #region 基础操作
    /// <summary>批量移除缓存项</summary>
    /// <param name="keys">键集合</param>
    public override Int32 Remove(params String[] keys)
    {
        if (keys == null || keys.Length == 0) return 0;

        keys = keys.Select(GetKey).ToArray();
        if (keys.Length == 1) return base.Remove(keys[0]);

        InitCluster();

        if (Cluster != null)
        {
            return Execute(keys, (rds, ks) => rds.Execute<Int32>("DEL", ks), true).Sum();
        }
        else
        {
            return Execute(keys.FirstOrDefault(), (rds, k) => rds.Execute<Int32>("DEL", keys), true);
        }
    }
    #endregion

    #region 集合操作
    /// <summary>批量获取缓存项</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public override IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
    {
        if (keys == null || !keys.Any()) return new Dictionary<String, T>();

        var keys2 = keys.ToArray();

        keys2 = keys2.Select(GetKey).ToArray();
        if (keys2.Length == 1 || Cluster == null) return base.GetAll<T>(keys2);

        //Execute(keys.FirstOrDefault(), (rds, k) => rds.GetAll<T>(keys));
        var rs = Execute(keys2, (rds, ks) => rds.GetAll<T>(ks), false);

        var dic = new Dictionary<String, T?>();
        foreach (var item in rs)
        {
            if (item != null && item.Count > 0)
            {
                foreach (var kv in item)
                {
                    dic[kv.Key] = kv.Value;
                }
            }
        }

        return dic;
    }

    /// <summary>批量设置缓存项</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
    public override void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1)
    {
        if (values == null || values.Count == 0) return;

        if (expire < 0) expire = Expire;

        // 优化少量读取
        if (values.Count <= 2)
        {
            foreach (var item in values)
            {
                Set(item.Key, item.Value, expire);
            }
            return;
        }

        var keys = values.Keys.Select(GetKey).ToArray();
        //Execute(values.FirstOrDefault().Key, (rds, k) => rds.SetAll(values), true);
        var rs = Execute(keys, (rds, ks) => rds.SetAll(ks.ToDictionary(e => e, e => values[e])), true);

        // 使用管道批量设置过期时间
        if (expire > 0)
        {
            var ts = TimeSpan.FromSeconds(expire);

            StartPipeline();
            try
            {
                foreach (var item in keys)
                {
                    SetExpire(item, ts);
                }
            }
            finally
            {
                StopPipeline(true);
            }
        }
    }

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

   /// <summary>
   /// 获取哈希表所有数据
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="key"></param>
   /// <returns></returns>
   public IDictionary<String, T> GetHashAll<T>(String key)
   {
       var hashMap = new RedisHash<String, T>(this, key);
       var nCount = hashMap!.Count();
       var sModel = new SearchModel()
       {
           Pattern = "*",
           Position = 0,
           Count = nCount
       };
       return hashMap!.Search(sModel).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
   }
   
    /// <summary>获取队列，快速LIST结构，无需确认</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">消息队列主题</param>
    /// <returns></returns>
    public override IProducerConsumer<T> GetQueue<T>(String topic) => new RedisQueue<T>(this, topic);

    /// <summary>获取可靠队列，消息需要确认</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">消息队列主题</param>
    /// <returns></returns>
    public virtual RedisReliableQueue<T> GetReliableQueue<T>(String topic) => new(this, topic);

    /// <summary>获取延迟队列</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">消息队列主题</param>
    /// <returns></returns>
    public virtual RedisDelayQueue<T> GetDelayQueue<T>(String topic) => new(this, topic);

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
    /// <param name="topic">消息队列主题</param>
    /// <returns></returns>
    public virtual RedisStream<T> GetStream<T>(String topic) => new(this, topic);

    /// <summary>获取有序集合</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual RedisSortedSet<T> GetSortedSet<T>(String key) => new(this, key);
    #endregion

    #region 字符串操作
    /// <summary>附加字符串</summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>返回字符串长度</returns>
    public virtual Int32 Append(String key, String value) => Execute(key, (r, k) => r.Execute<Int32>("APPEND", k, value), true);

    /// <summary>获取字符串区间</summary>
    /// <param name="key"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public virtual String? GetRange(String key, Int32 start, Int32 end) => Execute(key, (r, k) => r.Execute<String>("GETRANGE", k, start, end));

    /// <summary>设置字符串区间</summary>
    /// <param name="key"></param>
    /// <param name="offset"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual String? SetRange(String key, Int32 offset, String value) => Execute(key, (r, k) => r.Execute<String>("SETRANGE", k, offset, value), true);

    /// <summary>字符串长度</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual Int32 StrLen(String key) => Execute(key, (r, k) => r.Execute<Int32>("STRLEN", k));
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
        newKey = GetKey(newKey);

        var rs = Execute(key, (r, k) => r.Execute<String>(cmd, k, newKey), true);
        if (rs.IsNullOrEmpty()) return false;

        return rs == "OK" || rs.ToInt() > 0;
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
        InitCluster();

        if (Cluster != null)
            return Cluster.Nodes.SelectMany(x => Scan(x));
        else
            return Scan();

        IEnumerable<String> Scan(IRedisNode? node = null)
        {
            var count = model.Count;
            while (count > 0)
            {
                var p = model.Position;
                var rs = Execute(r => r.Execute<Object[]>("SCAN", p, "MATCH", GetKey(model.Pattern + ""), "COUNT", count), node);
                if (rs == null || rs.Length != 2) break;

                model.Position = (rs[0] as IPacket)?.ToStr().ToInt() ?? 0;

                if (rs[1] is Object[] ps)
                {
                    foreach (IPacket item in ps)
                    {
                        if (count-- > 0) yield return item.ToStr();
                    }
                }

                if (model.Position == 0) break;
            }
        }
    }

    /// <summary>模糊搜索，支持?和*</summary>
    /// <param name="pattern">匹配表达式</param>
    /// <param name="count">返回个数</param>
    /// <returns></returns>
    public virtual IEnumerable<String> Search(String pattern, Int32 count) => Search(new SearchModel { Pattern = pattern, Count = count });
    #endregion

    #region 事务

    /// <summary>申请分布式锁</summary>
    /// <param name="key">要锁定的key</param>
    /// <param name="msTimeout">锁等待时间，单位毫秒</param>
    /// <returns></returns>
    public override IDisposable? AcquireLock(String key, Int32 msTimeout)
    {
        key = GetKey(key);
        return base.AcquireLock(key, msTimeout);
    }

    /// <summary>申请分布式锁</summary>
    /// <param name="key">要锁定的key</param>
    /// <param name="msTimeout">锁等待时间，申请加锁时如果遇到冲突则等待的最大时间，单位毫秒</param>
    /// <param name="msExpire">锁过期时间，超过该时间如果没有主动释放则自动释放锁，必须整数秒，单位毫秒</param>
    /// <param name="throwOnFailure">失败时是否抛出异常，如果不抛出异常，可通过返回null得知申请锁失败</param>
    /// <returns></returns>
    public override IDisposable? AcquireLock(String key, Int32 msTimeout, Int32 msExpire, Boolean throwOnFailure)
    {
        key = GetKey(key);
        return base.AcquireLock(key, msTimeout, msExpire, throwOnFailure);
    }
    #endregion

    #region 常用原生命令
    /// <summary>获取指定键的数据结构类型，如stream</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual String? TYPE(String key) => Execute(key, (rc, k) => rc.Execute<String>("TYPE", k), false);

    /// <summary>向列表末尾插入</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public virtual Int32 RPUSH<T>(String key, params T[] values)
    {
        // 这里提前打包参数，需要处理key前缀。其它普通情况由Execute处理
        key = GetKey(key);
        var args = new List<Object>
        {
            key
        };
        foreach (var item in values)
        {
            if (item != null) args.Add(item);
        }
        return Execute(key, (rc, k) => rc.Execute<Int32>("RPUSH", args.ToArray()), true);
    }

    /// <summary>向列表头部插入</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public virtual Int32 LPUSH<T>(String key, params T[] values)
    {
        // 这里提前打包参数，需要处理key前缀。其它普通情况由Execute处理
        key = GetKey(key);
        var args = new List<Object>
        {
            key
        };
        foreach (var item in values)
        {
            if (item != null) args.Add(item);
        }
        return Execute(key, (rc, k) => rc.Execute<Int32>("LPUSH", args.ToArray()), true);
    }

    /// <summary>从列表末尾弹出一个元素</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual T? RPOP<T>(String key) => Execute(key, (rc, k) => rc.Execute<T>("RPOP", k), true);

    /// <summary>从列表末尾弹出一个元素并插入到另一个列表头部</summary>
    /// <remarks>适用于做安全队列</remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">源列表名称</param>
    /// <param name="destination">元素后写入的新列表名称</param>
    /// <returns></returns>
    public virtual T? RPOPLPUSH<T>(String source, String destination) => Execute(source, (rc, k) => rc.Execute<T>("RPOPLPUSH", k, GetKey(destination)), true);

    /// <summary>
    /// 从列表中弹出一个值，将弹出的元素插入到另外一个列表中并返回它； 如果列表没有元素会阻塞列表直到等待超时或发现可弹出元素为止。
    /// 适用于做安全队列(通过secTimeout决定阻塞时长)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">源列表名称</param>
    /// <param name="destination">元素后写入的新列表名称</param>
    /// <param name="secTimeout">设置的阻塞时长，单位为秒。设置前请确认该值不能超过FullRedis.Timeout 否则会出现异常</param>
    /// <returns></returns>
    public virtual T? BRPOPLPUSH<T>(String source, String destination, Int32 secTimeout) => Execute(source, (rc, k) => rc.Execute<T>("BRPOPLPUSH", k, GetKey(destination), secTimeout), true);

    /// <summary>从列表头部弹出一个元素</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual T? LPOP<T>(String key) => Execute(key, (rc, k) => rc.Execute<T>("LPOP", k), true);

    /// <summary>从列表末尾弹出一个元素，阻塞</summary>
    /// <remarks>
    /// RPOP 的阻塞版本，因为这个命令会在给定list无法弹出任何元素的时候阻塞连接。
    /// 该命令会按照给出的 key 顺序查看 list，并在找到的第一个非空 list 的尾部弹出一个元素。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys"></param>
    /// <param name="secTimeout"></param>
    /// <returns></returns>
    public virtual Tuple<String, T?>? BRPOP<T>(String[] keys, Int32 secTimeout = 0)
    {
        var sb = new StringBuilder();
        foreach (var item in keys)
        {
            var key = GetKey(item);
            if (sb.Length <= 0)
                sb.Append($"{key}");
            else
                sb.Append($" {key}");
        }
        var rs = Execute(keys[0], (rc, k) => rc.Execute<String[]>("BRPOP", sb.ToString(), secTimeout), true);
        if (rs == null || rs.Length != 2) return null;

        if (typeof(T) == typeof(String)) return new Tuple<String, T?>(rs[0], (T)(Object)rs[1]);

        return new Tuple<String, T?>(rs[0], (T?)JsonHost.Read(rs[1], typeof(T)));
    }

    /// <summary>从列表末尾弹出一个元素，阻塞</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="secTimeout"></param>
    /// <returns></returns>
    public virtual T? BRPOP<T>(String key, Int32 secTimeout = 0)
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
    public virtual Tuple<String, T?>? BLPOP<T>(String[] keys, Int32 secTimeout = 0)
    {
        var sb = new StringBuilder();
        foreach (var item in keys)
        {
            var key = GetKey(item);
            if (sb.Length <= 0)
                sb.Append($"{key}");
            else
                sb.Append($" {key}");
        }
        var rs = Execute(keys[0], (rc, k) => rc.Execute<String[]>("BLPOP", sb.ToString(), secTimeout), true);
        if (rs == null || rs.Length != 2) return null;

        if (typeof(T) == typeof(String)) return new Tuple<String, T?>(rs[0], (T)(Object)rs[1]);

        return new Tuple<String, T?>(rs[0], (T?)JsonHost.Read(rs[1], typeof(T)));
    }

    /// <summary>从列表头部弹出一个元素，阻塞</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="secTimeout"></param>
    /// <returns></returns>
    public virtual T? BLPOP<T>(String key, Int32 secTimeout = 0)
    {
        var rs = BLPOP<T>([key], secTimeout);
        return rs == null ? default : rs.Item2;
    }

    /// <summary>向集合添加多个元素</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="members"></param>
    /// <returns></returns>
    public virtual Int32 SADD<T>(String key, params T[] members)
    {
        key = GetKey(key);
        var args = new List<Object>
        {
            key
        };
        foreach (var item in members)
        {
            if (item != null) args.Add(item);
        }
        return Execute(key, (rc, k) => rc.Execute<Int32>("SADD", args.ToArray()), true);
    }

    /// <summary>向集合删除多个元素</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="members"></param>
    /// <returns></returns>
    public virtual Int32 SREM<T>(String key, params T[] members)
    {
        key = GetKey(key);
        var args = new List<Object>
        {
            key
        };
        foreach (var item in members)
        {
            if (item != null) args.Add(item);
        }
        return Execute(key, (rc, k) => rc.Execute<Int32>("SREM", args.ToArray()), true);
    }

    /// <summary>获取所有元素</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual T[]? SMEMBERS<T>(String key) => Execute(key, (r, k) => r.Execute<T[]>("SMEMBERS", k));

    /// <summary>返回集合元素个数</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual Int32 SCARD(String key) => Execute(key, (rc, k) => rc.Execute<Int32>("SCARD", k));

    /// <summary>成员 member 是否是存储的集合 key的成员</summary>
    /// <param name="key"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    public virtual Int32 SISMEMBER<T>(String key, T member) => Execute(key, (rc, k) => rc.Execute<Int32>("SISMEMBER", k, member));

    /// <summary>将member从source集合移动到destination集合中</summary>
    /// <param name="key"></param>
    /// <param name="dest"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    public virtual T[]? SMOVE<T>(String key, String dest, T member) => Execute(key, (r, k) => r.Execute<T[]>("SMOVE", k, dest, member), true);

    /// <summary>随机获取多个</summary>
    /// <param name="key"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public virtual T[]? SRANDMEMBER<T>(String key, Int32 count) => Execute(key, (r, k) => r.Execute<T[]>("SRANDMEMBER", k, count));

    /// <summary>随机获取并弹出</summary>
    /// <param name="key"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public virtual T[]? SPOP<T>(String key, Int32 count) => Execute(key, (r, k) => r.Execute<T[]>("SPOP", k, count), true);
    #endregion
}
