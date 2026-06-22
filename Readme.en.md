# NewLife.Redis — High-Performance Redis Client Component

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Redis?label=dev%20nuget&logo=nuget)

## [[中文]](https://github.com/NewLifeX/NewLife.Redis/blob/master/Readme.MD)

`NewLife.Redis` is a **high-performance / high-throughput / easy-to-integrate** .NET Redis client component built by the NewLife team. Since 2017, it has been running stably on multiple production platforms handling billions of records and high concurrency, processing **80+ billion** command calls daily.

> 📖 Full Documentation: [Requirements (中文)](Doc/需求文档.md) | [Architecture (中文)](Doc/架构文档.md) | [Competitive Analysis (中文)](Doc/竞品分析.md) | [Garnet Compatibility (中文)](Doc/Garnet兼容性.md)

---

## Table of Contents
- [NewLife.Redis — High-Performance Redis Client Component](#newliferedis--high-performance-redis-client-component)
  - [\[中文\]](#中文)
  - [Table of Contents](#table-of-contents)
  - [Introduction](#introduction)
  - [Core Features](#core-features)
  - [Redis Protocol & Version Support](#redis-protocol--version-support)
    - [RESP Protocol Support](#resp-protocol-support)
    - [Redis Version Feature Support](#redis-version-feature-support)
  - [Architecture & Module Breakdown](#architecture--module-breakdown)
  - [Installation & Quick Start](#installation--quick-start)
  - [Basic Usage](#basic-usage)
  - [Data Structure Operations](#data-structure-operations)
    - [List](#list)
    - [Hash](#hash)
    - [Set / Sorted Set](#set--sorted-set)
    - [Stack](#stack)
    - [Geo](#geo)
    - [HyperLogLog](#hyperloglog)
  - [Pipeline & Auto Batching](#pipeline--auto-batching)
  - [Message Queues](#message-queues)
    - [Simple Queue](#simple-queue)
    - [Reliable Queue (at-least-once semantics)](#reliable-queue-at-least-once-semantics)
    - [Delay Queue](#delay-queue)
    - [Stream Queue (Redis 5.0+, multi-consumer-group)](#stream-queue-redis-50-multi-consumer-group)
  - [Publish / Subscribe](#publish--subscribe)
  - [Clustering & High Availability](#clustering--high-availability)
    - [Multi-Address Auto Failover](#multi-address-auto-failover)
    - [Auto Cluster Mode Detection](#auto-cluster-mode-detection)
  - [Cloud Provider Adapters](#cloud-provider-adapters)
    - [Alibaba Cloud KVStore / Tair](#alibaba-cloud-kvstore--tair)
    - [Tencent Cloud Redis](#tencent-cloud-redis)
    - [Huawei Cloud DCS](#huawei-cloud-dcs)
  - [Serialization & Encoders](#serialization--encoders)
  - [Security & Authentication](#security--authentication)
  - [Observability](#observability)
  - [Extension Packages](#extension-packages)
  - [Garnet Compatibility](#garnet-compatibility)
  - [Feature Completion Status](#feature-completion-status)
    - [Data Structures](#data-structures)
    - [Message Queues](#message-queues-1)
    - [Cluster Support](#cluster-support)
    - [Protocol & Advanced Features](#protocol--advanced-features)
  - [Performance Benchmarks](#performance-benchmarks)
  - [Best Practices](#best-practices)
  - [FAQ](#faq)
  - [Roadmap](#roadmap)
  - [Competitive Comparison](#competitive-comparison)
  - [NewLife Project Matrix](#newlife-project-matrix)
  - [Contributing & Community](#contributing--community)
  - [License](#license)
  - [NewLife Development Team](#newlife-development-team)

---

## Introduction

**NewLife.Redis** provides .NET developers with full-lifecycle Redis access:

- **Complete Protocol**: Implements RESP2, covers all mainstream commands from Redis 2.8 to 7.x, supports all data structures: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog.
- **Message Queues**: Built-in simple queue, **reliable queue (RPOPLPUSH + Ack semantics)**, delay queue, and Stream multi-consumer-group queue — ready to use, no need to reinvent the wheel.
- **Clustering & HA**: Supports Standalone / Master-Replica / Sentinel / Native Cluster, with automatic failover.
- **Cloud Provider Adapters**: Specialized configuration and adaptation for **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**, covering proxy mode, ACL authentication, SSL encryption, special auth formats, and other differences.
- **Compatible Implementations**: Auto-detects Microsoft Garnet, kvrocks, and other Redis-compatible servers, gracefully degrades with clear alternatives when features are unsupported.
- **Broad Framework Support**: A single package covers **.NET Framework 4.5 to .NET 10+**, suitable for industrial control, government, IoT, and other legacy scenarios.

---

## Core Features

- Battle-tested at scale: 200+ Redis instances, daily peak of 100M+ business object writes / 80B+ command calls
- Low latency: Single Get/Set round-trip 200–600µs (including network)
- High throughput: Built-in connection pool (max 100,000 concurrent) + efficient synchronous RESP protocol parsing + optional auto-pipeline batching
- Auto retry & multi-address failover: Server supports comma-separated nodes, rapid failover on network errors, default 10-second shielding window
- Rich advanced structures: List / Hash / Set / SortedSet / Stack / Geo / HyperLogLog / Stream
- Multiple message queue paradigms: Simple / Reliable / Delay / Stream / Multi-consumer-group
- Pluggable encoder: Default JSON, extensible to binary serialization
- APM tracing: Supports `ITracer` distributed tracing + `PerfCounter` performance counters
- Strong-named + multi-target: net45 / net461 / netstandard2.0 / netstandard2.1 / (extensions include netcoreapp3.1 → net10)
- Zero heavy external dependencies, fully leverages the NewLife ecosystem (logging, config, serialization, security)

---

## Redis Protocol & Version Support

### RESP Protocol Support

| Protocol | Status | Description |
|----------|--------|-------------|
| RESP2 | ✅ Fully supported | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | 🔄 Planned | Map / Set / Double types, requires HELLO handshake negotiation |

### Redis Version Feature Support

| Redis Version | Key Features | Status |
|---------------|--------------|--------|
| 2.8 | Basic commands, SCAN, HyperLogLog, Pub/Sub | ✅ Fully supported |
| 3.0 | Native Cluster, CRC16 shard routing | ✅ Fully supported |
| 3.2 | GEO spatial data | ✅ Fully supported |
| 4.0 | UNLINK async delete, MODULE | 🔄 Partial support |
| 5.0 | Stream data structure (full XADD/XREAD/XGROUP/XACK) | ✅ Fully supported |
| 6.0 | ACL access control, TLS/SSL, LMOVE, RESET | ✅ Fully supported |
| 6.2 | COPY, GETDEL, GETEX, GEOSEARCH | ✅ Core commands supported |
| 7.0 | Functions (FCALL), LMPOP/ZMPOP, sharded Pub/Sub | 🔄 Planned |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 Planned |

---

## Architecture & Module Breakdown

```
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                            │
│  ASP.NET Core / Console / WinForms / IoT / Worker Service        │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│          Extension Integration (NewLife.Redis.Extensions)        │
│  IDistributedCache · IDataProtection · AddRedisCaching DI        │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                 Advanced Features (FullRedis)                    │
│  RedisList<T> · RedisHash<T> · RedisSet<T> · RedisSortedSet<T>  │
│  RedisStack<T> · RedisGeo · HyperLogLog · PrefixedRedis          │
│  RedisQueue<T> · RedisReliableQueue<T> · RedisDelayQueue<T>      │
│  RedisStream<T> · PubSub · RedisEventBus                         │
│  RedisCluster · RedisSentinel · RedisReplication                 │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                  Core Client Layer (Redis)                       │
│  Connection Pool ObjectPool<RedisClient> (Min=10, Max=100,000)   │
│  Multi-Node Failover · Pipeline Mgmt · Encoder · Config Parsing  │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                  Protocol Layer (RedisClient)                    │
│  TCP/TLS Connection · RESP2 Codec · ArrayPool<byte> Buffers      │
└──────────────────────────────┬──────────────────────────────────┘
                               │ TCP/TLS
┌──────────────────────────────▼──────────────────────────────────┐
│  Redis Server / Garnet / kvrocks / Alibaba / Tencent / Huawei   │
└─────────────────────────────────────────────────────────────────┘
```

Key Class Descriptions:

| Class | Role |
|-------|------|
| `Redis` | Core client: connection pool, multi-node failover, pipeline, configuration management |
| `FullRedis` | Enhanced client: cluster routing, key prefix, data structure factory methods |
| `RedisClient` | RESP protocol unit: TCP connection, command encoding/decoding (internal, pool-managed) |
| `RedisList<T>` etc. | Generic data structure wrappers with unified encoding strategy |
| `RedisReliableQueue<T>` | Reliable message queue (RPOPLPUSH + Ack confirmation) |
| `RedisStream<T>` | Stream queue (Redis 5.0+, consumer-group mode) |
| `IPacketEncoder` | Pluggable encoding/decoding strategy |

---

## Installation & Quick Start

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

**Recommended: singleton, thread-safe, reuse throughout the application lifetime:**

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();

var rds = new FullRedis("127.0.0.1:6379", "password", 0)
{
    Log = XTrace.Log,           // Optional: component-level logging
    AutoPipeline = 100,         // Optional: auto-commit pipeline when command count reaches 100
};

// Write an object (auto JSON serialization)
rds.Set("user:1", new { Name = "Alice", Age = 30 }, expire: 3600);

// Read (auto deserialization)
var name = rds.Get<String>("user:1");
Console.WriteLine(name);
```

**Connection string mode (recommended for config centers):**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

---

## Basic Usage

```csharp
// Write / Read
rds.Set("k1", 123, expire: 600);
var v = rds.Get<Int32>("k1");

// Set only if key does not exist
var ok = rds.Add("k2", "init");

// Replace and return old value
var old = rds.Replace("k2", "new");

// Counters
rds.Increment("counter", 1);
rds.Decrement("counter", 2);

// Expiry management
rds.SetExpire("k1", TimeSpan.FromMinutes(10));
var ttl = rds.GetExpire("k1");

// Batch read/write
rds.SetAll(new Dictionary<String, Object> { ["a"] = 1, ["b"] = 2 }, expire: 300);
var dict = rds.GetAll<Int32>(["a", "b"]);

// Key prefix (PrefixedRedis)
var scoped = rds.GetPrefixed("myapp:");
scoped.Set("user", "Alice");  // Actual key is myapp:user
```

---

## Data Structure Operations

### List

```csharp
var list = rds.GetList<String>("queue:demo");
list.Add("job1");
list.Add("job2");
var first = list[0];
var popped = list.RightPop();
```

### Hash

```csharp
var hash = rds.GetDictionary<String>("user:1001");
hash["Name"] = "Alice";
hash["Email"] = "alice@example.com";
var name = hash["Name"];
var all = hash.GetAll();
```

### Set / Sorted Set

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

### Stack

```csharp
var stack = rds.GetStack<String>("undo:ops");
stack.Push("op1");
stack.Push("op2");
var last = stack.Pop();   // "op2"
```

### Geo

```csharp
var geo = rds.GetGeo("locations");
geo.Add("Beijing",  116.4074, 39.9042);
geo.Add("Shanghai", 121.4737, 31.2304);
var dist = geo.GetDistance("Beijing", "Shanghai", "km");
var nearby = geo.Search("Beijing", 500, "km");
```

### HyperLogLog

```csharp
var hll = rds.GetHyperLogLog("uv:today");
hll.Add("user1", "user2", "user3");
var count = hll.Count;  // Approximate cardinality
hll.Merge("uv:yesterday");
```

> Note: The base `Redis` instance only supports KV operations. Use `FullRedis` for advanced collections.

---

## Pipeline & Auto Batching

Pipeline significantly reduces network round-trips (RTT) and improves bulk operation throughput:

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**Auto mode** (recommended for production):

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

---

## Message Queues

### Simple Queue

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

### Reliable Queue (at-least-once semantics)

```csharp
var queue = rds.GetReliableQueue<OrderMsg>("orders");

// Produce
queue.Add(new OrderMsg { OrderId = "12345" });

// Consume + Acknowledge
var msg = queue.TakeOne(timeout: 30);
if (msg != null)
{
    // Process the message...
    queue.Acknowledge(msg);   // Acknowledge consumption; removed from Ack queue
}
// If the process crashes without acknowledging, the message is automatically rolled back
// to the main queue after 60 seconds
```

### Delay Queue

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

### Stream Queue (Redis 5.0+, multi-consumer-group)

```csharp
var stream = rds.GetStream<LogEntry>("logs");
stream.Group = "processor-group";

// Produce
stream.Add(new LogEntry { Level = "INFO", Message = "Application started" });

// Consume (multiple instances compete within the same group)
var entry = stream.TakeOne(timeout: 15);
if (entry != null)
{
    // Process...
    stream.Acknowledge(entry.Id);
}
```

---

## Publish / Subscribe

```csharp
var pubsub = rds.GetPubSub("events:user");

// Publish
pubsub.Publish("login:Alice");

// Subscribe (async loop)
var cts = new CancellationTokenSource();
await pubsub.SubscribeAsync((channel, message) =>
{
    Console.WriteLine($"[{channel}] {message}");
}, cts.Token);
```

---

## Clustering & High Availability

### Multi-Address Auto Failover

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

### Auto Cluster Mode Detection

```csharp
// When AutoDetect=true, automatically detect the working mode via INFO
// ⚠️ For public cloud proxy mode, keep the default false to avoid obtaining internal node addresses
var rds = new FullRedis("sentinel-host:26379", "pass", 0)
{
    AutoDetect = true,    // Auto-detect Cluster / Sentinel / Replication
};
```

---

## Cloud Provider Adapters

### Alibaba Cloud KVStore / Tair

```csharp
// Standard edition (Redis 2.8/4.0/5.0/6.0/7.0)
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6379", "YourPassword", 0);

// Redis 6.0 ACL user authentication
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6379", "YourUser", "YourPassword", 0);

// SSL encrypted connection
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6380", "YourPassword", 0)
{
    SslProtocol = SslProtocols.Tls12,
};

// ⚠️ Public cloud proxy mode: Keep AutoDetect at its default false
// ⚠️ Cluster edition: Proxy handles shard routing; clients don't need to know the internal topology
```

### Tencent Cloud Redis

```csharp
// Tencent Cloud standard/cluster edition: use instance ID as UserName (AUTH instanceId:password format)
var rds = new FullRedis("xxxxxx.redis.tencentcds.com:6379", "instanceId", "YourPassword", 0);

// Or via connection string
var rds = FullRedis.Create("server=xxxxxx.redis.tencentcds.com:6379;username=instanceId;password=YourPassword;db=0");
```

### Huawei Cloud DCS

```csharp
// Master/Standby edition (standard AUTH)
var rds = new FullRedis("xxxxxx.dcs.myhuaweicloud.com:6379", "YourPassword", 0);

// SSL edition (use after downloading the PEM certificate)
var rds = new FullRedis("xxxxxx.dcs.myhuaweicloud.com:6380", "YourPassword", 0)
{
    SslProtocol = SslProtocols.Tls12,
    Certificate = new X509Certificate2("huawei-dcs.pem"),
};

// ⚠️ Cluster edition does not support SELECT; Db must be 0
// ⚠️ AutoDetect = false (default) to avoid internal node address issues
```

**General Cloud Provider Notes:**
- Public cloud proxy mode: Keep `AutoDetect = false` (default) to prevent auto-detection of internal nodes
- Connection quota: Each cloud provider and instance size has connection limits; set the pool `Max` parameter accordingly
- TCP keepalive: Huawei Cloud defaults to a 300-second TCP idle timeout; set `Timeout` below that value and periodically PING

---

## Serialization & Encoders

Default encoder: `RedisJsonEncoder` (built-in JSON host), ready to use out of the box.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

Primitive types (`String` / `Int32` / `Int64` / `Double` / `Boolean`) are serialized directly without going through JSON for maximum performance.

---

## Security & Authentication

```csharp
// Redis 6.0+ ACL authentication
var rds = new FullRedis("host:6379", "username", "password", 0);

// TLS encryption (specify protocol version)
rds.SslProtocol = SslProtocols.Tls12;

// Certificate verification (mutual mTLS)
rds.Certificate = new X509Certificate2("client.pem", "certPassword");

// Connection string password encryption (ProtectedKey mechanism)
// Configure ProtectedKey.Instance.Secret; passwords in connection strings are auto-decrypted
```

---

## Observability

```csharp
// Structured logging
rds.Log       = XTrace.Log;      // Component-level logs (connection changes, cluster changes, retries)
rds.ClientLog = XTrace.Log;      // Protocol-level logs (every command; enable only when debugging)

// Performance counters
rds.Counter = new PerfCounter();
var stats = rds.Counter.GetAll();   // Get ops/s, latency, error rate

// APM distributed tracing (compatible with NewLife.Stardust / OpenTelemetry)
rds.Tracer = DefaultTracer.Instance;
// All Execute / queue produce-consume operations are automatically instrumented, with key as tag
```

---

## Extension Packages

`NewLife.Redis.Extensions` provides deep ASP.NET Core integration:

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

// Automatically registers IDistributedCache + FullRedis as singletons

// Persist data protection keys to Redis
builder.Services.AddDataProtection()
    .PersistKeysToRedis(redis, "DataProtection-Keys");
```

---

## Garnet Compatibility

NewLife.Redis fully supports Microsoft's [Garnet](https://github.com/microsoft/garnet) high-performance Redis-compatible server, with auto-detection and graceful degradation.

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| Feature | Garnet Support Status |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ Fully supported |
| HyperLogLog / Geo | ✅ Fully supported |
| Pub/Sub / Transactions / Lua Scripts | ✅ Fully supported |
| Pipeline | ✅ Fully supported |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ Fully supported |
| `RedisReliableQueue<T>` | ✅ Fully supported |
| `RedisStream<T>` | ❌ Garnet does not support Stream yet; automatically throws `NotSupportedException` with suggested alternatives |

See: [Garnet Compatibility Documentation (中文)](Doc/Garnet兼容性.md)

---

## Feature Completion Status

> Legend: ✅ Completed | 🔄 Planned | ❌ Explicitly Not Planned

### Data Structures

| Structure | Status | Description |
|-----------|--------|-------------|
| String KV + Batch | ✅ | MGET/MSET/INCR/APPEND/GETRANGE, etc. |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE, etc. |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN, etc. |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF, etc. |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN, etc. |
| Stream | ✅ | Full XADD/XREAD/XGROUP/XACK |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH, etc. |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

### Message Queues

| Queue Type | Status | Delivery Semantics |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | At most once |
| `RedisReliableQueue<T>` | ✅ | At least once (RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | Delayed delivery (Sorted Set) |
| `RedisStream<T>` | ✅ | Ordered + Consumer Group + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | Multi-consumer-group broadcast |
| `PubSub` + `RedisEventBus` | ✅ | Real-time push |
| RedLock Distributed Lock | 🔄 | Multi-instance Redlock algorithm |

### Cluster Support

| Mode | Status |
|------|--------|
| Standalone | ✅ |
| Master-Replica Replication | ✅ |
| Sentinel | ✅ |
| Native Cluster | ✅ |
| kvrocks Auto-Adapt | ✅ |
| Garnet Auto-Detect | ✅ |

### Protocol & Advanced Features

| Feature | Status |
|---------|--------|
| RESP2 Full Support | ✅ |
| RESP3 | 🔄 Planned |
| SSL/TLS | ✅ |
| ACL Authentication | ✅ |
| Transactions MULTI/EXEC/WATCH | ✅ |
| Lua Scripts EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 Planned |
| Pipeline (Explicit + Auto) | ✅ |
| Pattern Subscription PSUBSCRIBE | 🔄 |
| UNLINK Async Delete | 🔄 |

---

## Performance Benchmarks

Built-in benchmark in source (`Samples/Benchmark`), typical results (40 logical processors, local Redis 7.0):

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

Manual bench trigger:

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> Actual performance is affected by network RTT / value size / serialization complexity. It is recommended to keep individual values between 1–1.4KB.

---

## Best Practices

- **Singleton reuse**: `FullRedis` has an internal connection pool; strongly recommend using a single instance for the entire application lifetime — do not `new` each time
- **Key design**: Use `:` as a hierarchical separator, e.g., `user:1001:profile`; use `Prefix` for namespace isolation in production
- **Batch first**: Prefer `GetAll/SetAll` over looping single-key operations; use `AutoPipeline` to auto-merge write operations
- **Value size**: Keep values within 1–2KB; split large objects / compress / use Hash for field-level storage
- **Reliable consumption**: Use `RedisReliableQueue<T>` in production rather than a simple List to avoid message loss
- **Cloud providers**: Keep `AutoDetect = false` on public clouds to avoid internal node address issues
- **Monitoring**: Enable `Counter` statistics in production; only enable `ClientLog` when debugging to avoid hot-path logging overhead
- **SSL**: Enable TLS encrypted transport on public clouds and in production environments

---

## FAQ

**Q: Does it support Redis Cluster sharding?**
A: Yes. `FullRedis` auto-detects Cluster mode with `AutoDetect=true`. CRC16 routing, MOVED/ASK redirections are all implemented. For public cloud proxy mode, keep the default `AutoDetect=false`.

**Q: How does it compare to StackExchange.Redis?**
A: NewLife.Redis uses a connection-pool model (vs. StackExchange's multiplexing) for more intuitive connection behavior. Built-in message queue wrappers (reliable queue / delay queue) are a key differentiator. It also supports .NET Framework 4.5. See [Competitive Analysis (中文)](Doc/竞品分析.md).

**Q: How to handle slow queries caused by large values?**
A: Split data structures (e.g., Hash per-field) + batch reads/writes + binary encoder + control `MaxMessageSize` (default 1MB limit).

**Q: What if the connection pool runs out?**
A: The default max is 100,000 connections; normal workloads won't exhaust it. If you see wait timeouts, check for connection leaks (missing `Dispose`) or operations that hold connections for excessively long periods.

**Q: Why do connections fail on Alibaba Cloud / Tencent Cloud?**
A: Check: ① Is `AutoDetect` set to `false` (correct default)? ② Is the auth format correct (Tencent Cloud requires `username=instanceId`)? ③ Are the network security group / whitelist allowing the client IP? ④ Does the cluster edition prohibit using SELECT to switch databases?

**Q: Does Garnet support all features?**
A: Basic operations, List/Hash/Set/ZSet/HLL/Geo, and queues are all supported. Stream is not yet supported and will automatically throw `NotSupportedException` with a suggestion to use `RedisQueue<T>` as an alternative.

---

## Roadmap

- [ ] RESP3 protocol support (foundation for Redis 7.x new features)
- [ ] Redis Functions (FCALL/FUNCTION, replacing Lua scripts)
- [ ] LMPOP / ZMPOP (Redis 7.0 batch pop)
- [ ] RedLock multi-instance distributed lock
- [ ] Bitmap / Bitfield operations
- [ ] Pattern subscription PSUBSCRIBE full implementation
- [ ] UNLINK async delete
- [ ] Prometheus metrics export adapter
- [ ] Alibaba Cloud Tair extended commands (TairString / TairZSet / TairHash)
- [ ] Huawei Cloud DCS cluster edition node address mapping adapter
- [x] ~~Pub/Sub~~ (Completed)
- [x] ~~RedisStream multi-consumer-group~~ (Completed)
- [x] ~~Garnet server type detection~~ (Completed)

Feedback and feature requests welcome via Issue / PR.

---

## Competitive Comparison

| Capability | NewLife.Redis | StackExchange.Redis | FreeRedis | Notes |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | Critical for industrial/gov legacy systems |
| Connection Model | Connection Pool | Multiplexing | Connection Pool | Each has its use cases |
| Command Coverage | ~75% | ~95% | ~98% | Planned to reach 90%+ |
| Built-in Message Queue Wrappers | ✅ Complete | ❌ | 🔄 Partial | Unique reliable/delay queues |
| Built-in Serialization | ✅ | ❌ Manual | ❌ Manual | Reduces boilerplate code |
| Cloud Provider Specialized Adapters | ✅ (Alibaba/Tencent/Huawei) | Generic | Generic | Dedicated documentation |
| Garnet Explicit Adapter | ✅ | ❌ | ❌ | Type detection + graceful degradation |
| kvrocks Adapter | ✅ | ❌ | ❌ | Auto-detection |
| APM Tracing | ITracer interface | ActivitySource | ❌ | Integrable with OpenTelemetry |
| RESP3 | 🔄 | ✅ | ✅ | SE.Redis already supports |

See [Competitive Analysis Document (中文)](Doc/竞品分析.md) for details.

---

## NewLife Project Matrix

| Project | Description |
|---------|-------------|
| [NewLife.Core](https://github.com/NewLifeX/X) | Core library: logging / config / caching / serialization / APM |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Big-data ORM, billions of records + sharding + read/write separation |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Ultra-high-performance networking library (tens of millions throughput) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Distributed service / config / registry / publish center |
| [AntJob](https://github.com/NewLifeX/AntJob) | Distributed computing & scheduling platform |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | RocketMQ pure managed client |

> Full matrix, enterprise solutions, and commercial support: https://newlifex.com

---

## Contributing & Community

1. Before submitting, read the repository `.github/copilot-instructions.md` (coding standards & review checklist)
2. PRs: Keep changes minimal, add necessary comments and test descriptions
3. Issues: Provide version, runtime environment, and a minimal reproduction scenario

Community: QQ groups 1600800 / 1600838; GitHub Discussions / Issues.

---

## License

MIT License. Free to use commercially / modify / redistribute (no additional authorization required). Just retain the copyright notice.

---

## NewLife Development Team

![XCode](https://newlifex.com/logo.png)

Since 2002, the team has maintained 80+ .NET / IoT / distributed open-source projects, with over 4 million cumulative NuGet downloads. Products and components are widely used in electric power, logistics, industrial control, education, telecommunications, cultural heritage, and other industries.

Website: https://newlifex.com | Open Source: https://github.com/NewLifeX

WeChat Official Account:

![智能大石头](https://newlifex.com/stone.jpg)

---

> If this document doesn't cover your use case, feel free to submit an Issue — let's improve the documentation together!
