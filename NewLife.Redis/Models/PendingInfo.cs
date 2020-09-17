using System;
using System.Collections.Generic;
using NewLife.Data;

namespace NewLife.Caching.Models
{
    /// <summary>等待信息</summary>
    public class PendingInfo
    {
        #region 属性
        /// <summary>个数</summary>
        public Int32 Count { get; set; }

        /// <summary>开始Id</summary>
        public String StartId { get; set; }

        /// <summary>结束Id</summary>
        public String EndId { get; set; }

        /// <summary>消费者挂起情况</summary>
        public IDictionary<String, Int32> Consumers { get; set; }
        #endregion

        #region 方法
        /// <summary>分析</summary>
        /// <param name="vs"></param>
        public void Parse(Object[] vs)
        {
            if (vs == null || vs.Length < 3) return;

            Count = vs[0].ToInt();
            StartId = (vs[1] as Packet).ToStr();
            EndId = (vs[2] as Packet).ToStr();

            var dic = new Dictionary<String, Int32>();
            if (vs.Length >= 4 && vs[3] is Object[] vs2)
            {
                foreach (Object[] vs3 in vs2)
                {
                    if (vs3 != null && vs3.Length == 2)
                    {
                        var consumer = (vs3[0] as Packet).ToStr();
                        var count = (vs3[1] as Packet).ToStr().ToInt();
                        dic[consumer] = count;
                    }
                }
            }
            Consumers = dic;
        }
        #endregion
    }
}