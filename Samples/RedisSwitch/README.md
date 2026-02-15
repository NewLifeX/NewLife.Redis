# Redis Switch - Redis消息交换机

Redis Switch 是一个消息交换机项目，能够把Redis消息队列消费到的数据发送给WebAPI接口，同时提供WebAPI接口供使用方发布消息。

## 功能特性

1. **消息发布接口**：提供HTTP API接口供外部系统发布消息到Redis队列
2. **消息消费转发**：后台服务自动消费Redis队列中的消息，并转发到配置的目标WebAPI接口
3. **多消费者支持**：支持配置多个消费者并发处理消息
4. **重试机制**：消息转发失败时自动重试
5. **队列状态查询**：提供接口查询当前队列长度

## 配置说明

在 `appsettings.json` 中配置Redis连接和转发参数：

```json
{
  "Redis": {
    "Server": "127.0.0.1:6379",
    "Password": "",
    "Db": 0,
    "Timeout": 15000
  },
  "RedisSwitch": {
    "QueueName": "RedisSwitch",
    "ConsumerCount": 1,
    "TargetApiUrl": "http://localhost:5001/api/target",
    "BatchSize": 10,
    "RetryCount": 3
  }
}
```

### 配置项说明

- **Redis**: Redis连接配置
  - `Server`: Redis服务器地址
  - `Password`: Redis密码（可选）
  - `Db`: 数据库编号
  - `Timeout`: 超时时间（毫秒）

- **RedisSwitch**: 交换机配置
  - `QueueName`: 队列名称
  - `ConsumerCount`: 消费者数量
  - `TargetApiUrl`: 目标API地址
  - `BatchSize`: 批量处理大小
  - `RetryCount`: 重试次数

## API接口

### 1. 发布消息

**接口**: `POST /api/message/publish`

**请求体**:
```json
{
  "content": "消息内容",
  "properties": {
    "key1": "value1",
    "key2": "value2"
  }
}
```

**响应**:
```json
{
  "success": true,
  "messageId": "xxx-xxx-xxx",
  "errorMessage": null
}
```

### 2. 查询队列状态

**接口**: `GET /api/message/status`

**响应**:
```json
{
  "queueLength": 10,
  "timestamp": "2026-02-15T12:00:00"
}
```

## 运行项目

1. 确保Redis服务已启动
2. 修改配置文件 `appsettings.json`
3. 运行项目：

```bash
dotnet run
```

4. 访问Swagger文档：`http://localhost:5000/swagger`

## 使用示例

### 发布消息示例

```bash
curl -X POST "http://localhost:5000/api/message/publish" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "测试消息",
    "properties": {
      "source": "system",
      "priority": "high"
    }
  }'
```

### 查询队列状态

```bash
curl -X GET "http://localhost:5000/api/message/status"
```

## 工作流程

1. 外部系统通过 `/api/message/publish` 接口发布消息到Redis队列
2. 后台消费者服务自动从Redis队列中获取消息
3. 消费者将获取到的消息转发到配置的目标API地址
4. 如果转发失败，会根据配置的重试次数自动重试
5. 成功转发后，消息从队列中移除

## 注意事项

1. 目标API需要接受POST请求，请求体为JSON格式的MessageData对象
2. 确保目标API能够正常访问
3. 建议根据实际负载调整消费者数量
4. Redis连接失败会导致服务无法启动
