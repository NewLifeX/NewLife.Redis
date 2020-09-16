using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife;
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
            //_redis = new FullRedis("127.0.0.1:6379", null, 2);

            var config = "";
            var file = @"config\redis.config";
            if (File.Exists(file)) config = File.ReadAllText(file.GetFullPath())?.Trim();
            if (config.IsNullOrEmpty()) config = "server=127.0.0.1:6379;db=3";
            if (!File.Exists(file)) File.WriteAllText(file.GetFullPath(), config);

            _redis = new FullRedis();
            _redis.Init(config);
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

            var zset = new RedisSortedSet<String>(_redis, rkey);
            var list = new SortedList<Double, String>();

            // 插入数据
            for (var i = 0; i < 17; i++)
            {
                var key = Rand.NextString(8);
                var score = Rand.Next() / 1000d;

                list.Add(score, key);
                var rs = zset.Add(key, score);
                Assert.Equal(1, rs);
            }

            Assert.Equal(list.Count, zset.Count);

            var ks = list.Keys.ToList();
            var vs = list.Values.ToList();

            // 删除
            {
                list.Remove(ks[0]);

                var rs = zset.Remove(vs[0]);
                Assert.Equal(1, rs);

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

            var zset = new RedisSortedSet<String>(_redis, rkey);

            // 插入数据
            var count = 10;
            var list = new List<String>();
            for (var i = 0; i < count; i++)
            {
                list.Add(Rand.NextString(8));
            }

            var score = Rand.Next() / 1000d;
            var rs = zset.Add(list, score);

            Assert.Equal(count, rs);
            Assert.Equal(count, zset.Count);
        }

        [Fact]
        public void Adds_null()
        {
            var rkey = "zset_adds_null";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet<String>(_redis, rkey);

            // 插入数据
            var count = 10;
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

            var zset = new RedisSortedSet<String>(_redis, rkey);

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

            var zset = new RedisSortedSet<String>(_redis, rkey);

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

            var zset = new RedisSortedSet<String>(_redis, rkey);

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

            var zset = new RedisSortedSet<String>(_redis, rkey);

            // 插入数据
            zset.Add("stone", 123.456);
            Assert.Equal(1, zset.Count);

            var dic = new Dictionary<String, Double>
            {
                //{ "newlife", 56.78 },
                { "stone", 33.44 }
            };

            // 原始返回新添加成员总数，另一个更新
            var rs = zset.Add("INCR", dic);

            //Assert.Equal(1, rs);
            //Assert.Equal(2, zset.Count);

            // 取出来
            Assert.Equal(123.456 + 33.44, zset.GetScore("stone"));
        }

        [Fact]
        public void Range_Test()
        {
            var rkey = "zset_Range";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet<String>(_redis, rkey);

            // 插入数据
            zset.Add("stone1", 12.34);
            zset.Add("stone2", 13.56);
            zset.Add("stone3", 14.34);
            zset.Add("stone4", 15.34);
            Assert.Equal(4, zset.Count);

            var count = zset.FindCount(13.56, 14.34);
            Assert.Equal(2, count);

            var rs = zset.Range(1, 2);
            Assert.Equal(2, rs.Length);
            Assert.Equal("stone2", rs[0]);
            Assert.Equal("stone3", rs[1]);

            var rs2 = zset.RangeWithScores(1, 2);
            Assert.Equal(2, rs2.Count);
            var kv2 = rs2.FirstOrDefault();
            Assert.Equal("stone2", kv2.Key);
            Assert.Equal(13.56, kv2.Value);
        }

        [Fact]
        public void RangeByScore_Test()
        {
            var rkey = "zset_RangeByScore";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet<String>(_redis, rkey);

            // 插入数据
            zset.Add("stone1", 12.34);
            zset.Add("stone2", 13.56);
            zset.Add("stone3", 14.34);
            zset.Add("stone4", 15.34);
            Assert.Equal(4, zset.Count);

            var count = zset.FindCount(13.56, 14.34);
            Assert.Equal(2, count);

            var rs = zset.RangeByScore(13.56, 14.34, 1, 2);
            Assert.Equal(1, rs.Length);
            Assert.Equal("stone3", rs[0]);

            var rs2 = zset.RangeByScoreWithScores(13.56, 14.34, 1, 2);
            Assert.Equal(1, rs2.Count);
            var kv2 = rs2.FirstOrDefault();
            Assert.Equal("stone3", kv2.Key);
            Assert.Equal(14.34, kv2.Value);
        }

        [Fact]
        public void Incr_Test()
        {
            var rkey = "zset_zincr";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet<String>(_redis, rkey);

            // 插入数据
            zset.Add("stone", 12.34);
            var old = zset.Increment("stone", 13.56);
            Assert.Equal(1, zset.Count);
            Assert.Equal(12.34 + 13.56, old);
            Assert.Equal(12.34 + 13.56, zset.GetScore("stone"));
        }

        [Fact]
        public void PopMaxMin_Test()
        {
            var rkey = "zset_pop";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet<String>(_redis, rkey);

            // 插入数据
            zset.Add("stone1", 12.34);
            zset.Add("stone2", 13.56);
            zset.Add("stone3", 14.34);
            zset.Add("stone4", 15.34);
            Assert.Equal(4, zset.Count);

            var max = zset.PopMax(1);
            Assert.Equal(3, zset.Count);
            Assert.Equal("stone4", max.Keys.First());
            Assert.Equal(15.34, max.Values.First());

            var min = zset.PopMin(2);
            Assert.Equal(1, zset.Count);

            var ks = min.Keys.ToArray();
            var vs = min.Values.ToArray();
            Assert.Equal("stone1", ks[0]);
            Assert.Equal(12.34, vs[0]);
            Assert.Equal("stone2", ks[1]);
            Assert.Equal(13.56, vs[1]);
        }

        [Fact]
        public void Search()
        {
            var rkey = "zset_Search";

            // 删除已有
            _redis.Remove(rkey);

            var zset = new RedisSortedSet<String>(_redis, rkey);

            // 插入数据
            zset.Add("stone1", 12.34);
            zset.Add("stone2", 13.56);
            zset.Add("stone3", 14.34);
            zset.Add("stone4", 15.34);
            Assert.Equal(4, zset.Count);

            var dic = zset.Search("*one?", 3, 2).ToList();
            Assert.Equal(3, dic.Count);
            //Assert.Equal("stone3", dic[0]);
            //Assert.Equal("stone4", dic[1]);
        }
    }
}