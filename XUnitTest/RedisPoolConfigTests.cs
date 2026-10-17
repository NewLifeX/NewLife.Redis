using System;
using System.Collections.Generic;
using NewLife.Caching;
using Xunit;

namespace XUnitTest;

public class RedisPoolConfigTests
{
    [Fact(DisplayName = "连接池配置默认值测试")]
    public void Defaults()
    {
        var config = new RedisPoolConfig();

        Assert.Equal(10, config.Min);
        Assert.Equal(100000, config.Max);
        Assert.Equal(30, config.IdleTime);
        Assert.Equal(300, config.MaxLifetime);
        Assert.Equal(15, config.WaitTimeout);
    }

    [Fact(DisplayName = "从配置字典加载连接池参数")]
    public void Load()
    {
        var config = new RedisPoolConfig();

        config.Load(new Dictionary<String, String>
        {
            ["PoolMin"] = "2",
            ["PoolMax"] = "1000",
            ["PoolIdleTime"] = "60",
            ["MaxLifetime"] = "600",
            ["WaitTimeout"] = "0",
        });

        Assert.Equal(2, config.Min);
        Assert.Equal(1000, config.Max);
        Assert.Equal(60, config.IdleTime);
        Assert.Equal(600, config.MaxLifetime);
        Assert.Equal(0, config.WaitTimeout);
    }

    [Fact(DisplayName = "非法配置值不覆盖默认值")]
    public void LoadInvalid()
    {
        var config = new RedisPoolConfig();
        var dic = new Dictionary<String, String>
        {
            ["PoolMin"] = "-1",
            ["PoolMax"] = "abc",
            ["PoolIdleTime"] = "",
            ["Unknown"] = "123",
        };

        config.Load(dic);

        Assert.Equal(10, config.Min);
        Assert.Equal(100000, config.Max);
        Assert.Equal(30, config.IdleTime);
        Assert.Equal(300, config.MaxLifetime);
        Assert.Equal(15, config.WaitTimeout);
    }
}
