namespace NewLife.Caching.Clusters;

/// <summary>哨兵节点信息</summary>
public class SentinelInfo
{
    #region 属性
    /// <summary>地址</summary>
    public String IP { get; set; }

    /// <summary>端口</summary>
    public Int32 Port { get; set; }

    /// <summary>节点信息</summary>
    public String EndPoint => IP.IsNullOrEmpty() ? null : $"{IP}:{Port}";

    /// <summary>运行标识</summary>
    public String RunId { get; set; }

    /// <summary>纪元</summary>
    public Int32 Age { get; set; }

    /// <summary>主节点名称</summary>
    public String MasterName { get; set; }

    /// <summary>主节点地址</summary>
    public String MasterIP { get; set; }

    /// <summary>主节点端口</summary>
    public Int32 MasterPort { get; set; }

    /// <summary>节点信息</summary>
    public String MasterEndPoint => MasterIP.IsNullOrEmpty() ? null : $"{MasterIP}:{MasterPort}";

    /// <summary>主节点纪元</summary>
    public Int32 MasterAge { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => !IP.IsNullOrEmpty() ? EndPoint : base.ToString();
    #endregion

    #region 方法
    /// <summary>分析字符串</summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static SentinelInfo Parse(String str)
    {
        if (str.IsNullOrEmpty()) return null;

        // 127.0.0.1,7003,2890784206bf38ba9f9fbf7b61547f8524331c7a,0,mymaster,127.0.0.1,6379,0
        var ss = str.Split(',');

        var inf = new SentinelInfo
        {
            IP = ss[0],
            Port = ss[1].ToInt(),
            RunId = ss[2],
            Age = ss[3].ToInt(),
            MasterName = ss[4],
            MasterIP = ss[5],
            MasterPort = ss[6].ToInt(),
            MasterAge = ss[7].ToInt(),
        };

        return inf;
    }
    #endregion
}