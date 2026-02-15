using NewLife.Caching;
using NewLife.Log;
using RedisSwitch.Models;
using System.Text;
using System.Text.Json;

namespace RedisSwitch.Services;

/// <summary>消息消费后台服务</summary>
public class MessageConsumerService : BackgroundService
{
    private readonly FullRedis _redis;
    private readonly RedisSwitchOptions _options;
    private readonly ILogger<MessageConsumerService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>构造函数</summary>
    /// <param name="redis">Redis实例</param>
    /// <param name="options">配置选项</param>
    /// <param name="logger">日志</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    public MessageConsumerService(
        FullRedis redis,
        RedisSwitchOptions options,
        ILogger<MessageConsumerService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _redis = redis;
        _options = options;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>执行后台任务</summary>
    /// <param name="stoppingToken">停止令牌</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageConsumerService is starting.");

        // 创建多个消费者任务
        var tasks = new List<Task>();
        for (var i = 0; i < _options.ConsumerCount; i++)
        {
            var consumerId = i;
            tasks.Add(ConsumeAsync(consumerId, stoppingToken));
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("MessageConsumerService is stopping.");
    }

    /// <summary>消费消息</summary>
    /// <param name="consumerId">消费者ID</param>
    /// <param name="stoppingToken">停止令牌</param>
    private async Task ConsumeAsync(Int32 consumerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consumer {ConsumerId} started", consumerId);

        var queue = _redis.GetQueue<MessageData>(_options.QueueName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 从队列获取消息
                    var message = await queue.TakeOneAsync(10, stoppingToken);
                    if (message != null)
                    {
                        _logger.LogInformation("Consumer {ConsumerId} received message: {MessageId}", consumerId, message.Id);

                        // 转发消息到目标API
                        await ForwardMessageAsync(message, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consumer {ConsumerId} error processing message", consumerId);
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }

        _logger.LogInformation("Consumer {ConsumerId} stopped", consumerId);
    }

    /// <summary>转发消息到目标API</summary>
    /// <param name="message">消息数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ForwardMessageAsync(MessageData message, CancellationToken cancellationToken)
    {
        var attempt = 0;
        var maxAttempts = _options.RetryCount + 1; // 初始尝试 + 重试次数
        var success = false;

        while (!success && attempt < maxAttempts)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(_options.TargetApiUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    success = true;
                    _logger.LogInformation("Message forwarded successfully: {MessageId}", message.Id);
                }
                else
                {
                    attempt++;
                    _logger.LogWarning("Failed to forward message {MessageId}, StatusCode: {StatusCode}, Attempt: {Attempt}/{MaxAttempts}", 
                        message.Id, response.StatusCode, attempt, maxAttempts);
                    if (attempt < maxAttempts)
                        await Task.Delay(1000 * attempt, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                attempt++;
                _logger.LogError(ex, "Error forwarding message {MessageId}, Attempt: {Attempt}/{MaxAttempts}", 
                    message.Id, attempt, maxAttempts);
                if (attempt < maxAttempts)
                    await Task.Delay(1000 * attempt, cancellationToken);
            }
        }

        if (!success)
        {
            _logger.LogError("Failed to forward message after {MaxAttempts} attempts: {MessageId}", maxAttempts, message.Id);
        }
    }
}
