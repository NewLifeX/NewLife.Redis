using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NewLife;
using NewLife.Caching;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DependencyInjectionExtensions
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds services for FullRedis to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static FullRedis AddRedis(this IServiceCollection services, String config)
    {
        if (String.IsNullOrEmpty(config)) throw new ArgumentNullException(nameof(config));

        var redis = new FullRedis();
        redis.Init(config);

        services.TryAddSingleton<ICache>(redis);
        services.AddSingleton<Redis>(redis);
        services.AddSingleton(redis);

        return redis;
    }

    /// <summary>
    /// Adds services for FullRedis to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="name"></param>
    /// <param name="config"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public static FullRedis AddRedis(this IServiceCollection services, String name, String config, Int32 timeout = 0)
    {
        if (String.IsNullOrEmpty(config)) throw new ArgumentNullException(nameof(config));

        var redis = new FullRedis();
        if (!name.IsNullOrEmpty()) redis.Name = name;
        redis.Init(config);
        if (timeout > 0) redis.Timeout = timeout;

        services.TryAddSingleton<ICache>(redis);
        services.AddSingleton<Redis>(redis);
        services.AddSingleton(redis);

        return redis;
    }

    /// <summary>
    /// Adds services for FullRedis to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="server"></param>
    /// <param name="psssword"></param>
    /// <param name="db"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public static FullRedis AddRedis(this IServiceCollection services, String server, String psssword, Int32 db, Int32 timeout = 0)
    {
        if (String.IsNullOrEmpty(server)) throw new ArgumentNullException(nameof(server));

        var redis = new FullRedis(server, psssword, db);
        if (timeout > 0) redis.Timeout = timeout;

        services.TryAddSingleton<ICache>(redis);
        services.AddSingleton<Redis>(redis);
        services.AddSingleton(redis);

        return redis;
    }

    /// <summary>添加Redis缓存</summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddRedis(this IServiceCollection services, Action<RedisOptions> setupAction)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (setupAction == null)
            throw new ArgumentNullException(nameof(setupAction));

        services.AddOptions();
        services.Configure(setupAction);
        //services.Add(ServiceDescriptor.Singleton<ICache, FullRedis>());
        services.AddSingleton(sp => new FullRedis(sp.GetRequiredService<IOptions<RedisOptions>>().Value));

        return services;
    }
}