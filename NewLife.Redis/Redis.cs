﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife.Collections;
using NewLife.Configuration;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;

namespace NewLife.Caching;

/// <summary>Redis客户端</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/redis
/// 
/// 强烈建议保持唯一的Redis对象供多次使用，Redis内部有连接池并且支持多线程并发访问。
/// 高级功能需要引用NewLife.Redis，然后实例化FullRedis类。
/// 
/// 网络层Broken pipe异常，可以在Server设置多个一样的地址（逗号隔开），让Redis客户端在遇到网络错误时进行重试。
/// </remarks>
public class Redis : Cache, IConfigMapping, ILogFeature
{
    #region 属性
    /// <summary>服务器，带端口。例如127.0.0.1:6397，支持逗号分隔的多地址，网络异常时，自动切换到其它节点，60秒后切回来</summary>
    public String? Server { get; set; }

    /// <summary>用户名。Redis6.0支持</summary>
    public String? UserName { get; set; }

    /// <summary>密码</summary>
    public String? Password { get; set; }

    /// <summary>目标数据库。默认0</summary>
    public Int32 Db { get; set; }

    /// <summary>读写超时时间。默认3000ms</summary>
    public Int32 Timeout { get; set; } = 3_000;

    /// <summary>出错重试次数。如果出现错误，可以重试的次数，默认3</summary>
    public Int32 Retry { get; set; } = 3;

    /// <summary>不可用节点的屏蔽时间。默认10秒</summary>
    public Int32 ShieldingTime { get; set; } = 10;

    /// <summary>完全管道。读取操作是否合并进入管道，默认false</summary>
    public Boolean FullPipeline { get; set; }

    /// <summary>自动管道。管道操作达到一定数量时，自动提交，默认0</summary>
    public Int32 AutoPipeline { get; set; }

    /// <summary>编码器。决定对象存储在redis中的格式，默认json</summary>
    public IPacketEncoder Encoder { get; set; } = new RedisJsonEncoder();

    /// <summary>Json序列化主机</summary>
    public IJsonHost JsonHost { get; set; } = null!;

    /// <summary>SSL协议。决定是否启用SSL连接，默认None不启用</summary>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>X509证书。用于SSL连接时验证证书指纹，可以直接加载pem证书文件，未指定时不验证证书</summary>
    /// <remarks>var cert = new X509Certificate2("abc.pem", "pass");</remarks>
    public X509Certificate2? Certificate { get; set; }

    /// <summary>失败时抛出异常。默认true</summary>
    public Boolean ThrowOnFailure { get; set; } = true;

    /// <summary>最大消息大小。超过时抛出异常，默认1024*1024字节，行业标准是32k算大Value，这里超过1m报错</summary>
    public Int32 MaxMessageSize { get; set; } = 1024 * 1024;

    /// <summary>性能计数器</summary>
    public PerfCounter? Counter { get; set; }

    /// <summary>性能跟踪器。仅记录read/write，形成调用链，key在tag中，没有记录异常。高速海量操作时不建议开启</summary>
    public ITracer? Tracer { get; set; }

    private IDictionary<String, String>? _Info;
    /// <summary>服务器信息</summary>
    public IDictionary<String, String> Info => _Info ??= GetInfo();

    private Version? _Version;
    /// <summary>Redis版本。可用于判断某些指令是否可用</summary>
    public Version Version
    {
        get
        {
            if (_Version == null)
            {
                var inf = Info;
                if (inf != null && inf.TryGetValue("redis_version", out var ver))
                {
                    if (!ver.IsNullOrEmpty() && Version.TryParse(ver, out var version))
                        _Version = version;
                }
                _Version ??= new Version();
            }

            return _Version;
        }
    }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public Redis() : base()
    {
        // 初始化Json序列化
        JsonHost ??= RedisJsonEncoder.GetJsonHost();
    }

    /// <summary>实例化Redis，指定服务器地址、密码、库</summary>
    /// <param name="server"></param>
    /// <param name="password"></param>
    /// <param name="db"></param>
    public Redis(String server, String password, Int32 db) : this()
    {
        // 有人多输入了一个空格，酿成大祸
        Server = server?.Trim();
        Password = password?.Trim();
        Db = db;
    }

