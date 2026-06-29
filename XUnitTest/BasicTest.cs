using System;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

// 所有测试用例放入一个汇编级集合，除非单独指定Collection特性
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace XUnitTest;

[Collection("Basic")]
public class BasicTest
{
    protected readonly FullRedis _redis;

    public BasicTest()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Db = 2;
        _redis.Retry = 0;
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    private static String _config;
    public static String GetConfig()
    {
        if (_config != null) return _config;
        lock (typeof(BasicTest))
        {
            if (_config != null) return _config;

            var config = "";
            var file = @"config\redis.config";
            if (File.Exists(file)) config = File.ReadAllText(file.GetFullPath())?.Trim();
            if (config.IsNullOrEmpty()) config = "server=127.0.0.1:6379;db=3";
            if (!File.Exists(file)) File.WriteAllText(file.EnsureDirectory(true).GetFullPath(), config);

            XTrace.WriteLine("Redis配置：{0}", config);

            return _config = config;
        }
    }

    [Fact(DisplayName = "信息测试")]
    public void InfoTest()
    {
        var inf = _redis.Execute(null, (client, k) => client.Execute<String>("info"));
        Assert.NotNull(inf);
    }

    [Fact(DisplayName = "字符串测试")]
    public void GetSet()
    {
        var ic = _redis;
        var key = "Name";

        // 添加删除
        ic.Set(key, Environment.UserName);
        ic.Append(key, "_XXX");
        var name = ic.Get<String>(key);
        Assert.Equal(Environment.UserName + "_XXX", name);

        var name2 = ic.GetRange(key, 0, Environment.UserName.Length - 1);
        Assert.Equal(Environment.UserName, name2);

        ic.SetRange(key, name.Length - 2, "YY");
        var name3 = ic.Get<String>(key);
        Assert.Equal(Environment.UserName + "_XYY", name3);

        var len = ic.StrLen(key);
        Assert.Equal((Environment.UserName + "_XYY").Length, len);
    }

    [Fact(DisplayName = "搜索测试")]
    public void SearchTest()
    {
        var ic = _redis;
        var key = "Company";
        var key2 = "Company2";

        // 添加删除
        ic.Set(key, Environment.UserName);
        ic.Rename(key, key2);
        Assert.True(ic.ContainsKey(key2));
        Assert.False(ic.ContainsKey(key));

        //var ss = ic.Search("*");
        //Assert.True(ss.Length > 0);

        var ss2 = ic.Search("Company*", 0, 10).ToArray();
        Assert.True(ss2.Length > 0);

        //var ss3 = ic.Search("ReliableQueue:Status:*", 100).ToArray();
        //Assert.True(ss3.Length > 0);
    }

    [Fact]
    public void GetInfo()
    {
        var rds = _redis.CreateSub(0);
        var inf = rds.GetInfo(true);
        Assert.NotNull(inf);
    }

    [Fact(DisplayName = "UNLINK异步删除测试")]
    public void UnlinkTest()
    {
        var ic = _redis;
        var key1 = "unlink_test1";
        var key2 = "unlink_test2";

        ic.Set(key1, "value1");
        ic.Set(key2, "value2");
        Assert.True(ic.ContainsKey(key1));
        Assert.True(ic.ContainsKey(key2));

        var count = ic.Unlink(key1, key2);
        Assert.Equal(2, count);
        Assert.False(ic.ContainsKey(key1));
        Assert.False(ic.ContainsKey(key2));
    }

    [Fact(DisplayName = "TOUCH更新LRU测试")]
    public void TouchTest()
    {
        var ic = _redis;
        var key1 = "touch_test1";
        var key2 = "touch_test2";

        ic.Set(key1, "value1");
        ic.Set(key2, "value2");

        var count = ic.Touch(key1, key2);
        Assert.Equal(2, count);
    }

    [Fact(DisplayName = "COPY复制键测试")]
    public void CopyTest()
    {
        var ic = _redis;
        var src = "copy_src";
        var dst = "copy_dst";

        ic.Set(src, "hello");
        var ok = ic.Copy(src, dst);
        Assert.True(ok);
        Assert.True(ic.ContainsKey(dst));
        Assert.Equal("hello", ic.Get<String>(dst));
    }

    [Fact(DisplayName = "COPY复制键覆盖测试")]
    public void CopyReplaceTest()
    {
        var ic = _redis;
        var src = "copy_src2";
        var dst = "copy_dst2";

        ic.Set(src, "new_value");
        ic.Set(dst, "old_value");
        var ok = ic.Copy(src, dst, replace: true);
        Assert.True(ok);
        Assert.Equal("new_value", ic.Get<String>(dst));
    }

