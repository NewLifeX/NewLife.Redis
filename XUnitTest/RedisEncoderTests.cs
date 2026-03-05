using System;
using System.Reflection;
using NewLife.Caching;
using Xunit;

namespace XUnitTest;

public class RedisEncoderTests
{
    [Fact(DisplayName = "构造函数测试")]
    public void Ctor()
    {
        var encoder = new RedisJsonEncoder();
        Assert.NotNull(encoder);
        Assert.NotNull(encoder.JsonHost);
    }

    [Fact(DisplayName = "解码OK为Boolean")]
    public void DecodeOK()
    {
        var encoder = new RedisJsonEncoder();
        // 使用反射调用受保护的 OnDecode 方法
        var method = typeof(RedisJsonEncoder).GetMethod("OnDecode", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(encoder, ["OK", typeof(Boolean)]);
        Assert.Equal(true, result);
    }

    [Fact(DisplayName = "解码普通字符串")]
    public void DecodeString()
    {
        var encoder = new RedisJsonEncoder();
        var method = typeof(RedisJsonEncoder).GetMethod("OnDecode", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(encoder, ["hello", typeof(String)]);
        Assert.Equal("hello", result);
    }

    [Fact(DisplayName = "解码数字字符串")]
    public void DecodeInt()
    {
        var encoder = new RedisJsonEncoder();
        var method = typeof(RedisJsonEncoder).GetMethod("OnDecode", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(encoder, ["42", typeof(Int32)]);
        Assert.Equal(42, result);
    }
}
