using System;
using System.Collections.Generic;
using NewLife.Caching;
using NewLife.Caching.Models;
using Xunit;

namespace XUnitTest
{
    public class GeoTests
    {
        private readonly FullRedis _redis;

        public GeoTests()
        {
            var rds = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            rds.Log = NewLife.Log.XTrace.Log;
#endif
            _redis = rds;
        }

        [Fact]
        public void Test()
        {
            var key = "geo";

            // 删除已有
            _redis.Remove(key);

            var geo = new RedisGeo(_redis, key);

            // 普通添加
            geo.Add("Stone", 12.12, 34.34);

            // 批量添加
            var list = new List<GeoInfo>();
            for (var i = 0; i < 10; i++)
            {
                list.Add(new GeoInfo { Name = "Test" + i, Longitude = i + 0.33, Latitude = i + 0.44 });
            }
            geo.Add(list.ToArray());

            // 计算两点距离
            var d = geo.GetDistance("Test3", "Test5");
            Assert.Equal(314115.9636, d);

            // 获取多点坐标
            var gis = geo.GetPosition("Test2", "Test4", "Test7");
            Assert.Equal(3, gis.Length);

            Assert.Equal(233, (Int32)Math.Round(gis[0].Longitude * 100));
            Assert.Equal(244, (Int32)Math.Round(gis[0].Latitude * 100));

            Assert.Equal(433, (Int32)Math.Round(gis[1].Longitude * 100));
            Assert.Equal(444, (Int32)Math.Round(gis[1].Latitude * 100));

            Assert.Equal(733, (Int32)Math.Round(gis[2].Longitude * 100));
            Assert.Equal(744, (Int32)Math.Round(gis[2].Latitude * 100));
        }

        [Fact]
        public void GetDistance()
        {
            var key = "geo_distance";

            // 删除已有
            _redis.Remove(key);

            var geo = new RedisGeo(_redis, key);

            // 普通添加
            geo.Add("北京", 116.404125, 39.900545);
            geo.Add("上海", 121.48789949, 31.24916171);

            // 计算两点距离
            var d = geo.GetDistance("北京", "上海");
            Assert.Equal(1066019.9908, d);
        }

        [Fact]
        public void GetHash()
        {
            var key = "geo_hash";

            // 删除已有
            _redis.Remove(key);

            var geo = new RedisGeo(_redis, key);

            // 普通添加
            geo.Add("北京", 116.404125, 39.900545);
            geo.Add("上海", 121.48789949, 31.24916171);

            // 计算两点距离
            var hs = geo.GetHash("北京", "上海");
            Assert.Equal(2, hs.Length);
            Assert.Equal("wx4fbzdvs80", hs[0]);
            Assert.Equal("wtw3u88z910", hs[1]);
        }

        [Fact]
        public void GetRadiusByXY()
        {
            var key = "geo_radius_xy";

            // 删除已有
            _redis.Remove(key);

            var geo = new RedisGeo(_redis, key);

            // 批量添加
            var list = new List<GeoInfo>();
            for (var i = 0; i < 10; i++)
            {
                list.Add(new GeoInfo { Name = "Test" + i, Longitude = i + 0.33, Latitude = i + 0.44 });
            }
            geo.Add(list.ToArray());

            // 按半径查找，指定坐标20000m以内的点
            var gis = geo.GetRadius(3.31, 3.45, 200000);
            Assert.Equal(3, gis.Length);

            Assert.Equal("Test3", gis[0].Name);
            Assert.Equal("Test4", gis[1].Name);
            Assert.Equal("Test2", gis[2].Name);

            Assert.Equal(2483.2078, gis[0].Distance);
            Assert.Equal(157907.9298, gis[1].Distance);
            Assert.Equal(156427.8334, gis[2].Distance);

            // 按半径查找，指定坐标200km以内的2个点
            gis = geo.GetRadius(3.31, 3.45, 200, "km", 2);
            Assert.Equal(2, gis.Length);

            Assert.Equal("Test3", gis[0].Name);
            Assert.Equal("Test2", gis[1].Name);

            Assert.Equal(2.4832, gis[0].Distance);
            Assert.Equal(156.4278, gis[1].Distance);
        }

        [Fact]
        public void GetRadiusByMember()
        {
            var key = "geo_radius_member";

            // 删除已有
            _redis.Remove(key);

            var geo = new RedisGeo(_redis, key);

            // 批量添加
            var list = new List<GeoInfo>();
            for (var i = 0; i < 10; i++)
            {
                list.Add(new GeoInfo { Name = "Test" + i, Longitude = i + 0.33, Latitude = i + 0.44 });
            }
            geo.Add(list.ToArray());

            // 按半径查找，指定点半径20000m以内的点
            var gis = geo.GetRadius("Test3", 200000);
            Assert.Equal(3, gis.Length);

            Assert.Equal("Test3", gis[0].Name);
            Assert.Equal("Test4", gis[1].Name);
            Assert.Equal("Test2", gis[2].Name);

            Assert.Equal(0, gis[0].Distance);
            Assert.Equal(157111.0190, gis[1].Distance);
            Assert.Equal(157193.0930, gis[2].Distance);

            // 按半径查找，指定坐标200km以内的2个点
            gis = geo.GetRadius("Test3", 200, "km", 2);
            Assert.Equal(2, gis.Length);

            Assert.Equal("Test3", gis[0].Name);
            Assert.Equal("Test4", gis[1].Name);

            Assert.Equal(0, gis[0].Distance);
            Assert.Equal(157.1110, gis[1].Distance);
        }
    }
}