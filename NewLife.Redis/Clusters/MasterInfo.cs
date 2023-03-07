namespace NewLife.Caching.Clusters;

/// <summary>主节点信息</summary>
public class MasterInfo
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>状态。ok</summary>
    public String Status { get; set; }

    /// <summary>地址</summary>
    public String IP { get; set; }

    /// <summary>端口</summary>
    public Int32 Port { get; set; }

    /// <summary>节点信息</summary>
    public String EndPoint => IP.IsNullOrEmpty() ? null : $"{IP}:{Port}";

    /// <summary>从机数</summary>
    public Int32 Slaves { get; set; }

    /// <summary>哨兵数</summary>
    public Int32 Sentinels { get; set; }
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
    public static MasterInfo Parse(String str)
    {
        if (str.IsNullOrEmpty()) return null;

        var dic = str.SplitAsDictionary("=", ",");

        var inf = new MasterInfo();
        if (dic.TryGetValue("name", out var v)) inf.Name = v;
        if (dic.TryGetValue("status", out v)) inf.Status = v;
        if (dic.TryGetValue("address", out v))
        {
            var ss = v.Split(':');
            inf.IP = ss[0];
            if (ss.Length > 1) inf.Port = ss[1].ToInt();
        }
        if (dic.TryGetValue("slaves", out v)) inf.Slaves = v.ToInt();
        if (dic.TryGetValue("sentinels", out v)) inf.Sentinels = v.ToInt();

        return inf;
    }
    #endregion
}