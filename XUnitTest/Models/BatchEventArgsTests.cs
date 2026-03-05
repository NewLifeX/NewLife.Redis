using System;
using NewLife.Caching.Models;
using Xunit;

namespace XUnitTest.Models;

public class BatchEventArgsTests
{
    [Fact(DisplayName = "默认值测试")]
    public void DefaultValues()
    {
        var args = new BatchEventArgs();
        Assert.Null(args.Keys);
    }

    [Fact(DisplayName = "设置Keys")]
    public void SetKeys()
    {
        var keys = new String[] { "key1", "key2", "key3" };
        var args = new BatchEventArgs { Keys = keys };

        Assert.Equal(3, args.Keys.Length);
        Assert.Equal("key1", args.Keys[0]);
        Assert.Equal("key2", args.Keys[1]);
        Assert.Equal("key3", args.Keys[2]);
    }

    [Fact(DisplayName = "继承EventArgs测试")]
    public void InheritanceTest()
    {
        var args = new BatchEventArgs();
        Assert.IsAssignableFrom<EventArgs>(args);
    }
}
