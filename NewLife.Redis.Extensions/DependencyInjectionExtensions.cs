using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NewLife;
using NewLife.Caching;
using NewLife.Caching.Services;
using NewLife.Configuration;
using NewLife.Log;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DependencyInjectionExtensions
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>注入FullRedis，应用内可使用FullRedis/Redis/ICache/ICacheProvider</summary>
    /// <param name="services"></param>
    /// <param name="redis"></param>
    /// <returns></returns>
    public static IServiceCollection AddRedis(this IServiceCollection services, FullRedis? redis = null)
    {
        //if (redis == null) throw new ArgumentNullException(nameof(redis));

        if (redis == null) return services.AddRedisCacheProvider();

        services.AddBasic();
        services.TryAddSingleton<ICache>(redis);
        services.AddSingleton<Redis>(redis);
        services.AddSingleton(redis);

        // 注册Redis缓存服务
        services.TryAddSingleton(p =>
        {
            var provider = new RedisCacheProvider(p);
            if (provider.Cache is not Redis) provider.Cache = redis;
            provider.RedisQueue ??= redis;

            return provider;
        });

        return services;
    }

    /// <summary>
    /// Adds services for FullRedis to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="tracer"></param>
    /// <returns></returns>
    public static FullRedis AddRedis(this IServiceCollection services, String config, ITracer tracer = null!)
    {
        if (String.IsNullOrEmpty(config)) throw new ArgumentNullException(nameof(config));

        var redis = new FullRedis();
        redis.Init(config);
        redis.Tracer = tracer;

        services.AddRedis(redis);

        return redis;
    }

    /// <summary>
    /// Adds services for FullRedis to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="name"></param>
    /// <param name="config"></param>
    /// <param name="timeout"></param>
    /// <param name="tracer"></param>
    /// <returns></returns>
    public static FullRedis AddRedis(this IServiceCollection services, String name, String config, Int32 timeout = 0, ITracer tracer = null!)
    {
        if (String.IsNullOrEmpty(config)) throw new ArgumentNullException(nameof(config));

        var redis = new FullRedis();
        if (!name.IsNullOrEmpty()) redis.Name = name;
        redis.Init(config);
        if (timeout > 0) redis.Timeout = timeout;
        redis.Tracer = tracer;

        services.AddRedis(redis);

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
    /// <param name="tracer"></param>
    /// <returns></returns>
    public static FullRedis AddRedis(this IServiceCollection services, String server, String psssword, Int32 db, Int32 timeout = 0, ITracer tracer = null!)
    {
        if (String.IsNullOrEmpty(server)) throw new ArgumentNullException(nameof(server));

        var redis = new FullRedis(server, psssword, db);
        if (timeout > 0) redis.Timeout = timeout;
        redis.Tracer = tracer;

        services.AddRedis(redis);

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

        services.AddBasic();
        services.AddOptions();
        services.Configure(setupAction);
        //services.Add(ServiceDescriptor.Singleton<ICache, FullRedis>());
        services.AddSingleton(sp => new FullRedis(sp, sp.GetRequiredService<IOptions<RedisOptions>>().Value));
        services.TryAddSingleton<ICache>(p => p.GetRequiredService<FullRedis>());
        services.TryAddSingleton<Redis>(p => p.GetRequiredService<FullRedis>());

        // 注册Redis缓存服务
        services.TryAddSingleton(p =>
        {
            var redis = p.GetRequiredService<FullRedis>();
            var provider = new RedisCacheProvider(p);
            if (provider.Cache is not Redis) provider.Cache = redis;
            provider.RedisQueue ??= redis;

            return provider;
        });

        return services;
    }

    /// <summary>
    /// 添加键前缀的PrefixedRedis缓存
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [Obsolete("=>AddRedis")]
    public static IServiceCollection AddPrefixedRedis(this IServiceCollection services, Action<RedisOptions> setupAction)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (setupAction == null)
            throw new ArgumentNullException(nameof(setupAction));

        services.AddBasic();
        services.AddOptions();
        services.Configure(setupAction);
        services.AddSingleton(sp => new FullRedis(sp, sp.GetRequiredService<IOptions<RedisOptions>>().Value));

        return services;
    }

    /// <summary>添加Redis缓存提供者ICacheProvider。从配置读取RedisCache和RedisQueue</summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddRedisCacheProvider(this IServiceCollection services)
    {
        services.AddBasic();
        services.AddSingleton<ICacheProvider, RedisCacheProvider>();
        services.TryAddSingleton<ICache>(p => p.GetRequiredService<ICacheProvider>().Cache);
        services.TryAddSingleton<Redis>(p =>
        {
            var redis = p.GetRequiredService<ICacheProvider>().Cache as Redis;
            if (redis == null) throw new InvalidOperationException("未配置Redis，可在配置文件或配置中心指定名为RedisCache的连接字符串");

            return redis;
        });
        services.TryAddSingleton<FullRedis>(p =>
        {
            var redis = p.GetRequiredService<ICacheProvider>().Cache as FullRedis;
            if (redis == null) throw new InvalidOperationException("未配置Redis，可在配置文件或配置中心指定名为RedisCache的连接字符串");

            return redis;
        });

        return services;
    }

    static void AddBasic(this IServiceCollection services)
    {
        // 注册依赖项
        services.TryAddSingleton<ILog>(XTrace.Log);
        services.TryAddSingleton<ITracer>(DefaultTracer.Instance ??= new DefaultTracer());

        if (!services.Any(e => e.ServiceType == typeof(IConfigProvider)))
            services.TryAddSingleton<IConfigProvider>(JsonConfigProvider.LoadAppSettings());
    }
}