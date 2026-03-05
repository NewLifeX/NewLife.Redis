using System;
using NewLife.Caching.Models;
using Xunit;

namespace XUnitTest.Models;

public class GeoInfoTests
{
    [Fact(DisplayName = "默认值测试")]
    public void DefaultValues()
    {
        var info = new GeoInfo();
        Assert.Null(info.Name);
        Assert.Equal(0.0, info.Longitude);
        Assert.Equal(0.0, info.Latitude);
        Assert.Equal(0.0, info.Distance);
    }

    [Fact(DisplayName = "属性设置测试")]
    public void SetProperties()
    {
        var info = new GeoInfo
        {
            Name = "Beijing",
            Longitude = 116.407526,
            Latitude = 39.90403,
            Distance = 1234.56,
        };

        Assert.Equal("Beijing", info.Name);
        Assert.Equal(116.407526, info.Longitude);
        Assert.Equal(39.90403, info.Latitude);
        Assert.Equal(1234.56, info.Distance);
    }
}
