namespace NewLife.Caching;

/// <summary>
/// Redis缓存选项
/// </summary>
public class RedisOptions 
{
    /// <summary>实例名</summary>
    public String? InstanceName { get; set; }

    /// <summary>
    /// 配置字符串。例如：server=127.0.0.1:6379;password=123456;db=3;timeout=3000
    /// </summary>
    public String? Configuration { get; set; }

    /// <summary>服务器</summary>
    public String? Server { get; set; }

    /// <summary>数据库</summary>
    public Int32 Db { get; set; }

    /// <summary>密码</summary>
    public String? Password { get; set; }

    /// <summary>超时时间。单位毫秒</summary>
    public Int32 Timeout { get; set; } = 3000;

    /// <summary>键前缀,只适用于PrefixedRedis</summary>
    public String? Prefix { get; set; }
}