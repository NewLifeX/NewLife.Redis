using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class MasterInfoTest
{
    [Fact]
    public void Parse()
    {
        var str = "name=redis-master,status=ok,address=127.0.0.1:6379,slaves=3,sentinels=3";

        var inf = MasterInfo.Parse(str);
        Assert.NotNull(inf);
        Assert.Equal("redis-master", inf.Name);
        Assert.Equal("ok", inf.Status);
        Assert.Equal("127.0.0.1", inf.IP);
        Assert.Equal(6379, inf.Port);
        Assert.Equal(3, inf.Slaves);
        Assert.Equal(3, inf.Sentinels);
    }
}
