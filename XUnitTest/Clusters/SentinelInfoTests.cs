using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class SentinelInfoTests
{
    [Fact]
    public void Parse()
    {
        var str = "127.0.0.1,7003,2890784206bf38ba9f9fbf7b61547f8524331c7a,0,mymaster,127.0.0.1,6379,0";

        var inf = SentinelInfo.Parse(str);
        Assert.NotNull(inf);
        Assert.Equal("127.0.0.1", inf.IP);
        Assert.Equal(7003, inf.Port);
        Assert.Equal("2890784206bf38ba9f9fbf7b61547f8524331c7a", inf.RunId);
        Assert.Equal(0, inf.Age);
        Assert.Equal("mymaster", inf.MasterName);
        Assert.Equal("127.0.0.1", inf.MasterIP);
        Assert.Equal(6379, inf.MasterPort);
        Assert.Equal(0, inf.MasterAge);
    }
}
