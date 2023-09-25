using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;

namespace NewLife.Redis.Extensions;

/// <summary>Redis数据保护扩展</summary>
public static class RedisDataProtectionBuilderExtensions
{
    private const String DataProtectionKeysName = "DataProtection-Keys";

    /// <summary>存储数据保护Key到Redis</summary>
    /// <param name="builder"></param>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder, FullRedis redis, String key = DataProtectionKeysName)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (redis == null) throw new ArgumentNullException(nameof(redis));

        return PersistKeysToRedisInternal(builder, redis, key);
    }

    private static IDataProtectionBuilder PersistKeysToRedisInternal(IDataProtectionBuilder builder, FullRedis redis, String key)
    {
        builder.Services.Configure(delegate (KeyManagementOptions options)
        {
            options.XmlRepository = new RedisXmlRepository(redis, key);
        });
        return builder;
    }
}
