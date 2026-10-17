using System;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using Xunit;

namespace XUnitTest;

public class DependencyInjectionExtensionsTests
{
    private const String Config = "server=127.0.0.1:6379;db=3";

    [Fact(DisplayName = "按连接字符串注册Redis")]
    public void AddRedisConfig()
    {
        var services = new ServiceCollection();

        var redis = services.AddRedis(Config);

        Assert.NotNull(redis);
        Assert.Equal("127.0.0.1:6379", redis.Server);
        Assert.Equal(3, redis.Db);

        using var provider = services.BuildServiceProvider();
        Assert.Same(redis, provider.GetRequiredService<FullRedis>());
        Assert.Same(redis, provider.GetRequiredService<Redis>());
        Assert.Same(redis, provider.GetRequiredService<ICache>());
    }

    [Fact(DisplayName = "按名称与超时注册Redis")]
    public void AddRedisNameAndTimeout()
    {
        var services = new ServiceCollection();

        var redis = services.AddRedis(name: "OrderCache", config: Config, timeout: 8_000);

        Assert.Equal("OrderCache", redis.Name);
        Assert.Equal(8_000, redis.Timeout);
    }

    [Fact(DisplayName = "按服务器地址注册Redis")]
    public void AddRedisServer()
    {
        var services = new ServiceCollection();

        var redis = services.AddRedis("127.0.0.1:6379", "123456", 5, 6_000);

        Assert.Equal("127.0.0.1:6379", redis.Server);
        Assert.Equal("123456", redis.Password);
        Assert.Equal(5, redis.Db);
        Assert.Equal(6_000, redis.Timeout);
    }

    [Fact(DisplayName = "按选项注册Redis")]
    public void AddRedisOptions()
    {
        var services = new ServiceCollection();

        services.AddRedis(options =>
        {
            options.Server = "127.0.0.1:6379";
            options.Db = 4;
            options.Prefix = "unit:";
            options.Timeout = 7_000;
        });

        using var provider = services.BuildServiceProvider();
        var redis = provider.GetRequiredService<FullRedis>();

        Assert.Equal("127.0.0.1:6379", redis.Server);
        Assert.Equal(4, redis.Db);
        Assert.Equal("unit:", redis.Prefix);
        Assert.Equal(7_000, redis.Timeout);
    }

    [Fact(DisplayName = "空连接字符串抛出异常")]
    public void AddRedisEmptyConfig()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddRedis(""));
        Assert.Throws<ArgumentNullException>(() => services.AddRedis(server: "", psssword: "pwd", db: 0));
        Assert.Throws<ArgumentNullException>(() => services.AddRedis((Action<RedisOptions>)null!));
    }
}
