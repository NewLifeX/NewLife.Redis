using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest
{
    public class RedisSortedSetTests
    {
        private readonly FullRedis _redis;

        public RedisSortedSetTests()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [Fact]
        public void SortedSet_Normal()
        {
            var rkey = "zset_test";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet(_redis, rkey);
            var list = new SortedList<Double, String>();

            // 插入数据
            for (var i = 0; i < 17; i++)
            {
                var key = Rand.NextString(8);
                var score = Rand.Next() / 1000d;

                list.Add(score, key);
                var rs = zset.Add(key, score);
                Assert.True(rs);
            }

            Assert.Equal(list.Count, zset.Count);

            var ks = list.Keys.ToList();
            var vs = list.Values.ToList();

            // 删除
            {
                list.Remove(ks[0]);

                var rs = zset.Remove(vs[0]);
                Assert.True(rs);

                Assert.Equal(list.Count, zset.Count);

                ks.RemoveAt(0);
                vs.RemoveAt(0);
            }

            // 取值
            {
                var key = vs[0];
                var score = ks[0];

                Assert.Equal(score, zset.GetScore(key));
            }

            // 最小两个
            {
                var keys = zset.Range(0, 1);
                Assert.Equal(vs[0], keys[0]);
                Assert.Equal(vs[1], keys[1]);
            }

            // 最大三个
            {
                var keys = zset.Range(-2, -1);
                Assert.Equal(vs[^1], keys[1]);
                Assert.Equal(vs[^2], keys[0]);
            }

            // 累加
            {
                var key = vs[^1];
                var score = ks[^1];

                var sc = Rand.Next() / 1000d;
                zset.Increment(key, sc);

                Assert.Equal(score + sc, zset.GetScore(key));
            }

            // 获取指定项的排名
            {
                var idx = Rand.Next(vs.Count);
                var key = vs[idx];

                var rank = zset.Rank(key);
                Assert.Equal(idx, rank);
            }
        }

        [Fact]
        public void Adds()
        {
            var rkey = "zset_adds";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet(_redis, rkey);

            // 插入数据
            var count = Rand.Next(1000, 10000);
            var dic = new Dictionary<String, Double>();
            for (var i = 0; i < count; i++)
            {
                var key = Rand.NextString(8);
                var score = Rand.Next() / 1000d;

                dic.Add(key, score);
            }

            var rs = zset.Add(null, dic);
            Assert.Equal(count, rs);
            Assert.Equal(count, zset.Count);

        }

        [Fact]
        public void Add_xx()
        {
            var rkey = "zset_add_xx";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet(_redis, rkey);

            // 插入数据
            zset.Add("stone", 123.456);
            Assert.Equal(1, zset.Count);

            var dic = new Dictionary<String, Double>
            {
                { "newlife", 56.78 },
                { "stone", 33.44 }
            };

            // XX: 仅仅更新存在的成员，不添加新成员。刚好又只返回添加的总数，所以此时总是返回0
            var rs = zset.Add("XX", dic);

            Assert.Equal(0, rs);
            Assert.Equal(1, zset.Count);

            // 取出来
            Assert.Equal(33.44, zset.GetScore("stone"));
        }

        [Fact]
        public void Add_nx()
        {
            var rkey = "zset_add_nx";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet(_redis, rkey);

            // 插入数据
            zset.Add("stone", 123.456);
            Assert.Equal(1, zset.Count);

            var dic = new Dictionary<String, Double>
            {
                { "newlife", 56.78 },
                { "stone", 33.44 }
            };

            var rs = zset.Add("NX", dic);

            Assert.Equal(1, rs);
            Assert.Equal(2, zset.Count);

            // 取出来
            Assert.Equal(123.456, zset.GetScore("stone"));
        }

        [Fact]
        public void Add_ch()
        {
            var rkey = "zset_add_ch";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet(_redis, rkey);

            // 插入数据
            zset.Add("stone", 123.456);
            Assert.Equal(1, zset.Count);

            var dic = new Dictionary<String, Double>
            {
                { "newlife", 56.78 },
                { "stone", 33.44 }
            };

            // 原始返回新添加成员总数
            var rs = zset.Add(null, dic);
            Assert.Equal(1, rs);
            Assert.Equal(2, zset.Count);

            // 清空
            _redis.Remove(rkey);
            zset.Add("stone", 123.456);

            // CH返回值变化总数
            rs = zset.Add("CH", dic);

            Assert.Equal(2, rs);
            Assert.Equal(2, zset.Count);

            // 取出来
            Assert.Equal(33.44, zset.GetScore("stone"));
        }

        [Fact]
        public void Add_incr()
        {
            var rkey = "zset_add_incr";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet(_redis, rkey);

            // 插入数据
            zset.Add("stone", 123.456);
            Assert.Equal(1, zset.Count);

            var dic = new Dictionary<String, Double>
            {
                { "newlife", 56.78 },
                { "stone", 33.44 }
            };

            // 原始返回新添加成员总数，另一个更新
            var rs = zset.Add("INCR", dic);

            Assert.Equal(1, rs);
            Assert.Equal(2, zset.Count);

            // 取出来
            Assert.Equal(123.456 + 33.44, zset.GetScore("stone"));
        }
    }
}