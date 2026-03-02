# NewLife.Redis - 高性能 Redis 客户端组件

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Redis?label=dev%20nuget&logo=nuget)

## [[English]](https://github.com/NewLifeX/NewLife.Redis/blob/master/Readme.en.md)

`NewLife.Redis` 是新生命团队打造的**高性能 / 高吞吐 / 易集成**的 .NET Redis 客户端组件。自 2017 年起在多个千万 / 百亿级数据与高并发生产平台稳定运行，经受日均 **80+ 亿次**命令调用考验。

> 📖 完整文档：[需求文档](Doc/需求文档.md) | [架构文档](Doc/架构文档.md) | [竞品分析](Doc/竞品分析.md) | [Garnet 兼容性](Doc/Garnet兼容性.md)

---
## 目录
- [NewLife.Redis - 高性能 Redis 客户端组件](#newliferedis---高性能-redis-客户端组件)
  - [\[English\]](#english)
  - [目录](#目录)
  - [产品介绍](#产品介绍)
  - [核心特性](#核心特性)
  - [Redis 协议与版本支持](#redis-协议与版本支持)
    - [RESP 协议支持](#resp-协议支持)
    - [Redis 版本特性支持](#redis-版本特性支持)
  - [架构与模块划分](#架构与模块划分)
  - [安装与快速开始](#安装与快速开始)
  - [基础用法](#基础用法)
  - [数据结构操作](#数据结构操作)
    - [List（列表）](#list列表)
    - [Hash（哈希）](#hash哈希)
    - [Set（集合）/ SortedSet（有序集合）](#set集合-sortedset有序集合)
    - [Stack（栈）](#stack栈)
    - [Geo（地理信息）](#geo地理信息)
    - [HyperLogLog（基数统计）](#hyperloglog基数统计)
  - [管道 Pipeline 与自动合并](#管道-pipeline-与自动合并)
  - [消息队列](#消息队列)
    - [简单队列](#简单队列)
    - [可靠队列（至少一次语义）](#可靠队列至少一次语义)
    - [延迟队列](#延迟队列)
    - [Stream 流队列（Redis 5.0+，多消费组）](#stream-流队列redis-50多消费组)
  - [发布订阅](#发布订阅)
  - [集群与高可用](#集群与高可用)
    - [多地址自动切换](#多地址自动切换)
    - [自动集群模式检测](#自动集群模式检测)
  - [云厂商适配](#云厂商适配)
    - [阿里云 KVStore / Tair](#阿里云-kvstore--tair)
    - [腾讯云 Redis](#腾讯云-redis)
    - [华为云 DCS](#华为云-dcs)
  - [序列化与编码器](#序列化与编码器)
  - [安全与认证](#安全与认证)
  - [可观测性](#可观测性)
  - [扩展包](#扩展包)
  - [Garnet 兼容性](#garnet-兼容性)
  - [功能拆分与完成情况](#功能拆分与完成情况)
    - [数据结构](#数据结构)
    - [消息队列](#消息队列-1)
    - [集群支持](#集群支持)
    - [协议与高级特性](#协议与高级特性)
  - [性能测试参考](#性能测试参考)
  - [最佳实践](#最佳实践)
  - [常见问题 FAQ](#常见问题-faq)
  - [路线图 Roadmap](#路线图-roadmap)
  - [竞品对比](#竞品对比)
  - [新生命项目矩阵](#新生命项目矩阵)
  - [贡献指南 \& 社区](#贡献指南--社区)
  - [许可证](#许可证)
  - [新生命开发团队](#新生命开发团队)

---
## 产品介绍

**NewLife.Redis** 为 .NET 开发者提供对 Redis 全生命周期的访问能力：

- **协议完整**：实现 RESP2 协议，覆盖 Redis 2.8 ～ 7.x 全版本主流命令，支持 String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog 所有数据结构。
- **消息队列**：内置简单队列、**可靠队列（RPOPLPUSH + Ack 语义）**、延迟队列、Stream 多消费组队列，开箱即用，无需重复造轮子。
- **集群高可用**：支持 Standalone / 主从复制 / Sentinel 哨兵 / Cluster 原生集群，自动故障切换。
- **云厂商适配**：针对**阿里云 KVStore / 腾讯云 Redis / 华为云 DCS** 提供专项配置说明与适配，覆盖代理模式、ACL 认证、SSL 加密、特殊认证格式等差异。
- **兼容实现**：自动识别 Microsoft Garnet、kvrocks 等 Redis 兼容实现，功能不支持时优雅降级并提供明确替代方案。
- **宽框架支持**：一个包覆盖 **.NET Framework 4.5 ～ .NET 10+**，适配工控、政务、IoT 等遗留场景。

---
## 核心特性

- 经大规模生产验证：200+ Redis 实例，日峰值 1 亿+ 业务对象写入 / 80+ 亿命令调用
- 低延迟：单次 Get/Set 往返 200～600µs（含网络）
- 高吞吐：内置连接池（最大 100,000 并发）+ 高效同步 RESP 协议解析 + 可选自动管道合并
- 自动重试与多地址故障切换：Server 支持逗号分隔多节点，网络异常快速切换，默认 10 秒屏蔽窗口
- 丰富高级结构：List / Hash / Set / SortedSet / Stack / Geo / HyperLogLog / Stream
- 多种消息队列范式：简单 / 可靠 / 延迟 / Stream / 多消费组
- 可插拔编码器：默认 JSON，可扩展二进制序列化
- APM 追踪：支持 `ITracer` 链路追踪 + `PerfCounter` 性能计数器
- 强命名 + 多目标框架：net45 / net461 / netstandard2.0 / netstandard2.1 / (扩展包含 netcoreapp3.1 → net10)
- 零外部重量级依赖，充分复用 NewLife 生态（日志、配置、序列化、安全）

---
## Redis 协议与版本支持

### RESP 协议支持

| 协议 | 状态 | 说明 |
|------|------|------|
| RESP2 | ✅ 完整支持 | 简单字符串 / 错误 / 整数 / 批量字符串 / 数组 |
| RESP3 | 🔄 规划中 | Map / Set / Double 类型，需 HELLO 握手协商 |

### Redis 版本特性支持

| Redis 版本 | 主要特性 | 状态 |
|-----------|---------|------|
| 2.8 | 基础命令、SCAN、HyperLogLog、Pub/Sub | ✅ 完整支持 |
| 3.0 | Cluster 原生集群、CRC16 分片路由 | ✅ 完整支持 |
| 3.2 | GEO 地理信息 | ✅ 完整支持 |
| 4.0 | UNLINK 异步删除、MODULE | 🔄 部分支持 |
| 5.0 | Stream 数据结构（XADD/XREAD/XGROUP/XACK 全套）| ✅ 完整支持 |
| 6.0 | ACL 访问控制、TLS/SSL、LMOVE、RESET | ✅ 完整支持 |
| 6.2 | COPY、GETDEL、GETEX、GEOSEARCH | ✅ 主要命令支持 |
| 7.0 | Functions（FCALL）、LMPOP/ZMPOP、分片 Pub/Sub | 🔄 规划中 |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 规划中 |

---
## 架构与模块划分

```
┌─────────────────────────────────────────────────────────────────┐
│                          应用层                                  │
│  ASP.NET Core / Console / WinForms / IoT / Worker Service        │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                扩展集成层 (NewLife.Redis.Extensions)             │
│  IDistributedCache · IDataProtection · AddRedisCaching DI        │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                   高级功能层 (FullRedis)                          │
│  RedisList<T> · RedisHash<T> · RedisSet<T> · RedisSortedSet<T>  │
│  RedisStack<T> · RedisGeo · HyperLogLog · PrefixedRedis          │
│  RedisQueue<T> · RedisReliableQueue<T> · RedisDelayQueue<T>      │
│  RedisStream<T> · PubSub · RedisEventBus                         │
│  RedisCluster · RedisSentinel · RedisReplication                 │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                   核心客户端层 (Redis)                            │
│  连接池 ObjectPool<RedisClient>（Min=10, Max=100000）             │
│  多节点切换 · Pipeline 管理 · 编码器 · 配置解析                   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                   协议通信层 (RedisClient)                        │
│  TCP/TLS 连接 · RESP2 编解码 · ArrayPool<byte> 缓冲区             │
└──────────────────────────────┬──────────────────────────────────┘
                               │ TCP/TLS
┌──────────────────────────────▼──────────────────────────────────┐
│  Redis Server / Garnet / kvrocks / 阿里云 / 腾讯云 / 华为云       │
└─────────────────────────────────────────────────────────────────┘
```

关键类说明：

| 类 | 职责 |
|----|------|
| `Redis` | 核心客户端：连接池、多节点切换、Pipeline、配置管理 |
| `FullRedis` | 增强客户端：集群路由、键前缀、数据结构工厂方法 |
| `RedisClient` | RESP 协议单元：TCP 连接、命令编解码（内部，池化管理）|
| `RedisList<T>` 等 | 泛型数据结构封装，统一编码策略 |
| `RedisReliableQueue<T>` | 可靠消息队列（RPOPLPUSH + Ack 确认）|
| `RedisStream<T>` | Stream 流队列（Redis 5.0+，消费组模式）|
| `IPacketEncoder` | 可插拔编解码策略 |

---
## 安装与快速开始

```bash
# 稳定版
dotnet add package NewLife.Redis
# ASP.NET Core 扩展
dotnet add package NewLife.Redis.Extensions
# 开发版（含预发布）
dotnet add package NewLife.Redis --prerelease
```

**推荐单例，线程安全，全程复用：**

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();

var rds = new FullRedis("127.0.0.1:6379", "password", 0)
{
    Log = XTrace.Log,           // 可选：组件日志
    AutoPipeline = 100,         // 可选：命令数达到 100 自动提交管道
};

// 写入对象（自动 JSON 序列化）
rds.Set("user:1", new { Name = "Alice", Age = 30 }, expire: 3600);

// 读取（自动反序列化）
var name = rds.Get<String>("user:1");
Console.WriteLine(name);
```

**连接字符串模式（推荐配置中心）：**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

---
## 基础用法

```csharp
// 写入 / 读取
rds.Set("k1", 123, expire: 600);
var v = rds.Get<Int32>("k1");

// 仅在不存在时写入
var ok = rds.Add("k2", "init");

// 替换并返回旧值
var old = rds.Replace("k2", "new");

// 计数器
rds.Increment("counter", 1);
rds.Decrement("counter", 2);

// 过期管理
rds.SetExpire("k1", TimeSpan.FromMinutes(10));
var ttl = rds.GetExpire("k1");

// 批量读写
rds.SetAll(new Dictionary<String, Object> { ["a"] = 1, ["b"] = 2 }, expire: 300);
var dict = rds.GetAll<Int32>(["a", "b"]);

// 键前缀（PrefixedRedis）
var scoped = rds.GetPrefixed("myapp:");
scoped.Set("user", "Alice");  // 实际 key 为 myapp:user
```

---
## 数据结构操作

### List（列表）

```csharp
var list = rds.GetList<String>("queue:demo");
list.Add("job1");
list.Add("job2");
var first = list[0];
var popped = list.RightPop();
```

### Hash（哈希）

```csharp
var hash = rds.GetDictionary<String>("user:1001");
hash["Name"] = "Alice";
hash["Email"] = "alice@example.com";
var name = hash["Name"];
var all = hash.GetAll();
```

### Set（集合）/ SortedSet（有序集合）

```csharp
var set = rds.GetSet<String>("tags");
set.Add("redis");
set.Add("cache");
var contains = set.Contains("redis");

var zset = rds.GetSortedSet<String>("leaderboard");
zset.Add("Player1", 100.0);
zset.Add("Player2", 95.5);
var top3 = zset.GetByRank(0, 2);
```

### Stack（栈）

```csharp
var stack = rds.GetStack<String>("undo:ops");
stack.Push("op1");
stack.Push("op2");
var last = stack.Pop();   // "op2"
```

### Geo（地理信息）

```csharp
var geo = rds.GetGeo("locations");
geo.Add("Beijing",  116.4074, 39.9042);
geo.Add("Shanghai", 121.4737, 31.2304);
var dist = geo.GetDistance("Beijing", "Shanghai", "km");
var nearby = geo.Search("Beijing", 500, "km");
```

### HyperLogLog（基数统计）

```csharp
var hll = rds.GetHyperLogLog("uv:today");
hll.Add("user1", "user2", "user3");
var count = hll.Count;  // 近似基数
hll.Merge("uv:yesterday");
```

> 注意：基础 `Redis` 实例仅支持 KV 操作，高级集合请使用 `FullRedis`。

---
## 管道 Pipeline 与自动合并

Pipeline 可显著降低网络往返（RTT），提升批量操作吞吐：

```csharp
// 显式管道
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**自动模式**（推荐生产使用）：

```csharp
rds.AutoPipeline = 100;    // 写操作达到 100 条自动提交
rds.FullPipeline = true;   // 读写操作均进入管道（吞吐优先场景）
```

---
## 消息队列

### 简单队列

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// 生产者
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// 消费者（阻塞等待，timeout=0 永久阻塞）
var msg = queue.TakeOne(timeout: 30);
```

### 可靠队列（至少一次语义）

```csharp
var queue = rds.GetReliableQueue<OrderMsg>("orders");

// 生产
queue.Add(new OrderMsg { OrderId = "12345" });

// 消费 + 确认
var msg = queue.TakeOne(timeout: 30);
if (msg != null)
{
    // 处理消息...
    queue.Acknowledge(msg);   // 确认消费，从 Ack 队列移除
}
// 若进程崩溃未确认，60s 后消息自动回滚到主队列
```

### 延迟队列

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// 60 秒后可被消费
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// 消费（自动等待到期消息）
var msg = delay.TakeOne(timeout: 5);
```

### Stream 流队列（Redis 5.0+，多消费组）

```csharp
var stream = rds.GetStream<LogEntry>("logs");
stream.Group = "processor-group";

// 生产
stream.Add(new LogEntry { Level = "INFO", Message = "Application started" });

// 消费（多实例竞争消费同一组）
var entry = stream.TakeOne(timeout: 15);
if (entry != null)
{
    // 处理...
    stream.Acknowledge(entry.Id);
}
```

---
## 发布订阅

```csharp
var pubsub = rds.GetPubSub("events:user");

// 发布
pubsub.Publish("login:Alice");

// 订阅（异步循环）
var cts = new CancellationTokenSource();
await pubsub.SubscribeAsync((channel, message) =>
{
    Console.WriteLine($"[{channel}] {message}");
}, cts.Token);
```

---
## 集群与高可用

### 多地址自动切换

```csharp
// 逗号分隔多地址，网络异常时自动切换，60 秒后切回主节点
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // 不可用节点屏蔽 10 秒（默认）
    Retry = 3,            // 重试次数（默认）
};
```

### 自动集群模式检测

```csharp
// AutoDetect=true 时，通过 INFO 自动识别工作模式
// ⚠️ 公有云代理模式下请保持默认 false，避免获取到内网节点地址
var rds = new FullRedis("sentinel-host:26379", "pass", 0)
{
    AutoDetect = true,    // 自动识别 Cluster / Sentinel / Replication
};
```

---
## 云厂商适配

### 阿里云 KVStore / Tair

```csharp
// 标准版（Redis 2.8/4.0/5.0/6.0/7.0）
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6379", "YourPassword", 0);

// Redis 6.0 ACL 用户认证
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6379", "YourUser", "YourPassword", 0);

// SSL 加密连接
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6380", "YourPassword", 0)
{
    SslProtocol = SslProtocols.Tls12,
};

// ⚠️ 公有云代理模式：AutoDetect 保持默认 false
// ⚠️ 集群版：代理已处理分片路由，客户端无需感知内部拓扑
```

### 腾讯云 Redis

```csharp
// 腾讯云标准/集群版：UserName 填写实例 ID（AUTH instanceId:password 格式）
var rds = new FullRedis("xxxxxx.redis.tencentcds.com:6379", "instanceId", "YourPassword", 0);

// 或连接字符串形式
var rds = FullRedis.Create("server=xxxxxx.redis.tencentcds.com:6379;username=instanceId;password=YourPassword;db=0");
```

### 华为云 DCS

```csharp
// 主备版（标准 AUTH）
var rds = new FullRedis("xxxxxx.dcs.myhuaweicloud.com:6379", "YourPassword", 0);

// SSL 加密版（下载 PEM 证书后使用）
var rds = new FullRedis("xxxxxx.dcs.myhuaweicloud.com:6380", "YourPassword", 0)
{
    SslProtocol = SslProtocols.Tls12,
    Certificate = new X509Certificate2("huawei-dcs.pem"),
};

// ⚠️ 集群版不支持 SELECT，Db 必须为 0
// ⚠️ AutoDetect = false（默认），避免内网节点地址问题
```

**云厂商通用注意事项：**
- 公有云代理模式：保持 `AutoDetect = false`（默认值），不让客户端自动探测内网节点
- 连接数配额：各云厂商各规格连接数有上限，合理设置连接池 `Max` 参数
- TCP 保活：华为云默认 300 秒 TCP 空闲超时，设置 `Timeout` 小于此值并定期 PING

---
## 序列化与编码器

默认编码：`RedisJsonEncoder`（内置 JSON 主机），开箱即用。

```csharp
// 查看当前 JSON 配置
var jsonHost = RedisJsonEncoder.GetJsonHost();

// 切换为自定义编码器（如 MessagePack）
rds.Encoder = new MyMessagePackEncoder();
```

基元类型（`String` / `Int32` / `Int64` / `Double` / `Boolean`）直接序列化，不经 JSON，性能最优。

---
## 安全与认证

```csharp
// Redis 6.0+ ACL 认证
var rds = new FullRedis("host:6379", "username", "password", 0);

// TLS 加密（指定协议版本）
rds.SslProtocol = SslProtocols.Tls12;

// 证书验证（双向 mTLS）
rds.Certificate = new X509Certificate2("client.pem", "certPassword");

// 连接字符串密码加密（ProtectedKey 机制）
// 配置 ProtectedKey.Instance.Secret，连接字符串中密码自动解密
```

---
## 可观测性

```csharp
// 结构化日志
rds.Log       = XTrace.Log;      // 组件日志（连接切换、集群变化、重试）
rds.ClientLog = XTrace.Log;      // 协议层日志（每条命令，调试时开启）

// 性能计数器
rds.Counter = new PerfCounter();
var stats = rds.Counter.GetAll();   // 获取 ops/s、延迟、错误率

// APM 链路追踪（与 NewLife.Stardust / OpenTelemetry 兼容）
rds.Tracer = DefaultTracer.Instance;
// 所有 Execute / 队列生产消费均自动埋点，key 作为 Tag
```

---
## 扩展包

`NewLife.Redis.Extensions` 提供 ASP.NET Core 深度集成：

```bash
dotnet add package NewLife.Redis.Extensions
```

```csharp
// Program.cs
builder.Services.AddRedisCaching(options =>
{
    options.Server   = "127.0.0.1:6379";
    options.Password = "pass";
    options.Db       = 0;
    options.Prefix   = "myapp:";
});

// 同时自动注册 IDistributedCache + FullRedis 单例

// 数据保护密钥存储到 Redis
builder.Services.AddDataProtection()
    .PersistKeysToRedis(redis, "DataProtection-Keys");
```

---
## Garnet 兼容性

NewLife.Redis 完整支持微软的 [Garnet](https://github.com/microsoft/garnet) 高性能 Redis 兼容服务器，自动检测并优雅降级。

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// 自动检测服务器类型
if (redis.IsGarnet)
    Console.WriteLine("已连接到 Garnet 服务器");

Console.WriteLine($"服务器类型：{redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"服务器版本：{redis.Version}");
```

| 功能 | Garnet 支持状态 |
|------|---------------|
| String / List / Hash / Set / Sorted Set | ✅ 完全支持 |
| HyperLogLog / Geo | ✅ 完全支持 |
| Pub/Sub / 事务 / Lua 脚本 | ✅ 完全支持 |
| Pipeline / 管道 | ✅ 完全支持 |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ 完全支持 |
| `RedisReliableQueue<T>` | ✅ 完全支持 |
| `RedisStream<T>` | ❌ Garnet 暂不支持 Stream，自动抛出 `NotSupportedException` 并提示替代方案 |

详见：[Garnet 兼容性文档](Doc/Garnet兼容性.md)

---
## 功能拆分与完成情况

> 图例：✅ 已完成 | 🔄 规划中 | ❌ 明确不计划

### 数据结构

| 结构 | 完成状态 | 说明 |
|------|---------|------|
| String KV + 批量 | ✅ | MGET/MSET/INCR/APPEND/GETRANGE 等 |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE 等 |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN 等 |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF 等 |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN 等 |
| Stream | ✅ | XADD/XREAD/XGROUP/XACK 全套 |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH 等 |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

### 消息队列

| 队列类型 | 完成状态 | 语义保障 |
|---------|---------|---------|
| `RedisQueue<T>` | ✅ | 至多一次 |
| `RedisReliableQueue<T>` | ✅ | 至少一次（RPOPLPUSH + Ack）|
| `RedisDelayQueue<T>` | ✅ | 延迟投递（Sorted Set）|
| `RedisStream<T>` | ✅ | 有序 + 消费组 + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | 多消费组广播 |
| `PubSub` + `RedisEventBus` | ✅ | 实时推送 |
| RedLock 分布式锁 | 🔄 | 多实例 Redlock 算法 |

### 集群支持

| 模式 | 完成状态 |
|------|---------|
| Standalone 单机 | ✅ |
| 主从复制 Replication | ✅ |
| Sentinel 哨兵 | ✅ |
| Cluster 原生集群 | ✅ |
| kvrocks 自动适配 | ✅ |
| Garnet 自动检测 | ✅ |

### 协议与高级特性

| 功能 | 完成状态 |
|------|---------|
| RESP2 完整支持 | ✅ |
| RESP3 | 🔄 规划中 |
| SSL/TLS | ✅ |
| ACL 认证 | ✅ |
| 事务 MULTI/EXEC/WATCH | ✅ |
| Lua 脚本 EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 规划中 |
| Pipeline（显式+自动）| ✅ |
| 模式订阅 PSUBSCRIBE | 🔄 |
| UNLINK 异步删除 | 🔄 |

---
## 性能测试参考

源码内置 Benchmark（`Samples/Benchmark`），典型结果（40 逻辑处理器，本地 Redis 7.0）：

```
写入 400,000 项 | 4 线程  | ~576,368 ops/s
读取 800,000 项 | 8 线程  | ~647,249 ops/s
删除 800,000 项 | 8 线程  | ~1,011,378 ops/s
批量写入（管道）| 4 线程  | ~1,800,000 ops/s
```

手动触发 Bench：

```csharp
rds.Bench(rand: true, batch: 100);  // 随机键 + 批量优化
```

> 实际性能受网络 RTT / Value 大小 / 序列化复杂度影响。建议单 Value 控制在 1～1.4KB。

---
## 最佳实践

- **单例复用**：`FullRedis` 内部有连接池，强烈建议单例供全程使用，不要每次 `new`
- **Key 设计**：使用 `:` 分层，如 `user:1001:profile`；生产环境使用 `Prefix` 隔离命名空间
- **批量优先**：能 `GetAll/SetAll` 不循环单键；利用 `AutoPipeline` 自动合并写操作
- **Value 大小**：控制在 1～2KB；大对象拆分 / 压缩 / 用 Hash 分字段存储
- **可靠消费**：生产场景使用 `RedisReliableQueue<T>` 而非简单 List，避免消息丢失
- **云厂商**：公有云保持 `AutoDetect = false`，避免内网节点地址问题
- **监控**：生产启用 `Counter` 统计；`ClientLog` 仅调试时打开，避免热路径日志开销
- **SSL**：公有云 / 生产环境启用 TLS 加密传输

---
## 常见问题 FAQ

**Q: 是否支持 Redis Cluster 分片？**  
A: 支持。`FullRedis` 通过 `AutoDetect=true` 自动识别 Cluster 模式，CRC16 路由、MOVED/ASK 重定向均已实现。公有云代理模式建议保持默认 `AutoDetect=false`。

**Q: 和 StackExchange.Redis 相比有什么区别？**  
A: NewLife.Redis 采用连接池模型（对比 StackExchange 的多路复用），连接行为更直观；内置消息队列封装（可靠队列/延迟队列）是核心差异化；支持 .NET Framework 4.5。详见 [竞品分析](Doc/竞品分析.md)。

**Q: 如何处理大 Value 导致的慢查询？**  
A: 拆分数据结构（如 Hash 分字段）+ 批量读写 + 二进制编码器 + 控制 `MaxMessageSize`（默认 1MB 上限）。

**Q: 连接池用完了怎么办？**  
A: 默认最大 100,000 连接，正常业务不会耗尽。若出现等待超时，检查是否有连接泄漏（未 `Dispose`）或某些操作持续长时间占用。

**Q: 使用阿里云 / 腾讯云为什么连接失败？**  
A: 检查：① `AutoDetect` 是否为 false（默认正确）；② 认证格式是否正确（腾讯云需要 `username=instanceId`）；③ 网络安全组 / 白名单是否放开客户端 IP；④ 集群版是否禁止使用 SELECT 切库。

**Q: Garnet 服务器是否支持所有功能？**  
A: 基础操作、List/Hash/Set/ZSet/HLL/Geo/队列均支持；Stream 暂不支持，会自动抛出 `NotSupportedException` 并提示用 `RedisQueue<T>` 替代。

---
## 路线图 Roadmap

- [ ] RESP3 协议支持（Redis 7.x 新特性基础）
- [ ] Redis Functions（FCALL/FUNCTION，替代 Lua 脚本）
- [ ] LMPOP / ZMPOP（Redis 7.0 批量弹出）
- [ ] RedLock 多实例分布式锁
- [ ] Bitmap / Bitfield 操作
- [ ] 模式订阅 PSUBSCRIBE 完整实现
- [ ] UNLINK 异步删除
- [ ] Prometheus 指标导出适配器
- [ ] 阿里云 Tair 扩展命令（TairString / TairZSet / TairHash）
- [ ] 华为云 DCS 集群版节点地址映射适配
- [x] ~~发布订阅 PubSub~~（已完成）
- [x] ~~RedisStream 多消费组~~（已完成）
- [x] ~~Garnet 服务器类型检测~~（已完成）

欢迎通过 Issue / PR 参与投票或补充需求。

---
## 竞品对比

| 能力 | NewLife.Redis | StackExchange.Redis | FreeRedis | 备注 |
|------|--------------|--------------------|-----------|----|
| .NET Framework 4.5 | ✅ | ❌（4.6.1+）| ❌ | 工控 / 政务遗留系统关键 |
| 连接模型 | 连接池 | 多路复用 | 连接池 | 各有适用场景 |
| 命令覆盖率 | ~75% | ~95% | ~98% | 规划提升至 90%+ |
| 内置消息队列封装 | ✅ 完整 | ❌ | 🔄 部分 | 可靠 / 延迟队列独有 |
| 内置序列化 | ✅ | ❌ 手动 | ❌ 手动 | 降低样板代码 |
| 云厂商专项适配 | ✅（阿里/腾讯/华为）| 通用 | 通用 | 有专项文档 |
| Garnet 显式适配 | ✅ | ❌ | ❌ | 类型检测 + 降级 |
| kvrocks 适配 | ✅ | ❌ | ❌ | 自动识别 |
| APM 追踪 | ITracer 接口 | ActivitySource | ❌ | 可对接 OpenTelemetry |
| RESP3 | 🔄 | ✅ | ✅ | SE.Redis 已支持 |

详细分析见 [竞品分析文档](Doc/竞品分析.md)。

---
## 新生命项目矩阵

| 项目 | 说明 |
|------|------|
| [NewLife.Core](https://github.com/NewLifeX/X) | 核心库，日志 / 配置 / 缓存 / 序列化 / APM |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 大数据 ORM，百亿级 + 分表 + 读写分离 |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 超高性能网络库（千万级吞吐）|
| [Stardust](https://github.com/NewLifeX/Stardust) | 分布式服务 / 配置 / 注册 / 发布中心 |
| [AntJob](https://github.com/NewLifeX/AntJob) | 分布式计算 & 调度平台 |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | RocketMQ 纯托管客户端 |

> 完整矩阵、企业级解决方案与商业支持请访问：https://newlifex.com

---
## 贡献指南 & 社区

1. 提交前阅读仓库 `.github/copilot-instructions.md`（编码规范 & 审核清单）
2. PR：保持最小变更、添加必要注释与测试说明
3. Issue：提供版本、运行环境、最小复现场景

社区：QQ 群 1600800 / 1600838；GitHub Discussions / Issues 参与答疑。

---
## 许可证

MIT License。可自由商用 / 修改 / 再发行（无需额外授权）。保留版权声明即可。

---
## 新生命开发团队

![XCode](https://newlifex.com/logo.png)

团队自 2002 年迄今，维护 80+ .NET / IoT / 分布式相关开源项目，NuGet 累计下载超 400 万。产品与组件已广泛服务于电力、物流、工业控制、教育、通信、文博等行业。  
网站：https://newlifex.com | 开源：https://github.com/NewLifeX  
微信公众号：

![智能大石头](https://newlifex.com/stone.jpg)

---
> 若本文档未覆盖你的使用场景，欢迎提交 Issue；一起让文档更完善！