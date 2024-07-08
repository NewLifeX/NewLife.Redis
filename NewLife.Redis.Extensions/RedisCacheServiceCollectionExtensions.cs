using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NewLife.Caching;
using NewLife.Caching.Services;

namespace NewLife.Redis.Extensions;

/// <summary>
/// Redis分布式缓存扩展
/// </summary>
public static class RedisCacheServiceCollectionExtensions
{
    /// <summary>
    /// 添加Redis分布式缓存，应用内可使用RedisCache/FullRedis/Redis/IDistributedCache/ICache/ICacheProvider
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, Action<RedisOptions> setupAction)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (setupAction == null)
            throw new ArgumentNullException(nameof(setupAction));


        services.AddOptions();
        services.Configure(setupAction);
        services.AddSingleton(sp => new RedisCache(sp, sp.GetRequiredService<IOptions<RedisOptions>>()));
        services.AddSingleton<IDistributedCache>(sp => sp.GetRequiredService<RedisCache>());

        services.TryAddSingleton<FullRedis>(sp => sp.GetRequiredService<RedisCache>());
        services.TryAddSingleton<ICache>(p => p.GetRequiredService<RedisCache>());
        services.TryAddSingleton<Caching.Redis>(p => p.GetRequiredService<RedisCache>());

        // 注册Redis缓存服务
        services.TryAddSingleton(p =>
        {
            var redis = p.GetRequiredService<RedisCache>();
            var provider = new RedisCacheProvider(p);
            if (provider.Cache is not Caching.Redis) provider.Cache = redis;
            provider.RedisQueue ??= redis;

            return provider;
        });

        return services;
    }
}