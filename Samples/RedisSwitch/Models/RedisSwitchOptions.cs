namespace RedisSwitch.Models;

/// <summary>Redis交换机配置</summary>
public class RedisSwitchOptions
{
    /// <summary>队列名称</summary>
    public String QueueName { get; set; } = "RedisSwitch";

    /// <summary>消费者数量</summary>
    public Int32 ConsumerCount { get; set; } = 1;

    /// <summary>目标API地址</summary>
    public String TargetApiUrl { get; set; } = "http://localhost:5001/api/target";

    /// <summary>批量处理大小</summary>
    public Int32 BatchSize { get; set; } = 10;

    /// <summary>重试次数</summary>
    public Int32 RetryCount { get; set; } = 3;
}
