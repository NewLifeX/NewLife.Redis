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

    [Fact(DisplayName = "解码Boolean字符串")]
    public void DecodeBoolean()
    {
        Assert.Equal(true, InvokeOnDecode("true", typeof(Boolean)));
        Assert.Equal(false, InvokeOnDecode("false", typeof(Boolean)));
        Assert.Equal(true, InvokeOnDecode("OK", typeof(Boolean)));
    }

    [Fact(DisplayName = "解码浮点数")]
    public void DecodeDouble()
    {
        var result = InvokeOnDecode("12.34", typeof(Double));
        Assert.Equal(12.34, result);
    }

    [Fact(DisplayName = "解码复杂对象")]
    public void DecodeObject()
    {
        var result = InvokeOnDecode("""{"Id":123,"Name":"新生命"}""", typeof(EncoderModel));

        var model = Assert.IsType<EncoderModel>(result);
        Assert.Equal(123, model.Id);
        Assert.Equal("新生命", model.Name);
    }

    [Fact(DisplayName = "OK字符串按普通字符串解码")]
    public void DecodeOkAsString()
    {
        var result = InvokeOnDecode("OK", typeof(String));
        Assert.Equal("OK", result);
    }

    private class EncoderModel
    {
        public Int32 Id { get; set; }
        public String? Name { get; set; }
    }
}
