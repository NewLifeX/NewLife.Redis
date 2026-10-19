using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using NewLife.Caching;
using Xunit;

namespace XUnitTest;

[Collection("Localization")]
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

    [Fact(DisplayName = "解码数字布尔与枚举")]
    public void DecodeBooleanAndEnum()
    {
        Assert.Equal(true, InvokeOnDecode("1", typeof(Boolean)));
        Assert.Equal(false, InvokeOnDecode("0", typeof(Boolean)));
        Assert.Equal(DayOfWeek.Monday, InvokeOnDecode("Monday", typeof(DayOfWeek)));
        Assert.Equal(DayOfWeek.Monday, InvokeOnDecode("1", typeof(DayOfWeek)));
    }

    [Fact(DisplayName = "解码空字符串保留基础类型默认值")]
    public void DecodeEmptyString()
    {
        Assert.Equal(0, InvokeOnDecode("", typeof(Int32)));
        Assert.Equal(0L, InvokeOnDecode("", typeof(Int64)));
        Assert.Equal(false, InvokeOnDecode("", typeof(Boolean)));
        Assert.Equal("", InvokeOnDecode("", typeof(String)));
    }

    [LocalizationFact(DisplayName = "编码不受区域小数点与日期格式影响")]
    public void EncodeUsesInvariantCulture()
    {
        WithCulture("de-DE", () =>
        {
            Assert.Equal("12.34", Encoding.UTF8.GetString(_encoder.Encode(12.34d)!.GetSpan()));
            Assert.Equal("-12.34", Encoding.UTF8.GetString(_encoder.Encode(-12.34d)!.GetSpan()));
            Assert.Equal("-5", Encoding.UTF8.GetString(_encoder.Encode(-5L)!.GetSpan()));
            Assert.Equal("56.78", Encoding.UTF8.GetString(_encoder.Encode(56.78m)!.GetSpan()));
            Assert.Equal("2026-08-01 08:30:15.123", Encoding.UTF8.GetString(_encoder.Encode(new DateTime(2026, 8, 1, 8, 30, 15, 123))!.GetSpan()));
        });

        WithCulture("th-TH", () =>
        {
            Assert.Equal("2026-08-01 08:30:15.123", Encoding.UTF8.GetString(_encoder.Encode(new DateTime(2026, 8, 1, 8, 30, 15, 123))!.GetSpan()));
        });
    }

    [LocalizationFact(DisplayName = "解码不受区域小数点与负号影响")]
    public void DecodeUsesInvariantCulture()
    {
        WithCulture("de-DE", () =>
        {
            Assert.Equal(12.34d, InvokeOnDecode("12.34", typeof(Double)));
            Assert.Equal(56.78m, InvokeOnDecode("56.78", typeof(Decimal)));
            Assert.Equal(new DateTime(2026, 8, 1, 8, 30, 15), InvokeOnDecode("2026-08-01 08:30:15", typeof(DateTime)));
        });

        WithCulture("th-TH", () =>
        {
            Assert.Equal(new DateTime(2026, 8, 1, 8, 30, 15), InvokeOnDecode("2026-08-01 08:30:15", typeof(DateTime)));
        });

        WithCulture("ar-SA", () =>
        {
            Assert.Equal(-5, InvokeOnDecode("-5", typeof(Int32)));
            Assert.Equal(-7L, InvokeOnDecode("-7", typeof(Int64)));
        });
    }

    private static void WithCulture(String name, Action action)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    private class EncoderModel
    {
        public Int32 Id { get; set; }
        public String? Name { get; set; }
    }
}
