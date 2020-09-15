using System;
using NewLife.Data;

namespace NewLife.Caching.Models
{
    /// <summary>消费组信息</summary>
    public class GroupInfo
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>消费者</summary>
        public Int32 Consumers { get; set; }

        /// <summary>挂起数</summary>
        public Int32 Pending { get; set; }
        #endregion

        #region 方法
        /// <summary>分析</summary>
        /// <param name="vs"></param>
        public void Parse(Object[] vs)
        {
            for (var i = 0; i < vs.Length - 1; i += 2)
            {
                var key = (vs[i] as Packet).ToStr();
                switch (key)
                {
                    case "name": Name = (vs[i + 1] as Packet)?.ToStr(); break;
                    case "consumers": Consumers = vs[i + 1].ToInt(); break;
                    case "pending": Pending = vs[i + 1].ToInt(); break;
                }
            }
        }
        #endregion
    }
}