using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class SlaveInfoTests
{
    [Fact]
    public void Parse()
    {
        var str = "ip=127.0.0.1,port=6002,state=online,offset=5547321,lag=0";

        var inf = SlaveInfo.Parse(str);
        Assert.NotNull(inf);
        Assert.Equal("127.0.0.1", inf.IP);
        Assert.Equal(6002, inf.Port);
        Assert.Equal("online", inf.State);
        Assert.Equal(5547321, inf.Offset);
        Assert.Equal(0, inf.Lag);
    }
}
