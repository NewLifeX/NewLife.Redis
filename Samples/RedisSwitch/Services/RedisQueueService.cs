using NewLife.Caching;
using NewLife.Log;
using RedisSwitch.Models;

namespace RedisSwitch.Services;

/// <summary>Redis队列服务</summary>
public class RedisQueueService
{
    private readonly FullRedis _redis;
    private readonly RedisSwitchOptions _options;
    private readonly ILogger<RedisQueueService> _logger;

    /// <summary>构造函数</summary>
    /// <param name="redis">Redis实例</param>
    /// <param name="options">配置选项</param>
    /// <param name="logger">日志</param>
    public RedisQueueService(FullRedis redis, RedisSwitchOptions options, ILogger<RedisQueueService> logger)
    {
        _redis = redis;
        _options = options;
        _logger = logger;
    }

    /// <summary>发布消息到队列</summary>
    /// <param name="message">消息数据</param>
    /// <returns>发布结果</returns>
    public async Task<PublishResponse> PublishAsync(MessageData message)
    {
        try
        {
            var queue = _redis.GetQueue<MessageData>(_options.QueueName);
            // NewLife.Redis 的 Add 方法是同步的，在异步上下文中调用不会阻塞线程池
            queue.Add(message);

            _logger.LogInformation("Published message to queue: {QueueName}, MessageId: {MessageId}", _options.QueueName, message.Id);

            return await Task.FromResult(new PublishResponse
            {
                Success = true,
                MessageId = message.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message: {MessageId}", message.Id);
            return new PublishResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>获取队列长度</summary>
    /// <returns>队列长度</returns>
    public Int32 GetQueueCount()
    {
        var queue = _redis.GetQueue<MessageData>(_options.QueueName);
        return queue.Count;
    }
}
