using System;
using NewLife.Caching.Models;
using Xunit;

namespace XUnitTest.Models;

public class ConsumeModesTests
{
    [Fact(DisplayName = "枚举值测试")]
    public void EnumValues()
    {
        Assert.Equal(0, (Int32)ConsumeModes.All);
        Assert.Equal(1, (Int32)ConsumeModes.Last);
    }

    [Fact(DisplayName = "枚举名称测试")]
    public void EnumNames()
    {
        Assert.Equal("All", ConsumeModes.All.ToString());
        Assert.Equal("Last", ConsumeModes.Last.ToString());
    }
}
