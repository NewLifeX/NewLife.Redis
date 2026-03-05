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

    [Fact(DisplayName = "Stats列表初始化")]
    public void StatsInitialization()
    {
        var group = new RedisStatGroup();
        Assert.NotNull(group.Stats);
        Assert.Empty(group.Stats);
    }
}
