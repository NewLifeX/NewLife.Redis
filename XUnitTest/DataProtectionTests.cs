using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewLife.Caching;
using NewLife.Redis.Extensions;
using Xunit;

namespace XUnitTest;

public class DataProtectionTests
{
    [Fact(DisplayName = "数据保护使用Redis实例持久化密钥")]
    public void PersistKeysWithInstance()
    {
        var services = new ServiceCollection();
        var redis = new FullRedis();
        redis.Init(BasicTest.GetConfig());

        var builder = services.AddDataProtection();
        builder.PersistKeysToRedis(redis, "unit:dp:keys");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>>().Value;

        var repository = Assert.IsType<RedisXmlRepository>(options.XmlRepository);
        Assert.NotNull(repository);
    }

    [Fact(DisplayName = "数据保护使用Redis工厂持久化密钥")]
    public void PersistKeysWithFactory()
    {
        var services = new ServiceCollection();
        var builder = services.AddDataProtection();
        builder.PersistKeysToRedis(() => new FullRedis(), "unit:dp:keys");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>>().Value;

        Assert.IsType<RedisXmlRepository>(options.XmlRepository);
    }

    [Fact(DisplayName = "数据保护扩展参数校验")]
    public void PersistKeysArgumentNull()
    {
        var services = new ServiceCollection();
        var builder = services.AddDataProtection();

        Assert.Throws<ArgumentNullException>(() => RedisDataProtectionBuilderExtensions.PersistKeysToRedis(null!, new FullRedis(), "key"));
        Assert.Throws<ArgumentNullException>(() => builder.PersistKeysToRedis((NewLife.Caching.Redis)null!, "key"));
        Assert.Throws<ArgumentNullException>(() => builder.PersistKeysToRedis((Func<NewLife.Caching.Redis>)null!, "key"));
    }

    [RedisFact(DisplayName = "RedisXmlRepository读写元素")]
    public void XmlRepositoryStoreAndGet()
    {
        var redis = new FullRedis();
        redis.Init(BasicTest.GetConfig());
        var key = "unit:dp:keys:" + Guid.NewGuid().ToString("N");
        redis.Remove(key);

        try
        {
            var repository = new RedisXmlRepository(redis, key);
            var element = XElement.Parse("<key id=\"1\"><value>abc</value></key>");

            repository.StoreElement(element, "unit");

            var list = repository.GetAllElements();
            Assert.Single(list);
            Assert.Equal("1", list.First().Attribute("id")!.Value);
            Assert.Equal("abc", list.First().Element("value")!.Value);
        }
        finally
        {
            redis.Remove(key);
            redis.Dispose();
        }
    }
}
