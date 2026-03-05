using System;
using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class MasterInfoExTests
{
    [Fact(DisplayName = "解析空字符串返回null")]
    public void ParseEmpty()
    {
        var result = MasterInfo.Parse("");
        Assert.Null(result);
    }

    [Fact(DisplayName = "解析null返回null")]
    public void ParseNull()
    {
        var result = MasterInfo.Parse(null);
        Assert.Null(result);
    }

    [Fact(DisplayName = "EndPoint属性")]
    public void EndPointProperty()
    {
        var str = "name=redis-master,status=ok,address=192.168.1.100:6380,slaves=2,sentinels=3";
        var inf = MasterInfo.Parse(str);

        Assert.NotNull(inf);
        Assert.Equal("192.168.1.100:6380", inf.EndPoint);
    }

    [Fact(DisplayName = "EndPoint为null当IP为空")]
    public void EndPointNull()
    {
        var inf = new MasterInfo();
        Assert.Null(inf.EndPoint);
    }

    [Fact(DisplayName = "ToString返回EndPoint")]
    public void ToStringTest()
    {
        var str = "name=test,status=ok,address=10.0.0.1:6379,slaves=1,sentinels=1";
        var inf = MasterInfo.Parse(str);

        Assert.NotNull(inf);
        Assert.Equal("10.0.0.1:6379", inf.ToString());
    }

    [Fact(DisplayName = "ToString无IP返回类型名")]
    public void ToStringNoIP()
    {
        var inf = new MasterInfo();
        var result = inf.ToString();
        Assert.NotNull(result);
        Assert.Contains("MasterInfo", result);
    }
}
