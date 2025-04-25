using NewLife.Data;

namespace NewLife.Caching.Queues;

/// <summary>消费者信息</summary>
public class ConsumerInfo
{
    #region 属性
    /// <summary>名称</summary>
    public String? Name { get; set; }

    /// <summary>挂起数</summary>
    public Int32 Pending { get; set; }

    /// <summary>空闲</summary>
    public Int32 Idle { get; set; }
    #endregion

    #region 方法
    /// <summary>分析</summary>
    /// <param name="vs"></param>
    public void Parse(Object[] vs)
    {
        if (vs == null || vs.Length == 0) return;

        for (var i = 0; i < vs.Length - 1; i += 2)
        {
            var key = (vs[i] as IPacket)!.ToStr();
            if (key.IsNullOrEmpty()) continue;

            switch (key)
            {
                case "name": Name = (vs[i + 1] as IPacket)?.ToStr(); break;
                case "pending": Pending = vs[i + 1].ToInt(); break;
                case "idle": Idle = vs[i + 1].ToInt(); break;
            }
        }
    }
    #endregion
}