using System;
using NewLife.Caching.Clusters;
using Xunit;

namespace XUnitTest.Clusters;

public class ClusterNodeTests
{
    [Fact(DisplayName = "解析主节点行")]
    public void ParseMasterLine()
    {
        var line = "7cf3c4e1a1c3a6bb52778bbfcc457ca1d9460de8 127.0.0.1:6001 myself,master - 0 0 2 connected 1-4 103-105 107 109";

        var node = new ClusterNode();
        node.Parse(line);

        Assert.Equal("7cf3c4e1a1c3a6bb52778bbfcc457ca1d9460de8", node.ID);
        Assert.Equal("127.0.0.1:6001", node.EndPoint);
        Assert.Equal("myself,master", node.Flags);
        Assert.Equal("-", node.Master);
        Assert.False(node.Slave);
        Assert.Equal(1, node.LinkState);

        Assert.Equal(4, node.Slots.Count);

        // 1-4
        Assert.Equal(1, node.Slots[0].From);
        Assert.Equal(4, node.Slots[0].To);

        // 103-105
        Assert.Equal(103, node.Slots[1].From);
        Assert.Equal(105, node.Slots[1].To);

        // 107
        Assert.Equal(107, node.Slots[2].From);
        Assert.Equal(107, node.Slots[2].To);

        // 109
        Assert.Equal(109, node.Slots[3].From);
        Assert.Equal(109, node.Slots[3].To);
    }

    [Fact(DisplayName = "解析从节点行")]
    public void ParseSlaveLine()
    {
        var line = "25cd3fd6d68b49a35e98050c3a7798dc907b905a 127.0.0.1:6002 slave abc123 1548512034793 1548512031738 1 connected";

        var node = new ClusterNode();
        node.Parse(line);

        Assert.Equal("25cd3fd6d68b49a35e98050c3a7798dc907b905a", node.ID);
        Assert.Equal("127.0.0.1:6002", node.EndPoint);
        Assert.Equal("slave", node.Flags);
        Assert.Equal("abc123", node.Master);
        Assert.True(node.Slave);
    }

    [Fact(DisplayName = "解析带端口后缀的节点")]
    public void ParseWithAtPort()
    {
        var line = "25cd3fd6 172.16.10.32:6379@16379 master - 0 0 1 connected 0-5460";

        var node = new ClusterNode();
        node.Parse(line);

        Assert.Equal("172.16.10.32:6379", node.EndPoint);
    }

    [Fact(DisplayName = "包含数据槽判断")]
    public void ContainSlot()
    {
        var line = "7cf3c4e1 127.0.0.1:6001 myself,master - 0 0 2 connected 1-4 103-105 107";

        var node = new ClusterNode();
        node.Parse(line);

        Assert.True(node.Contain(1));
        Assert.True(node.Contain(3));
        Assert.True(node.Contain(4));
        Assert.False(node.Contain(5));
        Assert.True(node.Contain(103));
        Assert.True(node.Contain(104));
        Assert.True(node.Contain(105));
        Assert.False(node.Contain(106));
        Assert.True(node.Contain(107));
        Assert.False(node.Contain(0));
    }

    [Fact(DisplayName = "获取所有槽")]
    public void GetAllSlots()
    {
        var line = "7cf3c4e1 127.0.0.1:6001 myself,master - 0 0 2 connected 1-4 107";

        var node = new ClusterNode();
        node.Parse(line);

        var slots = node.GetSlots();
        Assert.Equal(5, slots.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 107 }, slots);
    }

    [Fact(DisplayName = "解析空行")]
    public void ParseEmptyLine()
    {
        var node = new ClusterNode();
        node.Parse("");
        Assert.Null(node.ID);
    }

    [Fact(DisplayName = "解析导入迁移槽")]
    public void ParseImportingMigrating()
    {
        var line = "7cf3c4e1 127.0.0.1:6001 myself,master - 0 0 2 connected 0-5460 [5461-<-abc123] [5462->-def456]";

        var node = new ClusterNode();
        node.Parse(line);

        Assert.NotNull(node.Importings);
        Assert.True(node.Importings.ContainsKey(5461));
        Assert.Equal("abc123", node.Importings[5461]);

        Assert.NotNull(node.Migratings);
        Assert.True(node.Migratings.ContainsKey(5462));
        Assert.Equal("def456", node.Migratings[5462]);
    }

    [Fact(DisplayName = "解析fail状态")]
    public void ParseFailState()
    {
        var line = "25cd3fd6 127.0.0.1:6002 master,fail? - 0 0 1 connected";

        var node = new ClusterNode();
        node.Parse(line);

        Assert.Equal(0, node.LinkState);
    }

    [Fact(DisplayName = "ToString返回EndPoint")]
    public void ToStringTest()
    {
        var node = new ClusterNode();
        node.Parse("7cf3c4e1 127.0.0.1:6001 master - 0 0 2 connected");
        Assert.Equal("127.0.0.1:6001", node.ToString());
    }
}
