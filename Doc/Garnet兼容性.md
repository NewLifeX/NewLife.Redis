# Garnet 兼容性说明

## 概述

[Garnet](https://github.com/microsoft/garnet) 是微软推出的高性能 Redis 兼容服务器，使用 C# 开发，基于 .NET 平台。NewLife.Redis 对 Garnet 提供了良好的兼容性支持，同时也针对 Garnet 尚未实现的功能提供了优雅的降级处理。

## 兼容性检测

NewLife.Redis 提供了自动的服务器类型检测功能：

```csharp
var redis = new FullRedis("127.0.0.1:6379", null, 0);

// 检查服务器类型
var serverType = redis.ServerType;  // Redis / Garnet / Unknown
var isGarnet = redis.IsGarnet;      // 是否为 Garnet 服务器

Console.WriteLine($"服务器类型：{serverType}");
Console.WriteLine($"Redis版本：{redis.Version}");
```

检测机制：
- 通过 `INFO` 命令获取服务器信息
- 检查 `redis_version` 或 `server` 字段中是否包含 "Garnet" 字符串
- 自动识别并设置服务器类型

## 支持的功能

Garnet 完全兼容以下 NewLife.Redis 功能：

### 基础操作
- ✅ 字符串操作（GET、SET、SETEX、MGET、MSET 等）
- ✅ 键操作（DEL、EXISTS、EXPIRE、TTL 等）
- ✅ 数值操作（INCR、DECR、INCRBY、DECRBY）
- ✅ 位操作（GETBIT、SETBIT、BITCOUNT 等）

### 数据结构
- ✅ List 列表（LPUSH、RPUSH、LPOP、RPOP、LRANGE 等）
- ✅ Hash 哈希（HSET、HGET、HMSET、HGETALL 等）
- ✅ Set 集合（SADD、SMEMBERS、SISMEMBER 等）
- ✅ Sorted Set 有序集合（ZADD、ZRANGE、ZRANK 等）
- ✅ HyperLogLog（PFADD、PFCOUNT、PFMERGE）

### 高级功能
- ✅ 发布订阅（PUBLISH、SUBSCRIBE、PSUBSCRIBE）
- ✅ 事务（MULTI、EXEC、DISCARD）
- ✅ 管道（Pipeline）
- ✅ Lua 脚本（EVAL、EVALSHA）
- ✅ 连接池
- ✅ 批量操作

### 队列实现
- ✅ `RedisQueue<T>`（基于 List 的简单队列）
- ✅ `RedisDelayQueue<T>`（基于 Sorted Set 的延迟队列）
- ✅ 可靠队列（基于 RPOPLPUSH 的消费确认机制）

## 不支持的功能

### Stream 流式队列

Garnet 目前**不支持** Redis Stream 功能（XADD、XREAD、XGROUP 等命令）。

NewLife.Redis 会在使用 `RedisStream<T>` 时自动检测并抛出友好的异常：

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);  // Garnet 服务器
var stream = redis.GetStream<MyMessage>("mystream");

try
{
    // 尝试使用 Stream 功能
    stream.Add(new MyMessage { Text = "Hello" });
}
catch (NotSupportedException ex)
{
    // 会抛出异常：Garnet 服务器暂不支持 Redis Stream 功能。
    // 请使用 RedisQueue 等其他队列实现。
    Console.WriteLine(ex.Message);
}

// 检查是否支持
if (stream.IsSupported)
{
    stream.Add(new MyMessage { Text = "Hello" });
}
else
{
    Console.WriteLine("当前服务器不支持 Stream 功能");
}
```

**替代方案**：

对于 Garnet 服务器，推荐使用以下队列实现替代 Stream：

1. **简单队列场景**：使用 `RedisQueue<T>`
```csharp
var queue = redis.GetQueue<MyMessage>("myqueue");
queue.Add(new MyMessage { Text = "Hello" });
var msg = queue.TakeOne(timeout: 0);  // 阻塞等待
```

2. **延迟队列场景**：使用 `RedisDelayQueue<T>`
```csharp
var delayQueue = redis.GetDelayQueue<MyMessage>("delayqueue");
delayQueue.Add(new MyMessage { Text = "Delayed" }, delay: 60);  // 60秒后可消费
```

3. **可靠消费场景**：使用 List + RPOPLPUSH
```csharp
var sourceQueue = redis.GetList<MyMessage>("source");
var backupQueue = redis.GetList<MyMessage>("backup");

// 可靠消费
var msg = sourceQueue.RPOPLPUSH(backupQueue.Key);
// 处理消息...
// 处理成功后从备份队列删除
backupQueue.Remove(msg);
```

## 使用示例

### 基础功能示例

```csharp
using NewLife.Caching;

// 连接 Garnet 服务器
var redis = new FullRedis("127.0.0.1:7000", null, 0)
{
    Log = XTrace.Log
};

