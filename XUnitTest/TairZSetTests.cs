using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

/// <summary>阿里云 TairZSet 扩展命令测试</summary>
[Collection("Basic")]
public class TairZSetTests : RedisTestBase
{
    [RedisFact(DisplayName = "TairZSet 多维分数增删查")]
    public void Basic()
    {
        var ic = _redis;
        var key = "tair_exzset_test";

        ic.Remove(key);

        try
        {
            var members = new Dictionary<String, String>
            {
                ["a"] = "1#2",
                ["b"] = "3#4",
            };
            Assert.Equal(2, ic.ExZAdd(key, members));
            Assert.Equal("1#2", ic.ExZScore(key, "a"));
            Assert.Equal(2, ic.ExZCard(key));
            Assert.Equal(0, ic.ExZRank(key, "a"));
            Assert.Equal(1, ic.ExZRevRank(key, "a"));
            Assert.Equal(2, ic.ExZCount(key, "-inf", "+inf"));

            var range = ic.ExZRange(key, 0, -1);
            Assert.NotNull(range);
            Assert.Equal(2, range!.Length);

            var scored = ic.ExZRange(key, 0, -1, withScores: true);
            Assert.NotNull(scored);
            Assert.Equal(4, scored!.Length);

            Assert.Equal("3#5", ic.ExZIncrBy(key, "0#1", "b"));
            Assert.Equal(1, ic.ExZRem(key, "b"));
            Assert.Equal(1, ic.ExZCard(key));
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command") || ex.Message.Contains("unknown subcommand"))
        {
            XTrace.WriteLine("Redis不支持TairZSet扩展（需阿里云Tair/TairDB），跳过测试。{0}", ex.Message);
        }
    }
}
