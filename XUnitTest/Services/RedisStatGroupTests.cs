using System;
using NewLife.Caching;
using NewLife.Caching.Services;
using Xunit;

namespace XUnitTest.Services;

public class RedisStatGroupTests
{
    [Fact(DisplayName = "默认值测试")]
    public void DefaultValues()
    {
        var group = new RedisStatGroup();
        Assert.Null(group.Redis);
        Assert.NotNull(group.Stats);
        Assert.Empty(group.Stats);
    }

    [Fact(DisplayName = "设置Redis属性")]
    public void SetRedis()
    {
        var group = new RedisStatGroup();
        var redis = new FullRedis();
        group.Redis = redis;
        Assert.Same(redis, group.Redis);
    }
}
