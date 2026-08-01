using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

[Collection("Basic")]
public class SearchTest
{
    protected readonly FullRedis _redis;

    public SearchTest()
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

    [RedisFact(DisplayName = "搜索测试")]
    public void GetSearchTest()
    {
        var ic = _redis;
        for (var i = 0; i < 1000; i++)
        {
            var playGameVo = new PlayGameVo()
            {
                MemberId = (i + 1).ToString(),
                GameMode = 1,
                Num = 10000
            };
            //cache.Cache.Set(RedisConst.PlayGameKey + playGameVo.MemberId, playGameVo);
            //redis.Prefix=RedisConst.PlayGameKey;
            ic.Set("jinshi:member:battle-royale:play-game:" + playGameVo.MemberId, playGameVo);
        }
        var list = ic.Search("jinshi:member:battle-royale:play-game:*", 0, 1000).ToList();
        var all = ic.GetAll<String>(list);
        Assert.True(list.Count == 1000);
        Assert.False(list.Count < 1000);
    }

    [RedisFact(DisplayName = "搜索count默认-1表示全部")]
    public void SearchDefaultCount()
    {
        var ic = _redis;
        var key = "search:count:default:" + Guid.NewGuid().ToString("N");
        try
        {
            for (var i = 0; i < 5; i++)
            {
                ic.Set($"{key}:{i}", i);
            }

            // 基类 Cache.Search(pattern, 0, -1) 默认 count=-1 表示全部，历史上返回空
            var list = ic.Search($"{key}:*").ToList();
            Assert.Equal(5, list.Count);
        }
        finally
        {
            ic.Remove($"{key}:*");
        }
    }

    [RedisFact(DisplayName = "通配符删除Remove")]
    public void RemoveWildcard()
    {
        var ic = _redis;
        var key = "search:remove:wild:" + Guid.NewGuid().ToString("N");
        try
        {
            for (var i = 0; i < 5; i++)
            {
                ic.Set($"{key}:{i}", i);
            }

            // 历史上 Search 返回空导致通配符删除失效
            var rs = ic.Remove($"{key}:*");
            Assert.Equal(5, rs);

            Assert.False(ic.ContainsKey($"{key}:0"));
        }
        finally
        {
            ic.Remove($"{key}:*");
        }
    }

    [RedisFact(DisplayName = "批量操作不修改调用方数组")]
    public void BatchNotMutateInput()
    {
        var ic = new FullRedis();
        ic.Init(BasicTest.GetConfig());
        ic.Db = 3;
        ic.Retry = 0;
        ic.Prefix = "pfx:";

        var keys = new[] { "batch:a", "batch:b", "batch:c" };
        var backup = (String[])keys.Clone();
        try
        {
            ic.Set(keys[0], 1);
            ic.Set(keys[1], 2);
            ic.Set(keys[2], 3);

            ic.Remove(keys);
            // 历史上 OnRemove 会原地给入参加前缀，破坏调用方数组
            Assert.Equal(backup, keys);
        }
        finally
        {
            ic.Remove(keys);
        }
    }
}

public class PlayGameVo
{
    public String MemberId { get; set; }
    public Int32 GameMode { get; set; }
    public Int32 Num { get; set; }
}
