using System;
using System.Collections.Generic;
using System.Reflection;
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
        Assert.Equal(13, (Int32)ServerType.Valkey);
    }

    [Fact(DisplayName = "枚举名称测试")]
    public void EnumNames()
    {
        Assert.Equal("Unknown", ServerType.Unknown.ToString());
        Assert.Equal("Redis", ServerType.Redis.ToString());
        Assert.Equal("Garnet", ServerType.Garnet.ToString());
        Assert.Equal("Valkey", ServerType.Valkey.ToString());
    }

    [Fact(DisplayName = "枚举解析测试")]
    public void EnumParse()
    {
        Assert.Equal(ServerType.Redis, Enum.Parse<ServerType>("Redis"));
        Assert.Equal(ServerType.Garnet, Enum.Parse<ServerType>("Garnet"));
        Assert.Equal(ServerType.Unknown, Enum.Parse<ServerType>("Unknown"));
        Assert.Equal(ServerType.Valkey, Enum.Parse<ServerType>("Valkey"));
    }

    [Fact(DisplayName = "从INFO识别Valkey服务器")]
    public void DetectValkey()
    {
        Assert.Equal(ServerType.Valkey, Detect(new Dictionary<String, String> { ["server"] = "valkey" }));
        Assert.Equal(ServerType.Valkey, Detect(new Dictionary<String, String> { ["server"] = "Valkey 8.0.1" }));
        Assert.Equal(ServerType.Valkey, Detect(new Dictionary<String, String> { ["valkey_version"] = "8.0.1" }));
        Assert.Equal(ServerType.Redis, Detect(new Dictionary<String, String> { ["server"] = "redis" }));
    }

    [Fact(DisplayName = "从INFO识别其他兼容服务器")]
    public void DetectCompatServers()
    {
        Assert.Equal(ServerType.Garnet, Detect(new Dictionary<String, String> { ["server"] = "Garnet" }));
        Assert.Equal(ServerType.Pika, Detect(new Dictionary<String, String> { ["server"] = "Pika" }));
        Assert.Equal(ServerType.DragonflyDB, Detect(new Dictionary<String, String> { ["server"] = "dragonfly" }));
        Assert.Equal(ServerType.HuaweiCloud, Detect(new Dictionary<String, String> { ["os"] = "Huawei Cloud EulerOS" }));
        Assert.Equal(ServerType.AlibabaCloud, Detect(new Dictionary<String, String> { ["server"] = "Tair" }));
        Assert.Equal(ServerType.TencentCloud, Detect(new Dictionary<String, String> { ["server"] = "Tencent" }));
    }

    private static ServerType Detect(IDictionary<String, String> info)
    {
        var method = typeof(Redis).GetMethod("DetectServerType", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (ServerType)method!.Invoke(null, [info])!;
    }
}
