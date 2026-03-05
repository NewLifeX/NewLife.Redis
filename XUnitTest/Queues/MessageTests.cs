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

    public class TestEntity
    {
        public String? Name { get; set; }
        public Int32 Value { get; set; }
    }
}
