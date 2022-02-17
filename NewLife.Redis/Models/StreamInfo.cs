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
                var value = vs[i + 1];
                switch (key)
                {
                    case "length": Length = value.ToInt(); break;
                    case "radix-tree-keys": RadixTreeKeys = value.ToInt(); break;
                    case "radix-tree-nodes": RadixTreeNodes = value.ToInt(); break;
                    case "last-generated-id": LastGeneratedId = (value as Packet)?.ToStr(); break;
                    case "groups": Groups = value.ToInt(); break;
                    case "first-entry":
                        if (value is Object[] fs)
                        {
                            if (fs[0] is Packet pk) FirstId = pk.ToStr();
                            if (fs[1] is Object[] objs) FirstValues = objs.Select(e => (e as Packet)?.ToStr()).ToArray();
                        }
                        break;
                    case "last-entry":
                        if (value is Object[] ls)
                        {
                            if (ls[0] is Packet pk) LastId = pk.ToStr();
                            if (ls[1] is Object[] objs) LastValues = objs.Select(e => (e as Packet)?.ToStr()).ToArray();
                        }
                        break;
                }
            }
        }
        #endregion
    }
}