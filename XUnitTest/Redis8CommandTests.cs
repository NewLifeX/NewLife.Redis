using System;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

/// <summary>Redis 8.x 增量命令测试</summary>
[Collection("Basic")]
public class Redis8CommandTests : RedisTestBase
{
    [RedisFact(DisplayName = "BITOP 8.2 新运算符")]
    public void BitOpNewOperators()
    {
        var ic = _redis;
        if (ic.Version < new Version(8, 2))
        {
            XTrace.WriteLine("BITOP 新运算符需要 Redis 8.2+，当前版本 {0}，跳过测试。", ic.Version);
            return;
        }

        var key1 = "bitop82_a";
        var key2 = "bitop82_b";
        var dest = "bitop82_d";        ic.Remove(key1, key2, dest);

        // key1 = 0b1111，key2 = 0b0011
        for (var i = 0; i < 4; i++) ic.SetBit(key1, i, 1);
        for (var i = 0; i < 2; i++) ic.SetBit(key2, i, 1);

        Assert.True(ic.BitOp("DIFF", dest, key1, key2) > 0);
        Assert.Equal(0, ic.GetBit(dest, 0));
        Assert.Equal(1, ic.GetBit(dest, 2));

        Assert.True(ic.BitOp("DIFF1", dest, key1, key2) > 0);
        Assert.Equal(0, ic.GetBit(dest, 1));

        Assert.True(ic.BitOp("ANDOR", dest, key1, key2) > 0);
        Assert.Equal(1, ic.GetBit(dest, 1));
        Assert.Equal(0, ic.GetBit(dest, 2));

        Assert.True(ic.BitOp("ONE", dest, key1, key2) > 0);
        Assert.Equal(0, ic.GetBit(dest, 0));
        Assert.Equal(1, ic.GetBit(dest, 3));

        ic.Remove(key1, key2, dest);
    }

    [RedisFact(DisplayName = "XDELEX/XACKDEL 8.2 消息删除")]
    public void StreamDeleteCommands()
    {
        var ic = _redis;
        if (ic.Version < new Version(8, 2))
        {
            XTrace.WriteLine("XDELEX/XACKDEL 需要 Redis 8.2+，当前版本 {0}，跳过测试。", ic.Version);
            return;
        }

        var key = "stream82_test";
        ic.Remove(key);

        var stream = ic.GetStream<String>(key);
        stream.GroupCreate("g1", "0");
        var id1 = stream.Add("a");
        var id2 = stream.Add("b");
        Assert.NotNull(id1);
        Assert.NotNull(id2);

        var deleted = stream.DeleteMany([id1!, id2!], "KEEPREF");
        Assert.NotNull(deleted);
        Assert.Equal(2, deleted!.Length);
        Assert.All(deleted, n => Assert.Equal(1, n));

        var id3 = stream.Add("c");
        Assert.NotNull(id3);
        var messages = stream.ReadGroup("g1", "c1", 10, ">");
        Assert.NotEmpty(messages);

        var acked = stream.AckDelete("g1", [id3!], "DELREF");
        Assert.NotNull(acked);
        Assert.Equal(1, acked![0]);

        ic.Remove(key);
    }

    [RedisFact(DisplayName = "DELEX/DIGEST/MSETEX 8.4 字符串命令")]
    public void StringCommands84()
    {
        var ic = _redis;
        if (ic.Version < new Version(8, 4))
        {
            XTrace.WriteLine("DELEX/DIGEST/MSETEX 需要 Redis 8.4+，当前版本 {0}，跳过测试。", ic.Version);
            return;
        }

        var key = "str84_test";
        var key1 = "str84_k1";
        var key2 = "str84_k2";
        ic.Remove(key, key1, key2);

        ic.Set(key, "hello");
        var digest = ic.Digest(key);
        Assert.NotNull(digest);

        Assert.False(ic.DelEx(key, "IFEQ", "world"));
        Assert.True(ic.DelEx(key, "IFEQ", "hello"));
        Assert.False(ic.ContainsKey(key));

        ic.Set(key, "hello");
        Assert.True(ic.DelEx(key, "IFDEQ", digest!));
        Assert.False(ic.ContainsKey(key));

        Assert.False(ic.SetIf(key, "world", "IFEQ", "hello"));
        ic.Set(key, "hello");
        Assert.True(ic.SetIf(key, "world", "IFEQ", "hello", 60));
        Assert.Equal("world", ic.Get<String>(key));
        Assert.True(ic.PTtl(key) > 0);

        var values = new Dictionary<String, String>
        {
            [key1] = "v1",
            [key2] = "v2",
        };
        Assert.True(ic.MSetEx(values, expire: 60));
        Assert.Equal("v1", ic.Get<String>(key1));
        Assert.True(ic.PTtl(key1) > 0);

        ic.Set(key1, "v1");
        Assert.False(ic.MSetEx(values, "NX"));
        ic.Set(key2, "v2");
        Assert.True(ic.MSetEx(values, "XX"));

        ic.Remove(key1, key2);
    }

    [RedisFact(DisplayName = "SUNIONCARD/SDIFFCARD/LMOVEM 8.10 命令")]
    public void Commands810()
    {
        var ic = _redis;
        if (ic.Version < new Version(8, 10))
        {
            XTrace.WriteLine("SUNIONCARD/SDIFFCARD/LMOVEM 需要 Redis 8.10+，当前版本 {0}，跳过测试。", ic.Version);
            return;
        }

        var set1 = "set810_a";
        var set2 = "set810_b";
        ic.Remove(set1, set2);
        var s1 = ic.GetSet<String>(set1);
        s1.Add("a");
        s1.Add("b");
        s1.Add("c");
        var s2 = ic.GetSet<String>(set2);
        s2.Add("b");
        s2.Add("c");
        s2.Add("d");

        Assert.Equal(4, ic.SUnionCard(set1, set2));
        Assert.Equal(1, ic.SDiffCard(set1, set2));

        var list1 = "list810_a";
        var list2 = "list810_b";
        ic.Remove(list1, list2);
        ic.RPUSH(list1, "a", "b", "c");

        var moved = ic.LMoveM<String>(list1, list2, "LEFT", "RIGHT", 2);
        Assert.NotNull(moved);
        Assert.Equal(2, moved!.Length);
        Assert.Equal(2, ic.GetList<String>(list2).Count);

        ic.Remove(list1, list2, set1, set2);
    }
}