    [Fact(DisplayName = "GETEX获取并设置过期测试")]
    public void GetExTest()
    {
        var ic = _redis;
        var key = "getex_test";

        ic.Set(key, "test_value", 60);
        var val = ic.GetEx<String>(key, 120);
        Assert.Equal("test_value", val);

        // 键应该仍然存在（设置了新过期）
        Assert.True(ic.ContainsKey(key));
    }

    [Fact(DisplayName = "LMOVE原子移动测试")]
    public void LMoveTest()
    {
        var ic = _redis;
        var src = "lmove_src";
        var dst = "lmove_dst";

        // 清理
        ic.Remove(src);
        ic.Remove(dst);

        ic.RPUSH(src, "a", "b", "c");
        var val = ic.LMove<String>(src, dst, "RIGHT", "LEFT");
        Assert.Equal("c", val);
        Assert.Equal(2, ic.Execute(src, (rc, k) => rc.Execute<Int32>("LLEN", k)));
        Assert.Equal(1, ic.Execute(dst, (rc, k) => rc.Execute<Int32>("LLEN", k)));
    }

    [Fact(DisplayName = "BLMOVE阻塞移动测试")]
    public void BLMoveTest()
    {
        var ic = _redis;
        var src = "blmove_src";
        var dst = "blmove_dst";

        // 清理
        ic.Remove(src);
        ic.Remove(dst);

        ic.RPUSH(src, "x");
        var val = ic.BLMove<String>(src, dst, "RIGHT", "LEFT", 1);
        Assert.Equal("x", val);
    }

    [Fact(DisplayName = "SWAPDB交换数据库测试")]
    public void SwapDBTest()
    {
        var ic = _redis;
        var key = "swapdb_test";

        // 在当前DB设置键
        ic.Set(key, "swap_value");

        // 交换DB 2 和 DB 3
        var rs = ic.SwapDB(2, 3);
        Assert.Equal("OK", rs);
    }

    [Fact(DisplayName = "LPOS查找元素位置测试")]
    public void LPosTest()
    {
        var ic = _redis;
        var key = "lpos_test";

        ic.Remove(key);
        ic.RPUSH(key, "a", "b", "c", "b", "d");

        var pos = ic.LPos(key, "b");
        Assert.NotNull(pos);
        Assert.Single(pos);
        Assert.Equal(2, pos[0]);
    }

    [Fact(DisplayName = "SMISMEMBER批量判断成员测试")]
    public void SMIsMemberTest()
    {
        var ic = _redis;
        var key = "smismember_test";

        ic.Remove(key);
        ic.SADD(key, "a", "b", "c");

        var rs = ic.SMIsMember(key, "a", "c", "x");
        Assert.NotNull(rs);
        Assert.Equal(3, rs.Length);
        Assert.Equal(1, rs[0]);
        Assert.Equal(1, rs[1]);
        Assert.Equal(0, rs[2]);
    }

