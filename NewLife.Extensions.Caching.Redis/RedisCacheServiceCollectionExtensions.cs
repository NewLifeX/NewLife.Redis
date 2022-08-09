using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewLife.Caching;

namespace NewLife.Extensions.Caching.Redis;

/// <summary>
/// Redis分布式缓存扩展
/// </summary>
public static class RedisCacheServiceCollectionExtensions
{
    /// <summary>
    /// 添加Redis分布式缓存
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, Action<IOptions<RedisOptions>> setupAction)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (setupAction == null)
            throw new ArgumentNullException(nameof(setupAction));

        services.AddOptions();
        services.Configure(setupAction);
        services.Add(ServiceDescriptor.Singleton<IDistributedCache, RedisCache>());

        return services;
    }
}