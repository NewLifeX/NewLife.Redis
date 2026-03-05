using System;
using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class SlotTests
{
    [Fact(DisplayName = "单一槽ToString")]
    public void SingleSlot()
    {
        var slot = new Slot { From = 100, To = 100 };
        Assert.Equal("100", slot.ToString());
    }

    [Fact(DisplayName = "范围槽ToString")]
    public void RangeSlot()
    {
        var slot = new Slot { From = 0, To = 5460 };
        Assert.Equal("0-5460", slot.ToString());
    }

    [Fact(DisplayName = "默认值测试")]
    public void DefaultValues()
    {
        var slot = new Slot();
        Assert.Equal(0, slot.From);
        Assert.Equal(0, slot.To);
        Assert.Equal("0", slot.ToString());
    }
}
