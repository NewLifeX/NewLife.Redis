using System;
using System.Collections.Generic;
using NewLife.Caching.Models;
using NewLife.Data;

namespace NewLife.Caching
{
    /// <summary>地理信息数据</summary>
    /// <remarks>
    /// 将指定的地理空间位置（纬度、经度、名称）添加到指定的key中。
    /// 这些数据将会存储到sorted set这样的目的是为了方便使用GEORADIUS或者GEORADIUSBYMEMBER命令对数据进行半径查询等操作。
    /// 该命令以采用标准格式的参数x,y,所以经度必须在纬度之前。这些坐标的限制是可以被编入索引的，区域面积可以很接近极点但是不能索引。
    /// </remarks>
    public class RedisGeo : RedisBase
    {
        #region 实例化
        /// <summary>实例化地理信息</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisGeo(Redis redis, String key) : base(redis, key) { }
        #endregion

        /// <summary>添加</summary>
        /// <param name="name"></param>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        public Int32 Add(String name, Double longitude, Double latitude) => Execute(rc => rc.Execute<Int32>("GEOADD", Key, longitude, latitude, name), true);

        /// <summary>添加</summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public Int32 Add(params GeoInfo[] items)
        {
            var args = new List<Object> { Key };
            foreach (var item in items)
            {
                args.Add(item.Longitude);
                args.Add(item.Latitude);
                args.Add(item.Name);
            }
            return Execute(rc => rc.Execute<Int32>("GEOADD", args.ToArray()), true);
        }

        /// <summary>两点距离</summary>
        /// <param name="from">开始点</param>
        /// <param name="to">结束点</param>
        /// <param name="unit">单位。m/km/mi/ft</param>
        /// <returns></returns>
        public Double GetDistance(String from, String to, String unit = null)
        {
            return unit.IsNullOrEmpty() ?
                Execute(rc => rc.Execute<Double>("GEODIST", Key, from, to), false) :
                Execute(rc => rc.Execute<Double>("GEODIST", Key, from, to, unit), false);
        }

        /// <summary>获取一批点的坐标</summary>
        /// <param name="members"></param>
        /// <returns></returns>
        public GeoInfo[] GetPosition(params String[] members)
        {
            var args = new List<Object> { Key };
            foreach (var item in members)
            {
                args.Add(item);
            }

            var rs = Execute(rc => rc.Execute<Object[]>("GEOPOS", args.ToArray()), false);
            if (rs == null || rs.Length == 0) return null;

            var list = new List<GeoInfo>();
            for (var i = 0; i < rs.Length; i++)
            {
                var inf = new GeoInfo { Name = members[i] };
                if (rs[i] is Object[] vs)
                {
                    inf.Longitude = (vs[0] as Packet).ToStr().ToDouble();
                    inf.Latitude = (vs[1] as Packet).ToStr().ToDouble();
                }

                list.Add(inf);
            }

            return list.ToArray();
        }

        /// <summary>获取一批点的GeoHash一维编码</summary>
        /// <remarks>
        /// 一维编码表示一个矩形区域，前缀表示更大区域，例如北京wx4fbzdvs80包含在wx4fbzdvs里面。
        /// 这个特性可以用于附近地点搜索。
        /// GeoHash编码位数及距离关系：
        /// 1位，+-2500km；
        /// 2位，+-630km；
        /// 3位，+-78km；
        /// 4位，+-20km；
        /// 5位，+-2.4km；
        /// 6位，+-610m；
        /// 7位，+-76m；
        /// 8位，+-19m；
        /// </remarks>
        /// <param name="members"></param>
        /// <returns></returns>
        public String[] GetHash(params String[] members)
        {
            var args = new List<Object> { Key };
            foreach (var item in members)
            {
                args.Add(item);
            }

            return Execute(rc => rc.Execute<String[]>("GEOHASH", args.ToArray()), false);
        }

        /// <summary>以给定的经纬度为中心， 返回键包含的位置元素当中， 与中心的距离不超过给定最大距离的所有位置元素</summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <param name="radius"></param>
        /// <param name="unit"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public GeoInfo[] GetRadius(Double longitude, Double latitude, Double radius, String unit = null, Int32 count = 0)
        {
            if (unit.IsNullOrEmpty()) unit = "m";

            var rs = count > 0 ?
               Execute(rc => rc.Execute<Object[]>("GEORADIUS", Key, longitude, latitude, radius, unit, "WITHDIST", "WITHCOORD", "COUNT", count), false) :
               Execute(rc => rc.Execute<Object[]>("GEORADIUS", Key, longitude, latitude, radius, unit, "WITHDIST", "WITHCOORD"), false);
            if (rs == null || rs.Length == 0) return null;

            var list = new List<GeoInfo>();
            for (var i = 0; i < rs.Length; i++)
            {
                var inf = new GeoInfo();
                if (rs[i] is Object[] vs)
                {
                    inf.Name = (vs[0] as Packet).ToStr();
                    inf.Distance = (vs[1] as Packet).ToStr().ToDouble();
                    if (vs[2] is Object[] vs2)
                    {
                        inf.Longitude = (vs2[0] as Packet).ToStr().ToDouble();
                        inf.Latitude = (vs2[1] as Packet).ToStr().ToDouble();
                    }
                }

                list.Add(inf);
            }

            return list.ToArray();
        }

        /// <summary>以给定的点位为中心， 返回键包含的位置元素当中， 与中心的距离不超过给定最大距离的所有位置元素</summary>
        /// <param name="member"></param>
        /// <param name="radius"></param>
        /// <param name="unit"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public GeoInfo[] GetRadius(String member, Double radius, String unit = null, Int32 count = 0)
        {
            if (unit.IsNullOrEmpty()) unit = "m";

            var rs = count > 0 ?
                Execute(rc => rc.Execute<Object[]>("GEORADIUSBYMEMBER", Key, member, radius, unit, "WITHDIST", "WITHCOORD", "COUNT", count), false) :
                Execute(rc => rc.Execute<Object[]>("GEORADIUSBYMEMBER", Key, member, radius, unit, "WITHDIST", "WITHCOORD"), false);
            if (rs == null || rs.Length == 0) return null;

            var list = new List<GeoInfo>();
            for (var i = 0; i < rs.Length; i++)
            {
                var inf = new GeoInfo();
                if (rs[i] is Object[] vs)
                {
                    inf.Name = (vs[0] as Packet).ToStr();
                    inf.Distance = (vs[1] as Packet).ToStr().ToDouble();
                    if (vs[2] is Object[] vs2)
                    {
                        inf.Longitude = (vs2[0] as Packet).ToStr().ToDouble();
                        inf.Latitude = (vs2[1] as Packet).ToStr().ToDouble();
                    }
                }

                list.Add(inf);
            }

            return list.ToArray();
        }
    }
}