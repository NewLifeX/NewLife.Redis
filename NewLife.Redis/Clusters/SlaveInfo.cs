namespace NewLife.Caching.Clusters;

/// <summary>从节点信息</summary>
public class SlaveInfo
{
    #region 属性
    /// <summary>地址</summary>
    public String IP { get; set; }

    /// <summary>端口</summary>
    public Int32 Port { get; set; }

    /// <summary>节点信息</summary>
    public String EndPoint => IP.IsNullOrEmpty() ? null : $"{IP}:{Port}";

    /// <summary>状态。online/offline</summary>
    public String State { get; set; }

    /// <summary>偏移量</summary>
    public Int32 Offset { get; set; }

    /// <summary>延迟</summary>
    public Int32 Lag { get; set; }
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
    public static SlaveInfo Parse(String str)
    {
        if (str.IsNullOrEmpty()) return null;

        var dic = str.SplitAsDictionary("=", ",");
        //return JsonHelper.Convert<SlaveInfo>(dic);

        var inf = new SlaveInfo();
        if (dic.TryGetValue("ip", out var v)) inf.IP = v;
        if (dic.TryGetValue("port", out v)) inf.Port = v.ToInt();
        if (dic.TryGetValue("state", out v)) inf.State = v;
        if (dic.TryGetValue("offset", out v)) inf.Offset = v.ToInt();
        if (dic.TryGetValue("lag", out v)) inf.Lag = v.ToInt();

        return inf;
    }
    #endregion
}