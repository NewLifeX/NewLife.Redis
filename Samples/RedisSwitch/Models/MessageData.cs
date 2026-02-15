namespace RedisSwitch.Models;

/// <summary>消息数据</summary>
public class MessageData
{
    /// <summary>消息ID</summary>
    public String? Id { get; set; }

    /// <summary>消息内容</summary>
    public String? Content { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>附加数据</summary>
    public Dictionary<String, Object>? Properties { get; set; }
}
