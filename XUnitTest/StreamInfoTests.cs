using System;
using System.Text;
using NewLife.Caching.Queues;
using NewLife.Data;
using Xunit;

namespace XUnitTest;

[Collection("Basic")]
public class StreamInfoTests
{
    /// <summary>从字符串创建 IPacket 辅助方法</summary>
    private static IPacket MakePacket(String str)
    {
        return new ArrayPacket(Encoding.UTF8.GetBytes(str));
    }

    /// <summary>正常解析完整的 Redis 7.x XINFO STREAM 响应</summary>
    [Fact(DisplayName = "正常解析完整流信息")]
    public void Parse_NormalData_ShouldSucceed()
    {
        var data = new Object[]
        {
            MakePacket("length"), (Int64)7,
            MakePacket("radix-tree-keys"), (Int64)1,
            MakePacket("radix-tree-nodes"), (Int64)2,
            MakePacket("last-generated-id"), MakePacket("1782061658234-0"),
            MakePacket("max-deleted-entry-id"), MakePacket("0-0"),
            MakePacket("entries-added"), (Int64)12178,
            MakePacket("recorded-first-entry-id"), MakePacket("1781841443266-0"),
            MakePacket("groups"), (Int64)30,
            MakePacket("first-entry"), new Object[]
            {
                MakePacket("1781841443266-0"),
                new Object[] { MakePacket("__data"), MakePacket("{\"Id\":50991}") }
            },
            MakePacket("last-entry"), new Object[]
            {
                MakePacket("1782061658234-0"),
                new Object[] { MakePacket("__data"), MakePacket("{\"Id\":50997}") }
            },
        };

        var info = new StreamInfo();
        info.Parse(data);

        Assert.Equal(7, info.Length);
        Assert.Equal(1, info.RadixTreeKeys);
        Assert.Equal(2, info.RadixTreeNodes);
        Assert.Equal("1782061658234-0", info.LastGeneratedId);
        Assert.Equal(30, info.Groups);
        Assert.Equal(12178, info.EntriesAdded);
        Assert.Equal("1781841443266-0", info.FirstId);
        Assert.NotNull(info.FirstValues);
        Assert.Equal(2, info.FirstValues!.Length);
        Assert.Equal("__data", info.FirstValues[0]);
        Assert.Equal("{\"Id\":50991}", info.FirstValues[1]);
        Assert.Equal("1782061658234-0", info.LastId);
        Assert.NotNull(info.LastValues);
        Assert.Equal(2, info.LastValues!.Length);
        Assert.Equal("__data", info.LastValues[0]);
        Assert.Equal("{\"Id\":50997}", info.LastValues[1]);
    }

    /// <summary>first-entry 为空数组时不应抛异常</summary>
    [Fact(DisplayName = "first-entry为空数组不抛异常")]
    public void Parse_EmptyFirstEntry_ShouldNotThrow()
    {
        var data = new Object[]
        {
            MakePacket("length"), (Int64)0,
            MakePacket("groups"), (Int64)0,
            MakePacket("first-entry"), Array.Empty<Object>(),
            MakePacket("last-entry"), Array.Empty<Object>(),
        };

        var info = new StreamInfo();
        info.Parse(data);

        Assert.Equal(0, info.Length);
        Assert.Null(info.FirstId);
        Assert.Null(info.FirstValues);
        Assert.Null(info.LastId);
        Assert.Null(info.LastValues);
    }

    /// <summary>first-entry 只有一个元素时不应抛异常</summary>
    [Fact(DisplayName = "first-entry仅一个元素不抛异常")]
    public void Parse_FirstEntrySingleElement_ShouldNotThrow()
    {
        var data = new Object[]
        {
            MakePacket("length"), (Int64)1,
            MakePacket("first-entry"), new Object[] { MakePacket("1000-0") },
            MakePacket("last-entry"), new Object[] { MakePacket("1000-0") },
        };

        var info = new StreamInfo();
        info.Parse(data);

        Assert.Null(info.FirstValues);
        Assert.Null(info.LastValues);
    }

    /// <summary>空输入数组不抛异常</summary>
    [Fact(DisplayName = "空数组不抛异常")]
    public void Parse_EmptyArray_ShouldNotThrow()
    {
        var info = new StreamInfo();
        info.Parse(Array.Empty<Object>());

        Assert.Equal(0, info.Length);
        Assert.Equal(0, info.Groups);
    }

    /// <summary>null 输入不抛异常</summary>
    [Fact(DisplayName = "null输入不抛异常")]
    public void Parse_NullInput_ShouldNotThrow()
    {
        var info = new StreamInfo();
        info.Parse(null!);

        Assert.Equal(0, info.Length);
    }

    /// <summary>奇数长度数组正确处理最后未配对元素被忽略</summary>
    [Fact(DisplayName = "奇数长度数组安全处理")]
    public void Parse_OddLengthArray_ShouldBeSafe()
    {
        var data = new Object[]
        {
            MakePacket("length"), (Int64)3,
            MakePacket("groups"), (Int64)1,
            MakePacket("extra"), // 无配对值
        };

        var info = new StreamInfo();
        info.Parse(data);

        Assert.Equal(3, info.Length);
        Assert.Equal(1, info.Groups);
    }

    /// <summary>缺失 first-entry 和 last-entry 不抛异常</summary>
    [Fact(DisplayName = "缺失first/last-entry不抛异常")]
    public void Parse_MissingFirstLastEntry_ShouldNotThrow()
    {
        var data = new Object[]
        {
            MakePacket("length"), (Int64)5,
            MakePacket("radix-tree-keys"), (Int64)1,
            MakePacket("radix-tree-nodes"), (Int64)1,
            MakePacket("last-generated-id"), MakePacket("1782061658234-0"),
            MakePacket("groups"), (Int64)2,
            MakePacket("entries-added"), (Int64)500,
        };

        var info = new StreamInfo();
        info.Parse(data);

        Assert.Equal(5, info.Length);
        Assert.Equal(2, info.Groups);
        Assert.Equal(500, info.EntriesAdded);
        Assert.Equal("1782061658234-0", info.LastGeneratedId);
        Assert.Null(info.FirstId);
        Assert.Null(info.LastId);
        Assert.NotEqual(default, info.LastGenerated);
    }

    /// <summary>last-generated-id 无连字符时安全处理</summary>
    [Fact(DisplayName = "last-generated-id无连字符安全处理")]
    public void Parse_LastGeneratedIdNoDash_ShouldBeSafe()
    {
        var data = new Object[]
        {
            MakePacket("length"), (Int64)1,
            MakePacket("last-generated-id"), MakePacket("1782061658234"),
        };

        var info = new StreamInfo();
        info.Parse(data);

        Assert.Equal("1782061658234", info.LastGeneratedId);
    }

    /// <summary>last-generated-id 为空时不计算时间</summary>
    [Fact(DisplayName = "last-generated-id为空不计算时间")]
    public void Parse_NullLastGeneratedId_ShouldNotComputeTime()
    {
        var data = new Object[]
        {
            MakePacket("length"), (Int64)0,
        };

        var info = new StreamInfo();
        info.Parse(data);

        Assert.Null(info.LastGeneratedId);
        Assert.Equal(default, info.LastGenerated);
    }
}
