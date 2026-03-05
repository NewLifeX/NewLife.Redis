using System;
using System.Reflection;
using NewLife.Caching;
using Xunit;

namespace XUnitTest;

public class RedisEncoderTests
{
    private static readonly MethodInfo _onDecode = typeof(RedisJsonEncoder)
        .GetMethod("OnDecode", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly RedisJsonEncoder _encoder = new();

    private Object? InvokeOnDecode(String value, Type type) => _onDecode.Invoke(_encoder, [value, type]);

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
        var result = InvokeOnDecode("OK", typeof(Boolean));
        Assert.Equal(true, result);
    }

    [Fact(DisplayName = "解码普通字符串")]
    public void DecodeString()
    {
        var result = InvokeOnDecode("hello", typeof(String));
        Assert.Equal("hello", result);
    }

    [Fact(DisplayName = "解码数字字符串")]
    public void DecodeInt()
    {
        var result = InvokeOnDecode("42", typeof(Int32));
        Assert.Equal(42, result);
    }
}
