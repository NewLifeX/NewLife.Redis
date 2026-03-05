using System;
using NewLife.Caching;
using Xunit;

namespace XUnitTest;

public class RedisExceptionTests
{
    [Fact(DisplayName = "消息构造测试")]
    public void MessageCtor()
    {
        var ex = new RedisException("test error");
        Assert.Equal("test error", ex.Message);
    }

    [Fact(DisplayName = "内部异常构造测试")]
    public void InnerExceptionCtor()
    {
        var inner = new InvalidOperationException("inner error");
        var ex = new RedisException("outer error", inner);
        Assert.Equal("outer error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact(DisplayName = "异常继承测试")]
    public void InheritanceTest()
    {
        var ex = new RedisException("test");
        Assert.IsAssignableFrom<Exception>(ex);
    }
}
