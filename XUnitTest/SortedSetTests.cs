using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Caching;
using NewLife.Security;
using Xunit;

namespace XUnitTest
{
    public class RedisSortedSetTests
    {
        private readonly FullRedis _redis;

        public RedisSortedSetTests() => _redis = new FullRedis("127.0.0.1:6379", null, 2);

        [Fact]
        public void SortedSet_Normal()
        {
            var rkey = "ss_test";

            // 删除已有
            _redis.Remove(rkey);

            var rss = new RedisSortedSet(_redis, rkey);
            var list = new SortedList<Double, String>();

            // 插入数据
            for (var i = 0; i < 17; i++)
            {
                var key = Rand.NextString(8);
                var score = Rand.Next() / 1000d;

                list.Add(score, key);
                rss.Add(key, score);
            }

            Assert.Equal(list.Count, rss.Count);

            // 删除
            {
                var key = list.Values.First();
                list.Remove(list.Keys.First());

                rss.Remove(key);

                Assert.Equal(list.Count, rss.Count);
            }

            var ks = list.Values.ToList();

            // 取值
            {
                var score = list.Keys.First();
                var key = ks[0];
                Assert.Equal(score, rss.GetScore(key));
            }

            // 最小两个
            {
                var keys = rss.Range(0, 1);
                Assert.Equal(ks[0], keys[0]);
                Assert.Equal(ks[1], keys[1]);
            }

            // 最大三个
            {
                var keys = rss.Range(-2, -1);
                Assert.Equal(ks[^1], keys[1]);
                Assert.Equal(ks[^2], keys[0]);
            }

            // 累加
            {
                var key = ks[^1];
                var score = list.Keys.Last();

                var sc = Rand.Next() / 1000d;
                rss.Increment(key, sc);

                Assert.Equal(score + sc, rss.GetScore(key));
            }

            // 获取指定项的排名
            {
                var idx = Rand.Next(ks.Count);
                var key = ks[idx];

                var rank = rss.Rank(key);
                Assert.Equal(idx, rank);
            }
        }
    }
}