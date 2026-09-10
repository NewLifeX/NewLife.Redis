using System;
using NewLife.Caching.Queues;
using Xunit;

namespace XUnitTest.Queues;

public class MessageTests
{
    [Fact(DisplayName = "默认值测试")]
    public void DefaultValues()
    {
        var msg = new Message();
        Assert.Null(msg.Id);
        Assert.Null(msg.Body);
    }

    [Fact(DisplayName = "属性设置测试")]
    public void SetProperties()
    {
        var msg = new Message
        {
            Id = "1234567890-0",
            Body = ["key1", "value1", "key2", "value2"],
        };

        Assert.Equal("1234567890-0", msg.Id);
        Assert.Equal(4, msg.Body.Length);
    }

    [Fact(DisplayName = "GetBody空Body返回默认值")]
    public void GetBodyNullBody()
    {
        var msg = new Message();
        var result = msg.GetBody<String>();
        Assert.Null(result);
    }

    [Fact(DisplayName = "GetBody空数组返回默认值")]
    public void GetBodyEmptyBody()
    {
        var msg = new Message { Body = [] };
        var result = msg.GetBody<String>();
        Assert.Null(result);
    }

    [Fact(DisplayName = "GetBody返回String数组")]
    public void GetBodyStringArray()
    {
        var msg = new Message
        {
            Body = ["key1", "value1"],
        };

        var result = msg.GetBody<String[]>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
    }

    [Fact(DisplayName = "GetBody返回Object")]
    public void GetBodyObject()
    {
        var msg = new Message
        {
            Body = ["key1", "value1"],
        };

        var result = msg.GetBody<Object>();
        Assert.NotNull(result);
    }

    [Fact(DisplayName = "GetBody解码__data")]
    public void GetBodyData()
    {
        var msg = new Message
        {
            Body = ["__data", "hello world"],
        };

        var result = msg.GetBody<String>();
        Assert.Equal("hello world", result);
    }

    [Fact(DisplayName = "GetBody解码为实体")]
    public void GetBodyEntity()
    {
        var msg = new Message
        {
            Body = ["Name", "test", "Value", "123"],
        };

        var result = msg.GetBody<TestEntity>();
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(123, result.Value);
    }

    [Fact(DisplayName = "GetBody单字符串连接")]
    public void GetBodySingleString()
    {
        var msg = new Message
        {
            Body = ["hello", "world"],
        };

        var result = msg.GetBody<String>();
        Assert.Equal("hello,world", result);
    }

    [Fact(DisplayName = "GetValue命中返回字段值")]
    public void GetValueHit()
    {
        var msg = new Message
        {
            Body = ["key1", "value1", "key2", "value2"],
        };

        Assert.Equal("value1", msg.GetValue("key1"));
        Assert.Equal("value2", msg.GetValue("key2"));
    }

    [Fact(DisplayName = "GetValue未命中返回null")]
    public void GetValueMiss()
    {
        var msg = new Message
        {
            Body = ["key1", "value1"],
        };

        Assert.Null(msg.GetValue("key2"));
    }

    [Fact(DisplayName = "GetValue空Body返回null")]
    public void GetValueNullBody()
    {
        Assert.Null(new Message().GetValue("key1"));
        Assert.Null(new Message { Body = [] }.GetValue("key1"));
    }

    [Fact(DisplayName = "GetBody基元类型尾部附加字段仍可解码")]
    public void GetBodyPrimitiveWithTailExtras()
    {
        // 字段增强器把附加字段放在数据字段之后
        var msg = new Message
        {
            Body = ["__data", "1234", "__clientId", "tester"],
        };

        Assert.Equal(1234, msg.GetBody<Int32>());
        Assert.Equal("1234", msg.GetBody<String>());
        Assert.Equal("tester", msg.GetValue("__clientId"));
    }

    [Fact(DisplayName = "GetBody基元类型头部附加字段仍可解码")]
    public void GetBodyPrimitiveWithHeadExtras()
    {
        var msg = new Message
        {
            Body = ["__clientId", "tester", "__data", "1234"],
        };

        Assert.Equal(1234, msg.GetBody<Int32>());
        Assert.Equal("1234", msg.GetBody<String>());
    }

    [Fact(DisplayName = "GetBody支持自定义数据字段名")]
    public void GetBodyCustomPrimitiveKey()
    {
        var msg = new Message
        {
            Body = ["data", "42", "__clientId", "tester"],
        };

        Assert.Equal(42, msg.GetBody<Int32>("data"));
    }

    [Fact(DisplayName = "GetBody实体解码忽略尾部附加字段")]
    public void GetBodyEntityWithTailExtras()
    {
        var msg = new Message
        {
            Body = ["Name", "test", "Value", "123", "__clientId", "tester"],
        };

        var result = msg.GetBody<TestEntity>();
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(123, result.Value);
        Assert.Equal("tester", msg.GetValue("__clientId"));
    }

    public class TestEntity
    {
        public String? Name { get; set; }
        public Int32 Value { get; set; }
    }
}
