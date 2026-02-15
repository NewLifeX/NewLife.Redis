namespace RedisSwitch.Models;

/// <summary>发布消息请求</summary>
public class PublishRequest
{
    /// <summary>消息内容</summary>
    public String? Content { get; set; }

    /// <summary>附加数据</summary>
    public Dictionary<String, Object>? Properties { get; set; }
}
