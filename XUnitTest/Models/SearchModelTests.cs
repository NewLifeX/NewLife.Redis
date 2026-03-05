using System;
using NewLife.Caching.Models;
using Xunit;

namespace XUnitTest.Models;

public class SearchModelTests
{
    [Fact(DisplayName = "默认值测试")]
    public void DefaultValues()
    {
        var model = new SearchModel();
        Assert.Null(model.Pattern);
        Assert.Equal(0, model.Count);
        Assert.Equal(0, model.Position);
    }

    [Fact(DisplayName = "属性设置测试")]
    public void SetProperties()
    {
        var model = new SearchModel
        {
            Pattern = "test:*",
            Count = 100,
            Position = 50,
        };

        Assert.Equal("test:*", model.Pattern);
        Assert.Equal(100, model.Count);
        Assert.Equal(50, model.Position);
    }
}
