using System;
using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class SentinelInfoExTests
{
    [Fact(DisplayName = "解析空字符串返回null")]
    public void ParseEmpty()
    {
        var result = SentinelInfo.Parse("");
        Assert.Null(result);
    }

    [Fact(DisplayName = "解析null返回null")]
    public void ParseNull()
    {
        var result = SentinelInfo.Parse(null);
        Assert.Null(result);
    }

    [Fact(DisplayName = "EndPoint属性")]
    public void EndPointProperty()
    {
        var str = "192.168.1.100,7001,runid123,0,mymaster,10.0.0.1,6379,0";
        var inf = SentinelInfo.Parse(str);

        Assert.NotNull(inf);
        Assert.Equal("192.168.1.100:7001", inf.EndPoint);
    }

    [Fact(DisplayName = "MasterEndPoint属性")]
    public void MasterEndPointProperty()
    {
        var str = "192.168.1.100,7001,runid123,0,mymaster,10.0.0.1,6379,0";
        var inf = SentinelInfo.Parse(str);

        Assert.NotNull(inf);
        Assert.Equal("10.0.0.1:6379", inf.MasterEndPoint);
    }

    [Fact(DisplayName = "ToString返回EndPoint")]
    public void ToStringTest()
    {
        var str = "192.168.1.100,7001,runid123,0,mymaster,10.0.0.1,6379,0";
        var inf = SentinelInfo.Parse(str);

        Assert.NotNull(inf);
        Assert.Equal("192.168.1.100:7001", inf.ToString());
    }
}
