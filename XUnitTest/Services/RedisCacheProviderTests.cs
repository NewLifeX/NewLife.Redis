using System;
using NewLife.Caching.Services;
using NewLife.Configuration;
using NewLife.Model;
using Xunit;

namespace XUnitTest.Services;

public class RedisCacheProviderTests
{
    [Fact]
    public void Ctor()
    {
        var services = ObjectContainer.Current;
        services.AddSingleton<IConfigProvider>(JsonConfigProvider.LoadAppSettings());

        var sp = services.BuildServiceProvider();

        var provider = new RedisCacheProvider(sp);
        Assert.NotNull(provider.Cache);
        Assert.NotNull(provider.InnerCache);
        Assert.Null(provider.RedisQueue);

        var q = provider.GetQueue<Object>("test");
        Assert.NotNull(q);
    }
}
