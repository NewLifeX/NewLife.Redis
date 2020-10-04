using System;
using NewLife.Data;

namespace NewLife.Caching.Models
{
    /// <summary>等待项</summary>
    public class PendingItem
    {
        #region 属性
        /// <summary>消息Id</summary>
        public String Id { get; set; }

        /// <summary>消费者</summary>
        public String Consumer { get; set; }

        /// <summary>空闲时间。从读取到现在经历过的毫秒数</summary>
        public Int32 Idle { get; set; }

        /// <summary>传递次数</summary>
        public Int32 Delivery { get; set; }
        #endregion

        #region 方法
        /// <summary>分析</summary>
        /// <param name="vs"></param>
        public void Parse(Object[] vs)
        {
            if (vs == null || vs.Length < 4) return;

            Id = (vs[0] as Packet).ToStr();
            Consumer = (vs[1] as Packet).ToStr();
            Idle = vs[2].ToInt();
            Delivery = vs[3].ToInt();
        }
        #endregion
    }
}