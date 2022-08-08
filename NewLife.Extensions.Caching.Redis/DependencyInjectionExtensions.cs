using Microsoft.Extensions.DependencyInjection.Extensions;
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
}