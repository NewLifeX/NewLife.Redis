using System;
using NewLife.Caching.Queues;
using Xunit;

namespace XUnitTest.Queues;

public class RedisQueueStatusTests
{
    [Fact(DisplayName = "默认值测试")]
    public void DefaultValues()
    {
        var status = new RedisQueueStatus();

        Assert.Null(status.Key);
        Assert.Null(status.MachineName);
        Assert.Null(status.UserName);
        Assert.Equal(0, status.ProcessId);
        Assert.Null(status.Ip);
        Assert.Equal(default, status.CreateTime);
        Assert.Equal(default, status.LastActive);
        Assert.Equal(0, status.Consumes);
        Assert.Equal(0, status.Acks);
    }

    [Fact(DisplayName = "属性设置测试")]
    public void SetProperties()
    {
        var now = DateTime.Now;
        var status = new RedisQueueStatus
        {
            Key = "consumer1",
            MachineName = "Server1",
            UserName = "admin",
            ProcessId = 12345,
            Ip = "192.168.1.100",
            CreateTime = now,
            LastActive = now.AddSeconds(30),
            Consumes = 1000,
            Acks = 990,
        };

        Assert.Equal("consumer1", status.Key);
        Assert.Equal("Server1", status.MachineName);
        Assert.Equal("admin", status.UserName);
        Assert.Equal(12345, status.ProcessId);
        Assert.Equal("192.168.1.100", status.Ip);
        Assert.Equal(now, status.CreateTime);
        Assert.Equal(now.AddSeconds(30), status.LastActive);
        Assert.Equal(1000, status.Consumes);
        Assert.Equal(990, status.Acks);
    }
}
