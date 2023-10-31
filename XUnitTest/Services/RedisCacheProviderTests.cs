using System;
using NewLife.Caching.Services;
using NewLife.Model;
using Xunit;

namespace XUnitTest.Services;

public class RedisCacheProviderTests
{
    [Fact]
    public void Ctor()
    {
        var sp = ObjectContainer.Provider;

        var provider = new RedisCacheProvider(sp);
        Assert.NotNull(provider.Cache);
        Assert.NotNull(provider.InnerCache);
        Assert.Null(provider.RedisQueue);

        var q = provider.GetQueue<Object>("test");
        Assert.NotNull(q);
    }
}
