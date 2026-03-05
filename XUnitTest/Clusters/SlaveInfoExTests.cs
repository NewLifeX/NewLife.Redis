using System;
using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class SlaveInfoExTests
{
    [Fact(DisplayName = "解析空字符串返回null")]
    public void ParseEmpty()
    {
        var result = SlaveInfo.Parse("");
        Assert.Null(result);
    }

    [Fact(DisplayName = "解析null返回null")]
    public void ParseNull()
    {
        var result = SlaveInfo.Parse(null);
        Assert.Null(result);
    }

    [Fact(DisplayName = "EndPoint属性")]
    public void EndPointProperty()
    {
        var str = "ip=192.168.1.100,port=6380,state=online,offset=100,lag=1";
        var inf = SlaveInfo.Parse(str);

        Assert.NotNull(inf);
        Assert.Equal("192.168.1.100:6380", inf.EndPoint);
    }

    [Fact(DisplayName = "ToString返回EndPoint")]
    public void ToStringTest()
    {
        var str = "ip=10.0.0.1,port=6379,state=online,offset=0,lag=0";
        var inf = SlaveInfo.Parse(str);

        Assert.NotNull(inf);
        Assert.Equal("10.0.0.1:6379", inf.ToString());
    }

    [Fact(DisplayName = "离线状态解析")]
    public void ParseOfflineState()
    {
        var str = "ip=127.0.0.1,port=6002,state=offline,offset=0,lag=-1";
        var inf = SlaveInfo.Parse(str);

        Assert.NotNull(inf);
        Assert.Equal("offline", inf.State);
    }
}
