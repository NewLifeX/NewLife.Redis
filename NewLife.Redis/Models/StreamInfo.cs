using System;
using System.Linq;
using NewLife.Data;

namespace NewLife.Caching.Models
{
    /// <summary>消息流信息</summary>
    public class StreamInfo
    {
        #region 属性
        /// <summary>长度</summary>
        public Int32 Length { get; set; }

        /// <summary>基数树</summary>
        public Int32 RadixTreeKeys { get; set; }

        /// <summary>基数树节点数</summary>
        public Int32 RadixTreeNodes { get; set; }

        /// <summary>消费组</summary>
        public Int32 Groups { get; set; }

        /// <summary>最后生成Id</summary>
        public String LastGeneratedId { get; set; }

        /// <summary>第一个Id</summary>
        public String FirstId { get; set; }

        /// <summary>第一个消息</summary>
        public String[] FirstValues { get; set; }

        /// <summary>最后一个Id</summary>
        public String LastId { get; set; }

        /// <summary>最后一个消息</summary>
        public String[] LastValues { get; set; }
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
                    case "length": Length = vs[i + 1].ToInt(); break;
                    case "radix-tree-keys": RadixTreeKeys = vs[i + 1].ToInt(); break;
                    case "radix-tree-nodes": RadixTreeNodes = vs[i + 1].ToInt(); break;
                    case "last-generated-id": LastGeneratedId = (vs[i + 1] as Packet)?.ToStr(); break;
                    case "groups": Groups = vs[i + 1].ToInt(); break;
                    case "first-entry":
                        if (vs[i + 1] is Object[] fs)
                        {
                            FirstId = (fs[0] as Packet).ToStr();
                            FirstValues = (fs[1] as Object[]).Select(e => (e as Packet).ToStr()).ToArray();
                        }
                        break;
                    case "last-entry":
                        if (vs[i + 1] is Object[] ls)
                        {
                            LastId = (ls[0] as Packet).ToStr();
                            LastValues = (ls[1] as Object[]).Select(e => (e as Packet).ToStr()).ToArray();
                        }
                        break;
                }
            }
        }
        #endregion
    }
}