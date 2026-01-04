using NewLife.Data;

namespace NewLife.Caching.Queues;

/// <summary>消费组信息</summary>
public class GroupInfo
{
    #region 属性
    /// <summary>名称</summary>
    public String? Name { get; set; }

    /// <summary>消费者</summary>
    public Int32 Consumers { get; set; }

    /// <summary>挂起数</summary>
    public Int32 Pending { get; set; }

    /// <summary>最后消费Id</summary>
    public String? LastDeliveredId { get; set; }

    /// <summary>最后消费时间</summary>
    public DateTime LastDelivered { get; private set; }
    #endregion

    #region 方法
    /// <summary>分析</summary>
    /// <param name="vs"></param>
    public void Parse(Object[] vs)
    {
        for (var i = 0; i < vs.Length - 1; i += 2)
        {
            var key = (vs[i] as IPacket)!.ToStr();
            if (key.IsNullOrEmpty()) continue;

            var value = vs[i + 1];
            switch (key)
            {
                case "name": Name = (value as IPacket)?.ToStr(); break;
                case "consumers": Consumers = value.ToInt(); break;
                case "pending": Pending = value.ToInt(); break;
                case "last-delivered-id": LastDeliveredId = (value as IPacket)?.ToStr(); break;
            }
        }

        var last = LastDeliveredId;
        if (!last.IsNullOrEmpty())
        {
            var p = last.IndexOf('-');
            if (p > 0) last = last.Substring(0, p);

            LastDelivered = last.ToLong().ToDateTime().ToLocalTime();
        }
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String? ToString() => Name;
    #endregion
}