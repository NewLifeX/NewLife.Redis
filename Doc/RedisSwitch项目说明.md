# RedisSwitch 项目说明

## 项目概述

RedisSwitch 是一个基于 ASP.NET Core 的 Redis 消息交换机项目，主要功能包括：

1. **消息发布API**: 提供 HTTP API 接口，供外部系统发布消息到 Redis 队列
2. **消息消费转发**: 后台服务自动从 Redis 队列中消费消息，并转发到配置的目标 WebAPI 接口
3. **多消费者支持**: 支持配置多个消费者线程并发处理消息
4. **自动重试机制**: 消息转发失败时自动重试，提高消息处理的可靠性

## 项目结构

```
RedisSwitch/
├── Controllers/
│   └── MessageController.cs        # 消息发布控制器
├── Models/
│   ├── MessageData.cs              # 消息数据模型
│   ├── PublishRequest.cs           # 发布请求模型
│   ├── PublishResponse.cs          # 发布响应模型
│   └── RedisSwitchOptions.cs       # 交换机配置选项
├── Services/
│   ├── MessageConsumerService.cs   # 消息消费后台服务
│   └── RedisQueueService.cs        # Redis 队列服务
├── Program.cs                      # 应用程序入口
├── appsettings.json                # 应用配置文件
└── README.md                       # 项目说明文档
```

## 配置说明

### appsettings.json 配置

```json
{
  "Redis": {
    "Server": "127.0.0.1:6379",     // Redis 服务器地址
    "Password": "",                  // Redis 密码（可选）
    "Db": 0,                        // 数据库编号
    "Timeout": 15000                // 超时时间（毫秒）
  },
  "RedisSwitch": {
    "QueueName": "RedisSwitch",     // 队列名称
    "ConsumerCount": 1,             // 消费者数量
    "TargetApiUrl": "http://localhost:5001/api/target",  // 目标API地址
    "RetryCount": 3                 // 重试次数
  }
}
```

## API 接口文档

### 1. 发布消息

**接口地址**: `POST /api/message/publish`

**请求示例**:
```json
{
  "content": "这是一条测试消息",
  "properties": {
    "source": "TestSystem",
    "priority": "high",
    "timestamp": "2026-02-15T12:00:00"
  }
}
```

**响应示例**:
```json
{
  "success": true,
  "messageId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "errorMessage": null
}
```

**失败响应**:
```json
{
  "success": false,
  "messageId": null,
  "errorMessage": "消息内容不能为空"
}
```

### 2. 查询队列状态

**接口地址**: `GET /api/message/status`

**响应示例**:
```json
{
  "queueLength": 5,
  "timestamp": "2026-02-15T12:30:00"
}
```

## 使用流程

### 启动项目

1. 确保 Redis 服务已启动并可访问
2. 修改 `appsettings.json` 配置文件，设置正确的 Redis 连接信息和目标 API 地址
3. 运行项目：
   ```bash
   cd Samples/RedisSwitch
   dotnet run
   ```
4. 访问 Swagger 文档：`http://localhost:5000/swagger`

### 发布消息

使用 curl 发布消息：

```bash
curl -X POST "http://localhost:5000/api/message/publish" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "测试消息",
    "properties": {
      "userId": "12345",
      "action": "login"
    }
  }'
```

使用 PowerShell：

```powershell
$body = @{
    content = "测试消息"
    properties = @{
        userId = "12345"
        action = "login"
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/message/publish" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

### 查询队列状态

```bash
curl -X GET "http://localhost:5000/api/message/status"
```

## 工作原理

1. **消息发布流程**:
   - 客户端通过 HTTP POST 请求将消息发送到 `/api/message/publish` 接口
   - MessageController 接收请求，将消息封装为 MessageData 对象
   - RedisQueueService 将消息添加到 Redis 队列中
   - 返回发布结果给客户端

2. **消息消费转发流程**:
   - MessageConsumerService 作为后台服务启动
   - 根据配置创建多个消费者线程
   - 每个消费者线程循环从 Redis 队列中获取消息
   - 获取到消息后，通过 HTTP POST 将消息转发到目标 API
   - 如果转发失败，根据配置的重试次数自动重试
   - 转发成功后，消息自动从队列中移除

## 目标 API 要求

目标 API 需要满足以下要求：

1. **接受 POST 请求**
2. **Content-Type**: `application/json`
3. **请求体格式**: MessageData 对象的 JSON 序列化
   ```json
   {
     "id": "消息ID",
     "content": "消息内容",
     "createdTime": "2026-02-15T12:00:00",
     "properties": {
       "key1": "value1",
       "key2": "value2"
     }
   }
   ```

### 目标 API 示例

以下是一个简单的目标 API 示例（C#）：

```csharp
[ApiController]
[Route("api/[controller]")]
public class TargetController : ControllerBase
{
    private readonly ILogger<TargetController> _logger;

    public TargetController(ILogger<TargetController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult ReceiveMessage([FromBody] MessageData message)
    {
        _logger.LogInformation("Received message: {MessageId}, Content: {Content}", 
            message.Id, message.Content);
        
        // 处理消息
        // ...
        
        return Ok();
    }
}
```

## 性能调优建议

1. **调整消费者数量**: 根据消息处理速度和目标 API 的承载能力，调整 `ConsumerCount` 参数
2. **调整重试次数**: 根据目标 API 的稳定性，调整 `RetryCount` 参数
3. **监控队列长度**: 定期查询 `/api/message/status` 接口，监控队列积压情况
4. **Redis 连接池**: FullRedis 自动管理连接池，无需手动配置

## 注意事项

1. **Redis 连接**: 确保 Redis 服务器可访问，否则服务无法启动
2. **目标 API 可用性**: 目标 API 不可用时，消息会在重试失败后丢失（后续可考虑添加死信队列）
3. **消息幂等性**: 由于存在重试机制，目标 API 需要处理重复消息的情况
4. **网络延迟**: 目标 API 响应慢会影响整体吞吐量，建议异步处理
5. **日志记录**: 重要操作都有日志记录，便于问题排查

## 后续优化建议

1. **死信队列**: 添加死信队列机制，保存转发失败的消息
2. **消息优先级**: 支持消息优先级处理
3. **批量转发**: 实现批量消息转发，提高吞吐量
4. **健康检查**: 添加健康检查接口，监控服务状态
5. **消息持久化**: 添加消息持久化选项，防止消息丢失
6. **监控指标**: 添加 Prometheus 指标，监控消息处理速度、成功率等
7. **消息追踪**: 添加分布式追踪支持，便于问题定位

## 技术栈

- **框架**: ASP.NET Core 10.0
- **Redis 客户端**: NewLife.Redis
- **日志**: NewLife.Log / Microsoft.Extensions.Logging
- **API 文档**: Swagger / OpenAPI
- **HTTP 客户端**: IHttpClientFactory

## 版本历史

- **v1.0.0** (2026-02-15): 初始版本
  - 消息发布 API
  - 消息消费转发
  - 多消费者支持
  - 自动重试机制
  - 队列状态查询