// 检查服务器类型
if (redis.IsGarnet)
{
    Console.WriteLine("已连接到 Garnet 服务器");
}

// 基础操作
redis.Set("user:1001", new { Name = "张三", Age = 25 }, 3600);
var user = redis.Get<dynamic>("user:1001");

// List 操作
var list = redis.GetList<String>("tasks");
list.Add("Task1");
list.Add("Task2");

// Hash 操作
var hash = redis.GetDictionary<String>("user:1001:profile");
hash["Email"] = "zhangsan@example.com";
hash["Phone"] = "13800138000";

// 有序集合
var sortedSet = redis.GetSortedSet<String>("leaderboard");
sortedSet.Add("Player1", 100);
sortedSet.Add("Player2", 95);
```

### 队列使用示例

```csharp
// 使用简单队列（兼容 Garnet）
var queue = redis.GetQueue<OrderMessage>("orders");

// 生产者
queue.Add(new OrderMessage { OrderId = "12345", Amount = 99.99m });

// 消费者（阻塞等待）
var msg = queue.TakeOne(timeout: 30);  // 30秒超时
if (msg != null)
{
    Console.WriteLine($"处理订单：{msg.OrderId}");
}

// 批量消费
foreach (var order in queue.Take(10))
{
    Console.WriteLine($"订单：{order.OrderId}");
}
```

### 自动适配示例

```csharp
// 封装一个自动适配的队列工厂
public static class QueueFactory
{
    public static IProducerConsumer<T> CreateQueue<T>(Redis redis, String key)
    {
        // 如果是 Garnet 或不支持 Stream，使用 RedisQueue
        if (redis.IsGarnet || redis.Version < new Version(5, 0))
        {
            return redis.GetQueue<T>(key);
        }
        // 否则使用更强大的 Stream
        else
        {
            return redis.GetStream<T>(key);
        }
    }
}

// 使用
var queue = QueueFactory.CreateQueue<MyMessage>(redis, "myqueue");
queue.Add(new MyMessage { Text = "Hello" });
```

## 性能对比

根据官方测试，Garnet 在某些场景下性能优于 Redis：

| 操作类型 | Redis 7.x | Garnet | 说明 |
|---------|-----------|--------|------|
| GET/SET | 基准 | 1.2-1.5x | Garnet 在简单操作上性能更优 |
| List 操作 | 基准 | 0.9-1.1x | 性能接近 |
| Hash 操作 | 基准 | 1.0-1.3x | Garnet 在大 Hash 上有优势 |
| 高并发 | 基准 | 1.5-2.0x | Garnet 并发性能优秀 |

实际性能受网络、硬件、数据大小等多种因素影响，建议根据实际场景进行测试。

## 最佳实践

1. **自动检测**：在应用启动时检查服务器类型，动态选择合适的数据结构
2. **优雅降级**：对于 Stream 等不支持的功能，使用 List/Sorted Set 等基础结构替代
3. **功能封装**：将队列等功能封装为接口，根据服务器类型选择不同实现
4. **错误处理**：捕获 `NotSupportedException`，提供友好的错误提示
5. **版本升级**：关注 Garnet 的更新，及时使用新支持的功能

## 故障排查

### 问题：无法识别 Garnet 服务器

**解决方案**：
1. 检查 Garnet 版本是否足够新
2. 手动验证 INFO 命令输出
```csharp
var info = redis.GetInfo();
Console.WriteLine(info["redis_version"]);
Console.WriteLine(info.ContainsKey("server") ? info["server"] : "N/A");
```

### 问题：Stream 功能报错

**解决方案**：
1. 确认当前连接的是 Garnet 服务器：`redis.IsGarnet`
2. 使用 `stream.IsSupported` 检查功能可用性
3. 改用 `RedisQueue<T>` 或其他兼容的队列实现

### 问题：某些命令不支持

**解决方案**：
1. 查看 Garnet 官方文档确认命令支持情况
2. 使用 try-catch 捕获 `RedisException` 并降级处理
3. 报告给 NewLife.Redis 项目以改进兼容性检测

## 参考资源

- [Garnet 官方仓库](https://github.com/microsoft/garnet)
- [Garnet 命令支持列表](https://microsoft.github.io/garnet/docs/commands)
- [NewLife.Redis 文档](https://newlifex.com/core/redis)
- [Redis 命令参考](https://redis.io/commands)

## 更新日志

- **2026-02-15**：添加 Garnet 服务器类型检测功能
- **2026-02-15**：为 RedisStream 添加 Garnet 兼容性检查
- **2026-02-15**：完善文档和使用示例

如有问题或建议，欢迎在 [GitHub Issues](https://github.com/NewLifeX/NewLife.Redis/issues) 反馈。
