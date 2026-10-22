using System.ComponentModel;

namespace NewLife.Caching;

/// <summary>
/// Redis缓存选项
/// </summary>
[DisplayName("Redis settings")]
public class RedisOptions 
{
    /// <summary>实例名</summary>
    [Description("Instance name.")]
    public String? InstanceName { get; set; }

    /// <summary>
    /// 配置字符串。例如：server=127.0.0.1:6379;password=123456;db=3;timeout=3000
    /// </summary>
    [Description("Connection string, for example server=127.0.0.1:6379;password=123456;db=3;timeout=3000.")]
    public String? Configuration { get; set; }

    /// <summary>服务器</summary>
    [Description("Server address.")]
    public String? Server { get; set; }

    /// <summary>数据库</summary>
    [Description("Database index.")]
    public Int32 Db { get; set; }

    /// <summary>用户名。Redis6.0支持</summary>
    [Description("User name. Requires Redis 6.0 or later.")]
    public String? UserName { get; set; }

    /// <summary>密码</summary>
    [Description("Password.")]
    public String? Password { get; set; }

    /// <summary>超时时间。单位毫秒</summary>
    [Description("Timeout in milliseconds. Default 3000.")]
    public Int32 Timeout { get; set; } = 3000;

    /// <summary>键前缀</summary>
    [Description("Key prefix.")]
    public String? Prefix { get; set; }

    /// <summary>协议版本。默认0表示自动（RESP2），设为3启用RESP3协议（需Redis 6.0+支持）</summary>
    [Description("Protocol version. 0 means auto (RESP2); 3 enables RESP3, which requires Redis 6.0 or later.")]
    public Int32 ProtocolVersion { get; set; }
}