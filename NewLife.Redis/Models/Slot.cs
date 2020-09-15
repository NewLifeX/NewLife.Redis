using System;

namespace NewLife.Caching.Models
{
    /// <summary>数据槽区间</summary>
    public struct Slot
    {
        /// <summary>起始</summary>
        public Int32 From;

        /// <summary>结束</summary>
        public Int32 To;

        /// <summary>已重载。返回区间</summary>
        /// <returns></returns>
        public override String ToString() => From == To ? From + "" : $"{From}-{To}";
    }
}