using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

[Collection("Basic")]
public class HashTest
{
    protected readonly FullRedis _redis;

    public HashTest()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    [Fact]
    public void HMSETTest()
    {
        var key = "hash_key";

        // 删除已有
        _redis.Remove(key);

        var hash = _redis.GetDictionary<String>(key) as RedisHash<String, String>;
        Assert.NotNull(hash);

        var dic = new Dictionary<String, String>
        {
            ["aaa"] = "123",
            ["bbb"] = "456"
        };
        var rs = hash.HMSet(dic);
        Assert.True(rs);
        Assert.Equal(2, hash.Count);

        Assert.True(hash.ContainsKey("aaa"));
    }

    [Fact]
    public void Search()
    {
        var rkey = "hash_Search";

        // 删除已有
        _redis.Remove(rkey);

        var hash = _redis.GetDictionary<Double>(rkey);
        var hash2 = hash as RedisHash<String, Double>;

        // 插入数据
        hash.Add("stone1", 12.34);
        hash.Add("stone2", 13.56);
        hash.Add("stone3", 14.34);
        hash.Add("stone4", 15.34);
        Assert.Equal(4, hash.Count);

        var dic = hash2.Search("*one?", 3).ToDictionary(e => e.Key, e => e.Value);
        Assert.Equal(3, dic.Count);
        Assert.Equal("stone1", dic.Skip(0).First().Key);
        Assert.Equal("stone2", dic.Skip(1).First().Key);
        Assert.Equal("stone3", dic.Skip(2).First().Key);
    }

    [Fact]
    public void QuoteTest()
    {
        var key = "hash_quote";

        // 删除已有
        _redis.Remove(key);

        var hash = _redis.GetDictionary<String>(key);
        Assert.NotNull(hash);

        var org1 = "\"TypeName\":\"集团\"";
        var org5 = "\"LastUpdateTime\":\"2021-10-12 20:07:03\"";
        hash["org1"] = org1;
        hash["org5"] = org5;

        Assert.Equal(org1, hash["org1"]);

        Assert.Equal(org5, hash["org5"]);
    }

    [Fact]
    public void CheckHashTest()
    {
        var key = $"NewLife:eventinfo:adsfasdfasdfdsaf";

        var hash = _redis.GetDictionary<EventInfo>(key);
        Assert.NotNull(hash);

        var rh = hash as RedisHash<String, EventInfo>;

        foreach (var item in rh.GetAll())
        {
            XTrace.WriteLine(item.Key);
        }

        rh["0"] = new EventInfo { EventId = "1234", EventName = "Stone" };
    }

    [Fact]
    public void RemoveTest()
    {
        var key = $"NewLife:eventinfo:adsfasdfasdfdsaf";

        var hash = _redis.GetDictionary<EventInfo>(key);
        Assert.NotNull(hash);

        var rh = hash as RedisHash<String, EventInfo>;

        foreach (var item in rh.GetAll())
        {
            XTrace.WriteLine(item.Key);
        }

        rh["0"] = new EventInfo { EventId = "1234", EventName = "Stone" };
        rh["1"] = new EventInfo { EventId = "12345", EventName = "Stone" };
        rh["2"] = new EventInfo { EventId = "123456", EventName = "Stone" };

        rh.Remove("0");
        Assert.Equal(2, rh.Count);
    }

    class EventInfo
    {
        public String EventId { get; set; }
        public String EventName { get; set; }
    }
}

public class HashTest2 : HashTest
{
    public HashTest2() : base()
    {
        _redis.Prefix = "NewLife:";
    }
}