    /// <summary>实例化Redis，指定服务器地址、用户、密码、库</summary>
    /// <param name="server"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="db"></param>
    public Redis(String server, String username, String password, Int32 db) : this()
    {
        // 有人多输入了一个空格，酿成大祸
        Server = server?.Trim();
        UserName = username?.Trim();
        Password = password?.Trim();
        Db = db;
    }

    /// <summary>按照配置服务实例化Redis，用于NETCore依赖注入</summary>
    /// <param name="provider">服务提供者，将要解析IConfigProvider</param>
    /// <param name="name">缓存名称，也是配置中心key</param>
    public Redis(IServiceProvider provider, String name) : this()
    {
        Name = name;
        Tracer = provider.GetService<ITracer>();
        var log = provider.GetService<ILog>();
        if (log != null) Log = log;

        var config = provider.GetService<IConfigProvider>();
        config ??= JsonConfigProvider.LoadAppSettings();
        config.Bind(this, true, name);
    }

    /// <summary>实例化Redis，指定名称，支持从环境变量Redis_{Name}读取配置，或者逐个属性配置</summary>
    /// <param name="name"></param>
    public Redis(String name) : this()
    {
        if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));

        Name = name;

        // 从环境变量加载连接字符
        foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
        {
            if (item.Key is String key && item.Value is String value && key.EqualIgnoreCase($"Redis_{name}"))
            {
                Init(value);
            }
        }
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        try
        {
            Commit();
        }
        catch { }

        _Pool.TryDispose();
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{Name} Server={Server} Db={Db}";
    #endregion

    #region 配置方法
    private String? _configOld;
    /// <summary>使用连接字符串初始化</summary>
    /// <param name="config"></param>
    public override void Init(String config)
    {
        if (config.IsNullOrEmpty()) return;

        if (config == _configOld) return;

        if (!_configOld.IsNullOrEmpty() && XTrace.Log.Level <= LogLevel.Debug)
            XTrace.WriteLine("Redis[{0}]连接字符串改变！", Name);

        // 解密连接字符串中被保护的密码。解密密钥位于配置文件ProtectedKey，或者环境变量中
        var connStr = config;
        var pk = ProtectedKey.Instance;
        if (pk != null && pk.Secret != null) connStr = pk.Unprotect(connStr);

        // 默认分号分割，旧版逗号分隔。可能只有一个server=后续多个含逗号的地址
        var dic = ParseConfig(connStr);
        if (dic.Count > 0)
        {
            Server = dic["Server"]?.Trim();
            UserName = dic["UserName"]?.Trim();
            Password = dic["Password"]?.Trim();
            //Db = dic["Db"].ToInt();
            if (dic.TryGetValue("Db", out var str))
                Db = str.ToInt();

            if (Server.IsNullOrEmpty() && dic.TryGetValue("[0]", out var svr)) Server = svr;

            // 连接字符串可能独立写了port
            var port = dic["Port"].ToInt();
            if (port > 0 && !Server.IsNullOrEmpty() && !Server.Contains(':')) Server += ":" + port;

            if (dic.TryGetValue("Timeout", out str))
                Timeout = str.ToInt();
            else if (dic.TryGetValue("responseTimeout", out str))
                Timeout = str.ToInt();
            else if (dic.TryGetValue("connectTimeout", out str))
                Timeout = str.ToInt();

            if (dic.TryGetValue("ThrowOnFailure", out str))
                ThrowOnFailure = str.ToBoolean();

            if (dic.TryGetValue("MaxMessageSize", out str) && str.ToInt(-1) >= 0)
                MaxMessageSize = str.ToInt();

            if (dic.TryGetValue("Expire", out str) && str.ToInt(-1) >= 0)
                Expire = str.ToInt();
        }

        // 更换Redis连接字符串时，清空原连接池
        _servers = null;
        if (!_configOld.IsNullOrEmpty())
        {
            _Pool = null;
            _Info = null;
        }

        _configOld = config;

        // 初始化Json序列化
        JsonHost ??= RedisJsonEncoder.GetJsonHost();
        if (Encoder is RedisJsonEncoder encoder)
            encoder.JsonHost = JsonHost;
    }

    /// <summary>分析配置连接字符串</summary>
    /// <param name="connStr"></param>
    /// <returns></returns>
    protected IDictionary<String, String> ParseConfig(String connStr)
    {
        // 默认分号分割，旧版逗号分隔。可能只有一个server=后续多个含逗号的地址
        var dic =
            connStr.Contains(';') || connStr.Split('=').Length <= 2 ?
            connStr.SplitAsDictionary("=", ";", true) :
            connStr.SplitAsDictionary("=", ",", true);

        return dic;
    }

    void IConfigMapping.MapConfig(IConfigProvider provider, IConfigSection section)
    {
        if (section != null && section.Value != null) Init(section.Value);
    }
    #endregion

    #region 客户端池
    private class MyPool : ObjectPool<RedisClient>
    {
        public Redis Instance { get; set; } = null!;

        public Func<RedisClient> Callback { get; set; } = null!;

        protected override RedisClient OnCreate() => Callback();

        protected override Boolean OnGet(RedisClient value)
        {
            // 借出时清空残留
            value.Reset();

            return base.OnGet(value);
        }
    }

    private NetUri[]? _servers;
    private Int32 _idxServer;
    private Int32 _idxLast = -1;
    private DateTime _nextTrace;

    /// <summary>获取解析后的地址列表</summary>
    /// <returns></returns>
    public NetUri[] GetServices()
    {
        // 初始化服务器地址列表
        var svrs = _servers;
        if (svrs != null) return svrs;

        var server = Server?.Trim();
        if (server.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Server));

        var ss = server.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var uris = new NetUri[ss.Length];
        for (var i = 0; i < ss.Length; i++)
        {
            var svr2 = ss[i];
            if (!svr2.Contains("://")) svr2 = "tcp://" + svr2;

            var uri = new NetUri(svr2);
            if (uri.Port == 0) uri.Port = 6379;
            uris[i] = uri;
        }

        return _servers = uris;
    }

    /// <summary>设置服务端地址列表</summary>
    /// <param name="services"></param>
    public void SetSevices(NetUri[] services) => _servers = services;

    /// <summary>创建连接客户端</summary>
    /// <returns></returns>
    protected virtual RedisClient OnCreate()
    {
        var server = Server?.Trim();
        if (server.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Server));

        // 初始化服务器地址列表
        var svrs = GetServices();

        // 一定时间后，切换回来主节点
        var idx = _idxServer;
        if (idx > 0)
        {
            if (idx >= svrs.Length) idx %= svrs.Length;

            var now = DateTime.Now;
            if (_nextTrace.Year < 2000) _nextTrace = now.AddSeconds(300);
            if (now > _nextTrace)
            {
                _nextTrace = DateTime.MinValue;

                idx = _idxServer = 0;
            }
        }

        if (idx != _idxLast)
        {
            WriteLog("使用Redis服务器：{0}", svrs[idx % svrs.Length]);

            _idxLast = idx;
        }

        // 选择服务器，修改实例名，后面用于埋点
        var svr = svrs[idx % svrs.Length];
        if (Name.IsNullOrEmpty() || Name.EqualIgnoreCase("Redis", "FullRedis")) Name = svr.Host ?? svr.Address.ToString();

        var rc = new RedisClient(this, svr)
        {
            Log = ClientLog
        };
        //if (rds.Db > 0) rc.Select(rds.Db);

        return rc;
    }

    private IPool<RedisClient>? _Pool;
    /// <summary>连接池</summary>
    public IPool<RedisClient> Pool
    {
        get
        {
            if (_Pool != null) return _Pool;
            lock (this)
            {
                if (_Pool != null) return _Pool;

                return _Pool = CreatePool(OnCreate);
            }
        }
    }

    /// <summary>创建连接池</summary>
    /// <param name="onCreate"></param>
    /// <returns></returns>
    protected virtual IPool<RedisClient> CreatePool(Func<RedisClient> onCreate)
    {
        var pool = new MyPool
        {
            Name = Name + "Pool",
            Instance = this,
            Min = 10,
            Max = 100000,
            IdleTime = 30,
            AllIdleTime = 300,
            Log = ClientLog,

            Callback = onCreate,
        };

        return pool;
    }
    #endregion

    #region 方法
    /// <summary>执行命令，经过管道。FullRedis中还会考虑Cluster分流</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="key">命令key，用于选择集群节点</param>
    /// <param name="func">回调函数</param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public virtual TResult Execute<TResult>(String key, Func<RedisClient, String, TResult> func, Boolean write = false)
    {
        // 写入或完全管道模式时，才处理管道操作
        if (write || FullPipeline)
        {
            // 管道模式直接执行
            var rds = _client.Value;
            if (rds == null && AutoPipeline > 0) rds = StartPipeline();
            if (rds != null)
            {
                var rs = func(rds, key);

                // 命令数足够，自动提交
                if (AutoPipeline > 0 && rds.PipelineCommands >= AutoPipeline)
                {
                    StopPipeline(true);
                    StartPipeline();
                }

                return rs;
            }
        }

        // 读操作遇到未完成管道队列时，立马执行管道操作
        if (!write) StopPipeline(true);

        // 统计性能
        var sw = Counter?.StartCount();

        var i = 0;
        var delay = 500;
        do
        {
            // 每次重试都需要重新从池里借出连接
            var client = Pool.Get();
            try
            {
                client.Reset();
                return func(client, key);
            }
            catch (RedisException) { throw; }
            catch (Exception ex)
            {
                if (++i >= Retry) throw;

                // 销毁连接
                client.TryDispose();

                // 网络异常时，自动切换到其它节点
                if (ex is AggregateException ae) ex = ae.InnerException;
                if (NoDelay(ex))
                {
                    if (_servers == null || i >= _servers.Length) throw;
                    _idxServer++;
                }
                else
                    Thread.Sleep(delay *= 2);
            }
            finally
            {
                Pool.Return(client);

                Counter?.StopCount(sw);
            }
        } while (true);
    }

    /// <summary>网络IO类异常，无需等待，因为遇到此类异常时重发请求也是错</summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    internal Boolean NoDelay(Exception ex) => ex is SocketException or IOException or InvalidOperationException;

    /// <summary>直接执行命令，不考虑集群读写</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="func">回调函数</param>
    /// <returns></returns>
    public virtual TResult Execute<TResult>(Func<RedisClient, TResult> func)
    {
        // 统计性能
        var sw = Counter?.StartCount();

        var i = 0;
        var delay = 500;
        do
        {
            // 每次重试都需要重新从池里借出连接
            var pool = Pool;
            var client = pool.Get();
            try
            {
                client.Reset();
                return func(client);
            }
            catch (RedisException) { throw; }
            catch (Exception ex)
            {
                if (++i >= Retry) throw;

                // 销毁连接
                client.TryDispose();

                // 网络异常时，自动切换到其它节点
                if (ex is AggregateException ae) ex = ae.InnerException;
                if (NoDelay(ex))
                {
                    if (_servers == null || i >= _servers.Length) throw;
                    _idxServer++;
                }
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

    /// <summary>异步执行命令，经过管道。FullRedis中还会考虑Cluster分流</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="key">命令key，用于选择集群节点</param>
    /// <param name="func">回调函数</param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public virtual async Task<TResult> ExecuteAsync<TResult>(String key, Func<RedisClient, String, Task<TResult>> func, Boolean write = false)
    {
        // 写入或完全管道模式时，才处理管道操作
        if (write || FullPipeline)
        {
            // 管道模式直接执行
            var rds = _client.Value;
            if (rds == null && AutoPipeline > 0) rds = StartPipeline();
            if (rds != null)
            {
                var rs = await func(rds, key).ConfigureAwait(false);

                // 命令数足够，自动提交
                if (AutoPipeline > 0 && rds.PipelineCommands >= AutoPipeline)
                {
                    StopPipeline(true);
                    StartPipeline();
                }

                return rs;
            }
        }

        // 读操作遇到未完成管道队列时，立马执行管道操作
        if (!write) StopPipeline(true);

        // 统计性能
        var sw = Counter?.StartCount();

        var i = 0;
        var delay = 500;
        do
        {
            // 每次重试都需要重新从池里借出连接
            var client = Pool.Get();
            try
            {
                client.Reset();
                return await func(client, key).ConfigureAwait(false);
            }
            catch (RedisException) { throw; }
            catch (Exception ex)
            {
                if (++i >= Retry) throw;

                // 销毁连接
                client.TryDispose();

                // 网络异常时，自动切换到其它节点
                if (ex is AggregateException ae) ex = ae.InnerException;
                if (NoDelay(ex))
                {
                    if (_servers == null || i >= _servers.Length) throw;
                    _idxServer++;
                }
                else
                    Thread.Sleep(delay *= 2);
            }
            finally
            {
                Pool.Return(client);

                Counter?.StopCount(sw);
            }
        } while (true);
    }

    private readonly ThreadLocal<RedisClient?> _client = new();
    /// <summary>开始管道模式</summary>
    public virtual RedisClient StartPipeline()
    {
        var rds = _client.Value;
        if (rds == null)
        {
            rds = Pool.Get();
            rds.Reset();
            rds.StartPipeline();

            _client.Value = rds;
        }

        return rds;
    }

    /// <summary>结束管道模式</summary>
    /// <param name="requireResult">要求结果。默认true</param>
    public virtual Object?[]? StopPipeline(Boolean requireResult = true)
    {
        var rds = _client.Value;
        if (rds == null) return null;
        _client.Value = null;

        // 统计性能
        var sw = Counter?.StartCount();

        // 管道处理不需要重试
        try
        {
            return rds.StopPipeline(requireResult);
        }
        finally
        {
            // 如果不需要结果，则暂停一会，有效清理残留
            if (!requireResult) Thread.Sleep(10);

            rds.Reset();
            Pool.Return(rds);

            Counter?.StopCount(sw);
        }
    }

    /// <summary>提交变更。处理某些残留在管道里的命令</summary>
    /// <returns></returns>
    public override Int32 Commit()
    {
        var rs = StopPipeline(true);
        if (rs == null) return 0;

        return rs.Length;
    }
    #endregion

    #region 子库
    /// <summary>为同一服务器创建不同Db的子级库</summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public virtual Redis CreateSub(Int32 db)
    {
        var rds = (GetType().CreateInstance() as Redis)!;
        rds.Server = Server;
        rds.Db = db;
        rds.UserName = UserName;
        rds.Password = Password;

        rds.Encoder = Encoder;
        rds.JsonHost = JsonHost;
        rds.Timeout = Timeout;
        rds.Retry = Retry;
        rds.ShieldingTime = ShieldingTime;
        rds.FullPipeline = FullPipeline;
        rds.AutoPipeline = AutoPipeline;
        rds.SslProtocol = SslProtocol;
        rds.Certificate = Certificate;
        rds.ThrowOnFailure = ThrowOnFailure;
        rds.MaxMessageSize = MaxMessageSize;

        rds._Info = _Info;
        rds._Version = _Version;

        rds.Tracer = Tracer;
        rds.Log = Log;

        return rds;
    }
    #endregion

    #region 基础操作
    /// <summary>缓存个数</summary>
    public override Int32 Count => Execute(rds => rds.Execute<Int32>("DBSIZE"));

    /// <summary>获取所有键，限制10000项，超额请使用FullRedis.Search</summary>
    public override ICollection<String> Keys
    {
        get
        {
            if (Count > 10000) throw new InvalidOperationException("数量过大时，禁止获取所有键，请使用FullRedis.Search");

            return Execute(rds => rds.Execute<String[]>("KEYS", "*")) ?? [];
        }
    }

    /// <summary>获取信息</summary>
    /// <param name="all">是否获取全部信息，包括Commandstats</param>
    /// <returns></returns>
    public virtual IDictionary<String, String> GetInfo(Boolean all = false)
    {
        var rs = all ?
            Execute(rds => rds.Execute<String>("INFO", "all")) :
            Execute(rds => rds.Execute<String>("INFO"));
        if (rs.IsNullOrEmpty()) return new Dictionary<String, String>();

        //var inf = rs.ToStr();
        return rs.SplitAsDictionary(":", "\r\n");
    }

    /// <summary>单个实体项</summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
    public override Boolean Set<T>(String key, T value, Int32 expire = -1)
    {
        if (expire < 0) expire = Expire;

        var rs = "";
        if (expire <= 0)
            rs = Execute(key, (rds, k) => rds.Execute<String>("SET", k, value), true);
        else
            rs = Execute(key, (rds, k) => rds.Execute<String>("SETEX", k, expire, value), true);

        if (rs == "OK") return true;
        if (rs.IsNullOrEmpty()) return false;

        using var span = Tracer?.NewSpan($"redis:{Name}:ErrorSet", new { key, value });
        if (ThrowOnFailure) throw new XException("Redis.Set({0},{1})失败。{2}", key, value, rs);

        return false;
    }

    /// <summary>获取单体</summary>
    /// <param name="key">键</param>
    [return: MaybeNull]
    public override T Get<T>(String key) => Execute(key, (rds, k) => rds.Execute<T>("GET", k));

    /// <summary>移除缓存项</summary>
    /// <param name="key">键</param>
    public override Int32 Remove(String key)
    {
        if (key.IsNullOrEmpty()) return 0;

        return Execute(key, (rds, k) => rds.Execute<Int32>("DEL", k), true);
    }

    /// <summary>批量移除缓存项</summary>
    /// <param name="keys">键集合</param>
    public override Int32 Remove(params String[] keys)
    {
        if (keys == null || !keys.Any()) return 0;

        return Execute(keys.FirstOrDefault(), (rds, k) => rds.Execute<Int32>("DEL", keys), true);
    }

    /// <summary>清空所有缓存项</summary>
    public override void Clear() => Execute(rds => rds.Execute<String>("FLUSHDB"));

    /// <summary>是否存在</summary>
    /// <param name="key">键</param>
    public override Boolean ContainsKey(String key) => Execute(key, (rds, k) => rds.Execute<Int32>("EXISTS", k) > 0);

    /// <summary>设置缓存项有效期</summary>
    /// <param name="key">键</param>
    /// <param name="expire">过期时间</param>
    public override Boolean SetExpire(String key, TimeSpan expire) => Execute(key, (rds, k) => rds.Execute<String>("EXPIRE", k, (Int32)expire.TotalSeconds) == "1", true);

    /// <summary>获取缓存项有效期</summary>
    /// <param name="key">键</param>
    /// <returns></returns>
    public override TimeSpan GetExpire(String key)
    {
        var sec = Execute(key, (rds, k) => rds.Execute<Int32>("TTL", k));
        return TimeSpan.FromSeconds(sec);
    }
    #endregion

    #region 集合操作
    /// <summary>批量获取缓存项</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public override IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
    {
        var ks = keys as String[] ?? keys.ToArray();
        if (ks.Length == 0) return new Dictionary<String, T>();

        return Execute(ks[0], (rds, k) => rds.GetAll<T>(ks));
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

        Execute(values.FirstOrDefault().Key, (rds, k) => rds.SetAll(values), true);

        // 使用管道批量设置过期时间
        if (expire > 0)
        {
            var ts = TimeSpan.FromSeconds(expire);

            StartPipeline();
            try
            {
                foreach (var item in values)
                {
                    SetExpire(item.Key, ts);
                }
            }
            finally
            {
                StopPipeline(true);
            }
        }
    }

    /// <summary>获取哈希</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns></returns>
    public override IDictionary<String, T> GetDictionary<T>(String key) => throw new NotSupportedException("Redis未支持该功能，需要new FullRedis");

    /// <summary>获取队列</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns></returns>
    public override IProducerConsumer<T> GetQueue<T>(String key) => throw new NotSupportedException("Redis未支持该功能，需要new FullRedis");

    /// <summary>获取栈</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns></returns>
    public override IProducerConsumer<T> GetStack<T>(String key) => throw new NotSupportedException("Redis未支持该功能，需要new FullRedis");

    /// <summary>获取Set</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public override ICollection<T> GetSet<T>(String key) => throw new NotSupportedException("Redis未支持该功能，需要new FullRedis");
    #endregion

    #region 高级操作
    private static Version _v2612 = new("2.6.12");
    /// <summary>添加，已存在时不更新</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
    /// <returns></returns>
    public override Boolean Add<T>(String key, T value, Int32 expire = -1)
    {
        if (expire < 0) expire = Expire;

        // 没有有效期，直接使用SETNX
        if (expire <= 0) return Execute(key, (rds, k) => rds.Execute<Int32>("SETNX", k, value), true) > 0;

        // 带有有效期，需要判断版本是否支持
        var inf = Info;
        //if (inf != null && inf.TryGetValue("redis_version", out var ver) && ver.CompareTo("4.") >= 0 && ver.CompareTo("6.") < 0)
        //{
        //    return Execute(key, rds => rds.Execute<Int32>("SETNX", key, value, expire), true) > 0;
        //}
        if (Version >= _v2612)
        {
            //!!! 重构Redis.Add实现，早期的SETNX支持设置过期时间，后来不支持了，并且连资料都找不到了，改用2.6.12新版 SET key value EX expire NX
            var result = Execute(key, (rds, k) => rds.Execute<String>("SET", k, value, "EX", expire, "NX"), true);
            if (result.IsNullOrEmpty()) return false;
            if (result == "OK") return true;

            using var span = Tracer?.NewSpan($"redis:{Name}:ErrorAdd", new { key, value });
            if (ThrowOnFailure) throw new XException("Redis.Add({0},{1})失败。{2}", key, value, result);

            return false;
        }

        // 旧版本不支持SETNX带过期时间，需要分为前后两条指令
        var rs = Execute(key, (rds, k) => rds.Execute<Int32>("SETNX", k, value), true);
        if (rs > 0) SetExpire(key, TimeSpan.FromSeconds(expire));

        return rs > 0;
    }

    /// <summary>设置新值并获取旧值，原子操作</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <returns></returns>
    [return: MaybeNull]
    public override T Replace<T>(String key, T value) => Execute(key, (rds, k) => rds.Execute<T>("GETSET", k, value), true);

    /// <summary>尝试获取指定键，返回是否包含值。有可能缓存项刚好是默认值，或者只是反序列化失败</summary>
    /// <remarks>
    /// 在 Redis 中，可能有key（此时TryGet返回true），但是因为反序列化失败，从而得不到value。
    /// </remarks>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值。即使有值也不一定能够返回，可能缓存项刚好是默认值，或者只是反序列化失败</param>
    /// <returns>返回是否包含值，即使反序列化失败</returns>
    public override Boolean TryGetValue<T>(String key, [MaybeNullWhen(false)] out T value)
    {
        T? v1 = default;
        var rs1 = Execute(key, (rds, k) =>
        {
            var rs2 = rds.TryExecute("GET", [k], out T? v2);
            v1 = v2;
            return rs2;
        });
        value = v1;

        return rs1;
    }

    ///// <summary>获取 或 添加 缓存数据，在数据不存在时执行委托请求数据</summary>
    ///// <typeparam name="T"></typeparam>
    ///// <param name="key"></param>
    ///// <param name="callback"></param>
    ///// <returns></returns>
    //public override T GetOrAdd<T>(String key, Func<String, T> callback)
    //{
    //    var value = Get<T>(key);
    //    if (!Equals(value, default)) return value;

    //    if (ContainsKey(key)) return value;

    //    value = callback(key);

    //    if (Add(key, value)) return value;

    //    return Get<T>(key);
    //}

    /// <summary>累加，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public override Int64 Increment(String key, Int64 value)
    {
        if (value == 1)
            return Execute(key, (rds, k) => rds.Execute<Int64>("INCR", k), true);
        else
            return Execute(key, (rds, k) => rds.Execute<Int64>("INCRBY", k, value), true);
    }

    /// <summary>累加，原子操作，乘以100后按整数操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public override Double Increment(String key, Double value) => Execute(key, (rds, k) => rds.Execute<Double>("INCRBYFLOAT", k, value), true);

    /// <summary>递减，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public override Int64 Decrement(String key, Int64 value)
    {
        if (value == 1)
            return Execute(key, (rds, k) => rds.Execute<Int64>("DECR", k), true);
        else
            return Execute(key, (rds, k) => rds.Execute<Int64>("DECRBY", k, value.ToString()), true);
    }

    /// <summary>递减，原子操作，乘以100后按整数操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public override Double Decrement(String key, Double value) => Increment(key, -value);
    #endregion

    #region 性能测试
    /// <summary>性能测试</summary>
    /// <remarks>
    /// Redis性能测试[随机]，批大小[100]，逻辑处理器 40 个 2,400MHz Intel(R) Xeon(R) CPU E5-2640 v4 @ 2.40GHz
    /// 测试 100,000 项，  1 线程
    /// 赋值 100,000 项，  1 线程，耗时     418ms 速度   239,234 ops
    /// 读取 100,000 项，  1 线程，耗时     520ms 速度   192,307 ops
    /// 删除 100,000 项，  1 线程，耗时     125ms 速度   800,000 ops
    /// 测试 200,000 项，  2 线程
    /// 赋值 200,000 项，  2 线程，耗时     548ms 速度   364,963 ops
    /// 读取 200,000 项，  2 线程，耗时     549ms 速度   364,298 ops
    /// 删除 200,000 项，  2 线程，耗时     315ms 速度   634,920 ops
    /// 测试 400,000 项，  4 线程
    /// 赋值 400,000 项，  4 线程，耗时     694ms 速度   576,368 ops
    /// 读取 400,000 项，  4 线程，耗时     697ms 速度   573,888 ops
    /// 删除 400,000 项，  4 线程，耗时     438ms 速度   913,242 ops
    /// 测试 800,000 项，  8 线程
    /// 赋值 800,000 项，  8 线程，耗时   1,206ms 速度   663,349 ops
    /// 读取 800,000 项，  8 线程，耗时   1,236ms 速度   647,249 ops
    /// 删除 800,000 项，  8 线程，耗时     791ms 速度 1,011,378 ops
    /// 测试 4,000,000 项， 40 线程
    /// 赋值 4,000,000 项， 40 线程，耗时   4,848ms 速度   825,082 ops
    /// 读取 4,000,000 项， 40 线程，耗时   5,399ms 速度   740,877 ops
    /// 删除 4,000,000 项， 40 线程，耗时   6,281ms 速度   636,841 ops
    /// 测试 4,000,000 项， 64 线程
    /// 赋值 4,000,000 项， 64 线程，耗时   6,806ms 速度   587,716 ops
    /// 读取 4,000,000 项， 64 线程，耗时   5,365ms 速度   745,573 ops
    /// 删除 4,000,000 项， 64 线程，耗时   6,716ms 速度   595,592 ops
    /// </remarks>
    /// <param name="rand">随机读写</param>
    /// <param name="batch">批量操作</param>
    public override Int64 Bench(Boolean rand = true, Int32 batch = 1000)
    {
        XTrace.WriteLine($"目标服务器：{Server}/{Db}");

        //if (AutoPipeline == 0) AutoPipeline = 1000;
        // 顺序操作时，打开自动管道
        if (!rand && batch > 0)
        {
            AutoPipeline = batch;
            FullPipeline = true;
        }

        return base.Bench(rand, batch);
    }

    /// <summary>获取每个线程测试次数</summary>
    /// <param name="rand"></param>
    /// <param name="batch"></param>
    /// <returns></returns>
    protected override Int32 GetTimesPerThread(Boolean rand, Int32 batch)
    {
        var times = base.GetTimesPerThread(rand, batch);

        if (!rand)
        {
            if (batch > 10) times *= 10;
        }
        else
        {
            if (batch > 10) times *= 10;
        }

        return times;
    }

    /// <summary>累加测试</summary>
    /// <param name="keys">键</param>
    /// <param name="times">次数</param>
    /// <param name="threads">线程</param>
    /// <param name="rand">随机读写</param>
    /// <param name="batch">批量操作</param>
    protected override Int64 BenchInc(String[] keys, Int64 times, Int32 threads, Boolean rand, Int32 batch)
    {
        if (rand && batch > 10) times /= 10;
        return base.BenchInc(keys, times, threads, rand, batch);
    }

    /// <summary>删除测试</summary>
    /// <param name="keys"></param>
    /// <param name="times"></param>
    /// <param name="threads"></param>
    /// <param name="rand"></param>
    /// <param name="batch"></param>
    /// <returns></returns>
    protected override Int64 BenchRemove(String[] keys, Int64 times, Int32 threads, Boolean rand, Int32 batch)
    {
        if (rand && batch > 10) times *= 10;
        return base.BenchRemove(keys, times, threads, rand, batch);
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>客户端命令日志</summary>
    public ILog ClientLog { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}