using System;
using NewLife.Caching;
using Xunit;

namespace XUnitTest;

public class RedisOptionsTests
{
    [Fact(DisplayName = "默认值测试")]
    public void DefaultValues()
    {
        var options = new RedisOptions();

        Assert.Null(options.InstanceName);
        Assert.Null(options.Configuration);
        Assert.Null(options.Server);
        Assert.Equal(0, options.Db);
        Assert.Null(options.UserName);
        Assert.Null(options.Password);
        Assert.Equal(3000, options.Timeout);
        Assert.Null(options.Prefix);
        Assert.Equal(0, options.ProtocolVersion);
    }

    [Fact(DisplayName = "属性设置测试")]
    public void SetProperties()
    {
        var options = new RedisOptions
        {
            InstanceName = "TestInstance",
            Configuration = "server=127.0.0.1:6379;password=123456;db=3",
            Server = "127.0.0.1:6379",
            Db = 3,
            UserName = "admin",
            Password = "123456",
            Timeout = 5000,
            Prefix = "Test:",
            ProtocolVersion = 3,
        };

        Assert.Equal("TestInstance", options.InstanceName);
        Assert.Equal("server=127.0.0.1:6379;password=123456;db=3", options.Configuration);
        Assert.Equal("127.0.0.1:6379", options.Server);
        Assert.Equal(3, options.Db);
        Assert.Equal("admin", options.UserName);
        Assert.Equal("123456", options.Password);
        Assert.Equal(5000, options.Timeout);
        Assert.Equal("Test:", options.Prefix);
        Assert.Equal(3, options.ProtocolVersion);
    }
}
