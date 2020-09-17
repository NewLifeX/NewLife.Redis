using System;

namespace NewLife.Caching.Models
{
    /// <summary>地理坐标</summary>
    public class GeoInfo
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>经度</summary>
        public Double Longitude { get; set; }

        /// <summary>纬度</summary>
        public Double Latitude { get; set; }

        /// <summary>距离</summary>
        public Double Distance { get; set; }
        #endregion
    }
}