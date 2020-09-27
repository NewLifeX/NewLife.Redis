using System;

namespace NewLife.Caching.Models
{
    /// <summary>模糊搜索模型</summary>
    public class SearchModel
    {
        /// <summary>匹配表达式</summary>
        public String Pattern { get; set; }

        /// <summary>个数</summary>
        public Int32 Count { get; set; }

        /// <summary>开始位置，同时由内部填充返回</summary>
        public Int32 Position { get; set; }
    }
}