using System;
using NewLife.Caching;
using Xunit;

namespace XUnitTest;

public class ServerTypeTests
{
    [Fact(DisplayName = "枚举值测试")]
    public void EnumValues()
    {
        Assert.Equal(0, (Int32)ServerType.Unknown);
        Assert.Equal(1, (Int32)ServerType.Redis);
        Assert.Equal(2, (Int32)ServerType.Garnet);
    }

    [Fact(DisplayName = "枚举名称测试")]
    public void EnumNames()
    {
        Assert.Equal("Unknown", ServerType.Unknown.ToString());
        Assert.Equal("Redis", ServerType.Redis.ToString());
        Assert.Equal("Garnet", ServerType.Garnet.ToString());
    }

    [Fact(DisplayName = "枚举解析测试")]
    public void EnumParse()
    {
        Assert.Equal(ServerType.Redis, Enum.Parse<ServerType>("Redis"));
        Assert.Equal(ServerType.Garnet, Enum.Parse<ServerType>("Garnet"));
        Assert.Equal(ServerType.Unknown, Enum.Parse<ServerType>("Unknown"));
    }
}
