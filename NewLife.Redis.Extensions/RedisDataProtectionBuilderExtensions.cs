using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;

namespace NewLife.Redis.Extensions;

/// <summary>Redis数据保护扩展</summary>
public static class RedisDataProtectionBuilderExtensions
{
    private const String DataProtectionKeysName = "DataProtection-Keys";

    ///// <summary>存储数据保护Key到Redis，自动识别已注入到容器的FullRedis或Redis单例</summary>
    ///// <param name="builder"></param>
    ///// <returns></returns>
    ///// <exception cref="ArgumentNullException"></exception>
    //public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder)
    //{
    //    if (builder == null) throw new ArgumentNullException(nameof(builder));

    //    var redis = builder.Services.LastOrDefault(e => e.ServiceType == typeof(FullRedis))?.ImplementationInstance as NewLife.Caching.Redis;
    //    redis ??= builder.Services.LastOrDefault(e => e.ServiceType == typeof(NewLife.Caching.Redis))?.ImplementationInstance as NewLife.Caching.Redis;
    //    if (redis == null) throw new ArgumentNullException(nameof(redis));

    //    return PersistKeysToRedisInternal(builder, redis, DataProtectionKeysName);
    //}

    /// <summary>存储数据保护Key到Redis</summary>
    /// <param name="builder"></param>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder, NewLife.Caching.Redis redis, String key = DataProtectionKeysName)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (redis == null) throw new ArgumentNullException(nameof(redis));

        return PersistKeysToRedisInternal(builder, redis, key);
    }

    private static IDataProtectionBuilder PersistKeysToRedisInternal(IDataProtectionBuilder builder, NewLife.Caching.Redis redis, String key)
    {
        builder.Services.Configure(delegate (KeyManagementOptions options)
        {
            options.XmlRepository = new RedisXmlRepository(redis, key);
        });
        return builder;
    }

    /// <summary>存储数据保护Key到Redis</summary>
    /// <param name="builder"></param>
    /// <param name="redisFactory"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder, Func<NewLife.Caching.Redis> redisFactory, String key = DataProtectionKeysName)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (redisFactory == null) throw new ArgumentNullException(nameof(redisFactory));

        builder.Services.Configure(delegate (KeyManagementOptions options)
        {
            options.XmlRepository = new RedisXmlRepository(redisFactory, key);
        });

        return builder;
    }
}
