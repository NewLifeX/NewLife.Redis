using NewLife.Configuration;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Caching.Services;

/// <summary>Redis缓存服务。由Redis提供标准缓存和队列服务，锁定配置名RedisCache，可以在配置中心统一设置</summary>
/// <remarks>
/// 根据实际开发经验，即使在分布式系统中，也有大量的数据是不需要跨进程共享的，因此本接口提供了两级缓存。
/// 借助该缓存架构，可以实现各功能模块跨进程共享数据，分布式部署时可用Redis，需要考虑序列化成本。
/// 
/// 使用队列时，可根据是否设置消费组来决定使用简单队列还是完整队列。
/// 简单队列（如RedisQueue）可用作命令队列，Topic很多，但几乎没有消息。
/// 完整队列（如RedisStream）可用作消息队列，Topic很少，但消息很多，并且支持多消费组。
/// </remarks>
public class RedisCacheProvider : CacheProvider
{
    #region 属性
    private FullRedis? _redis;
    private FullRedis? _redisQueue;

    /// <summary>队列</summary>
    public FullRedis? RedisQueue { get => _redisQueue; set => _redisQueue = value; }
    #endregion

    #region 构造
    /// <summary>实例化Redis缓存服务</summary>
    public RedisCacheProvider() { }

    /// <summary>实例化Redis缓存服务，自动创建FullRedis对象</summary>
    /// <param name="serviceProvider"></param>
    public RedisCacheProvider(IServiceProvider serviceProvider)
    {
        var config = serviceProvider?.GetService<IConfigProvider>();
        config ??= JsonConfigProvider.LoadAppSettings();
        if (config != null) Init(config, serviceProvider);
    }
    #endregion

    #region 方法
    /// <summary>初始化</summary>
    /// <param name="config"></param>
    /// <param name="serviceProvider"></param>
    public void Init(IConfigProvider config, IServiceProvider? serviceProvider = null)
    {
        var cacheConn = config["RedisCache"];
        var queueConn = config["RedisQueue"];

        // 实例化全局缓存和队列，如果未设置队列，则使用缓存对象
        FullRedis? redis = null;
        if (!cacheConn.IsNullOrEmpty())
        {
            if (serviceProvider != null)
            {
                redis = serviceProvider.GetService<FullRedis>();
                if (redis != null && redis.Name != "RedisCache") redis = null;

                redis ??= new FullRedis(serviceProvider, "RedisCache")
                {
                    Log = serviceProvider.GetRequiredService<ILog>(),
                    Tracer = serviceProvider.GetRequiredService<ITracer>(),
                };
            }
            else
            {
                redis = new FullRedis { Name = "RedisCache", Log = XTrace.Log };
                redis.Init(cacheConn);
            }

            _redis = redis;
            _redisQueue = redis;
            Cache = redis;
        }
        if (!queueConn.IsNullOrEmpty())
        {
            if (serviceProvider != null)
            {
                redis = serviceProvider.GetService<FullRedis>();
                if (redis != null && redis.Name != "RedisQueue") redis = null;

                redis ??= new FullRedis(serviceProvider, "RedisQueue")
                {
                    Log = serviceProvider.GetRequiredService<ILog>(),
                    Tracer = serviceProvider.GetRequiredService<ITracer>(),
                };
            }
            else
            {
                redis = new FullRedis { Name = "RedisQueue", Log = XTrace.Log };
                redis.Init(queueConn);
            }

            _redisQueue = redis;
        }
    }

    /// <summary>获取队列。各功能模块跨进程共用的队列，默认使用LIST，带消费组时使用STREAM</summary>
    /// <remarks>
    /// 使用队列时，可根据是否设置消费组来决定使用简单队列还是完整队列。
    /// 简单队列（如RedisQueue）可用作命令队列，Topic很多，但几乎没有消息。
    /// 完整队列（如RedisStream）可用作消息队列，Topic很少，但消息很多，并且支持多消费组。
    /// </remarks>
    /// <typeparam name="T">消息类型。用于消息生产者时，可指定为Object</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="group">消费组。未指定消费组时使用简单队列（如RedisQueue），指定消费组时使用完整队列（如RedisStream）</param>
    /// <returns></returns>
    public override IProducerConsumer<T> GetQueue<T>(String topic, String? group = null)
    {
        if (_redisQueue != null)
        {
            IProducerConsumer<T>? queue = null;
            if (group.IsNullOrEmpty())
            {
                queue = _redisQueue.GetQueue<T>(topic);

                XTrace.WriteLine("[{0}]队列消息数：{1}", topic, queue.Count);
            }
            else
            {
                var rs = _redisQueue.GetStream<T>(topic);
                rs.Group = group;
                queue = rs;

                XTrace.WriteLine("[{0}/{2}]队列消息数：{1}", topic, queue.Count, group);
            }

            return queue;
        }

        return base.GetQueue<T>(topic, group);
    }
    #endregion
}
