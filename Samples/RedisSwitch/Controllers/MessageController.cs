using Microsoft.AspNetCore.Mvc;
using RedisSwitch.Models;
using RedisSwitch.Services;

namespace RedisSwitch.Controllers;

/// <summary>消息发布控制器</summary>
[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly RedisQueueService _queueService;
    private readonly ILogger<MessageController> _logger;

    /// <summary>构造函数</summary>
    /// <param name="queueService">队列服务</param>
    /// <param name="logger">日志</param>
    public MessageController(RedisQueueService queueService, ILogger<MessageController> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    /// <summary>发布消息</summary>
    /// <param name="request">发布请求</param>
    /// <returns>发布响应</returns>
    [HttpPost("publish")]
    public async Task<ActionResult<PublishResponse>> Publish([FromBody] PublishRequest request)
    {
        if (request == null || String.IsNullOrEmpty(request.Content))
            return BadRequest(new PublishResponse { Success = false, ErrorMessage = "消息内容不能为空" });

        var message = new MessageData
        {
            Id = Guid.NewGuid().ToString(),
            Content = request.Content,
            CreatedTime = DateTime.UtcNow,
            Properties = request.Properties
        };

        var response = await _queueService.PublishAsync(message);

        if (response.Success)
            return Ok(response);

        return StatusCode(500, response);
    }

    /// <summary>获取队列状态</summary>
    /// <returns>队列状态</returns>
    [HttpGet("status")]
    public ActionResult<Object> GetStatus()
    {
        var count = _queueService.GetQueueCount();
        return Ok(new
        {
            QueueLength = count,
            Timestamp = DateTime.UtcNow
        });
    }
}
