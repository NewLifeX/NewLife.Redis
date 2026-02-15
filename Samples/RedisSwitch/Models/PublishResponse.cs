namespace RedisSwitch.Models;

/// <summary>发布消息响应</summary>
public class PublishResponse
{
    /// <summary>是否成功</summary>
    public Boolean Success { get; set; }

    /// <summary>消息ID</summary>
    public String? MessageId { get; set; }

    /// <summary>错误信息</summary>
    public String? ErrorMessage { get; set; }
}