    [Fact(DisplayName = "ZMSCORE批量获取分数测试")]
    public void ZMScoreTest()
    {
        var ic = _redis;
        var key = "zmscore_test";

        ic.Remove(key);
        ic.Execute(key, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b", 30, "c"), true);

        var scores = ic.ZMScore(key, "a", "c", "x");
        Assert.NotNull(scores);
        Assert.Equal(3, scores.Length);
        Assert.Equal(10, scores[0]);
        Assert.Equal(30, scores[1]);
        Assert.Equal(0, scores[2]); // x 不存在，返回 null 转为 0
    }

    [Fact(DisplayName = "ZRANDMEMBER随机成员测试")]
    public void ZRandMemberTest()
    {
        var ic = _redis;
        var key = "zrandmember_test";

        ic.Remove(key);
        ic.Execute(key, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b", 30, "c"), true);

        var members = ic.ZRandMember<String>(key, 2);
        Assert.NotNull(members);
        Assert.Equal(2, members.Length);
    }

    [Fact(DisplayName = "BZPOPMIN阻塞弹出最小测试")]
    public void BZPopMinTest()
    {
        var ic = _redis;
        var key = "bzpopmin_test";

        ic.Remove(key);
        ic.Execute(key, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b"), true);

        var rs = ic.BZPopMin<String>(key, 1);
        Assert.NotNull(rs);
        Assert.Equal("a", rs.Item1);
        Assert.Equal(10, rs.Item2);
    }

    [Fact(DisplayName = "BZPOPMAX阻塞弹出最大测试")]
    public void BZPopMaxTest()
    {
        var ic = _redis;
        var key = "bzpopmax_test";

        ic.Remove(key);
        ic.Execute(key, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b"), true);

        var rs = ic.BZPopMax<String>(key, 1);
        Assert.NotNull(rs);
        Assert.Equal("b", rs.Item1);
        Assert.Equal(20, rs.Item2);
    }

    [Fact(DisplayName = "EXPIRETIME获取过期时间戳测试")]
    public void ExpireTimeTest()
    {
        var ic = _redis;
        var key = "expiretime_test";

        ic.Set(key, "test", 3600);
        var ts = ic.ExpireTime(key);
        Assert.True(ts > 0);
    }

    [Fact(DisplayName = "PEXPIRETIME获取毫秒过期时间戳测试")]
    public void PExpireTimeTest()
    {
        var ic = _redis;
        var key = "pexpiretime_test";

        ic.Set(key, "test", 3600);
        var ts = ic.PExpireTime(key);
        Assert.True(ts > 0);
    }

    [Fact(DisplayName = "MEMORY USAGE内存占用测试")]
    public void MemoryUsageTest()
    {
        var ic = _redis;
        var key = "memory_test";

        ic.Set(key, new String('x', 100));
        var usage = ic.MemoryUsage(key);
        Assert.True(usage > 0);
    }

    [Fact(DisplayName = "SET...GET替换并返回旧值测试")]
    public void SetGetTest()
    {
        var ic = _redis;
        var key = "setget_test";

        ic.Set(key, "old_value");
        var old = ic.SetGet(key, "new_value");
        Assert.Equal("old_value", old);
        Assert.Equal("new_value", ic.Get<String>(key));
    }

    [Fact(DisplayName = "SINTERCARD交集基数测试")]
    public void SInterCardTest()
    {
        var ic = _redis;
        var key1 = "sintercard_test1";
        var key2 = "sintercard_test2";

        ic.Remove(key1);
        ic.Remove(key2);
        ic.SADD(key1, "a", "b", "c");
        ic.SADD(key2, "b", "c", "d");

        var count = ic.SInterCard([key1, key2]);
        Assert.Equal(2, count);
    }

    [Fact(DisplayName = "OBJECT ENCODING编码测试")]
    public void ObjectEncodingTest()
    {
        var ic = _redis;
        var key = "obj_enc_test";

        ic.Set(key, "123");
        var enc = ic.ObjectEncoding(key);
        Assert.NotNull(enc);
    }

    [Fact(DisplayName = "SLOWLOG慢查询日志测试")]
    public void SlowLogTest()
    {
        var ic = _redis;

        var len = ic.SlowLogLen();
        Assert.True(len >= 0);

        var logs = ic.SlowLogGet(5);
        Assert.NotNull(logs);
    }

    [Fact(DisplayName = "WAIT等待副本确认测试")]
    public void WaitTest()
    {
        var ic = _redis;

        // 单机Redis通常返回0（没有副本）
        var count = ic.Wait(1, 100);
        Assert.True(count >= 0);
    }

    [Fact(DisplayName = "LATENCY延迟分析测试")]
    public void LatencyTest()
    {
        var ic = _redis;

        var latest = ic.LatencyLatest();
        Assert.NotNull(latest);
    }

    [Fact(DisplayName = "SETBIT设置位值测试")]
    public void SetBitTest()
    {
        var ic = _redis;
        var key = "bitmap_test";

        ic.Remove(key);
        var old = ic.SetBit(key, 7, 1);
        Assert.Equal(0, old);

        old = ic.SetBit(key, 7, 0);
        Assert.Equal(1, old);
    }

    [Fact(DisplayName = "GETBIT获取位值测试")]
    public void GetBitTest()
    {
        var ic = _redis;
        var key = "bitmap_test2";

        ic.Remove(key);
        ic.SetBit(key, 5, 1);

        var bit = ic.GetBit(key, 5);
        Assert.Equal(1, bit);

        bit = ic.GetBit(key, 99);
        Assert.Equal(0, bit);
    }

    [Fact(DisplayName = "BITCOUNT位计数测试")]
    public void BitCountTest()
    {
        var ic = _redis;
        var key = "bitcount_test";

        ic.Remove(key);
        ic.SetBit(key, 0, 1);
        ic.SetBit(key, 1, 1);
        ic.SetBit(key, 2, 0);

        var count = ic.BitCount(key);
        Assert.Equal(2, count);
    }

    [Fact(DisplayName = "BITOP位运算测试")]
    public void BitOpTest()
    {
        var ic = _redis;
        var key1 = "bitop_src1";
        var key2 = "bitop_src2";
        var dest = "bitop_dest";

        ic.Remove(key1, key2, dest);
        ic.SetBit(key1, 0, 1);
        ic.SetBit(key1, 1, 1);
        ic.SetBit(key2, 0, 1);
        ic.SetBit(key2, 1, 0);

        var len = ic.BitOp("AND", dest, key1, key2);
        Assert.True(len >= 0);
    }
}

public class BasicTest2 : BasicTest
{
    public BasicTest2() : base()
    {
        _redis.Prefix = "NewLife:";
    }
}