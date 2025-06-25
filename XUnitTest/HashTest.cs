using System;
using System.Collections.Generic;
using System.Linq;

using NewLife.Caching;
using NewLife.Log;
using NewLife.Serialization;

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

    [Fact(DisplayName = "获取所有数据，丢失数据bug")]
    public void ValuesHashTest()
    {
        //RedisHash、RedisList[Values、Values、GetAll、Search] redis 5.0 都有类似的情况
        //大批量数据获取，大概率会数据不完整，具体原有不明
        var key = $"NewLife:HashTestInfo:Test";
        {
            _redis.MaxMessageSize = Int32.MaxValue;
            var hash = _redis.GetDictionary<String>(key) as RedisHash<String, String>;
            hash.Clear();
            for (var i = 0; i < 10000; i++)
            {
                var k = i.ToString();
                hash.Add(k, new EventInfo { EventId = k, EventName = k }.ToJson());
            }

            //直接获取全部数据，如泛型对象的直接报错
            var list = hash.Values.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                Assert.NotEmpty(list[i]);
                try
                {
                    var item = list[i].ToJsonEntity<EventInfo>();
                }
                catch (Exception ex)
                {
                    //某块连续的数据段可能会不完整
                    Assert.Fail($"Index:{i} Item:{list[i]} Msg:{ex.Message}");
                }
            }
        }
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