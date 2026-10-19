# NewLife.Redis — High-Performance Redis Client Component

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Redis?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Redis?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Redis/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Redis/actions/workflows/test.yml)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Redis?logo=github)](https://github.com/NewLifeX/NewLife.Redis/graphs/contributors)

<!-- lang:start -->
🌐 [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` is a **high-performance / high-throughput / easy-to-integrate** .NET Redis client component built by the NewLife team. Since 2017, it has been running stably on multiple production platforms handling billions of records and high concurrency, processing **80+ billion** command calls daily.

> 📖 Full Documentation: [Requirements (中文)](Doc/需求文档.md) | [Architecture (中文)](Doc/架构文档.md) | [Feature List (中文)](Doc/功能清单.md) | [Competitive Analysis (中文)](Doc/竞品分析.md) | [Garnet Compatibility (中文)](Doc/Garnet兼容性.md) | [AOT Compatibility (中文)](Doc/AOT兼容性分析.md)

**Contents**: [Quick Start](#quick-start) · [Main Features](#main-features) · [Ecosystem Map](#ecosystem-map) · [Documentation](#documentation) · [Contributing](#contributing) · [Team](#team) · [License](#license)

## Quick Start

### Install

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### Basic usage

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

### Platform support

| Item | Coverage |
| --- | --- |
| Target frameworks | One package: net45, net461, netstandard2.0, netstandard2.1, net8.0; the extensions add netcoreapp3.1 → net10 |
| OS and architecture | Windows, Linux, macOS, Android, iOS; x64, arm64, loongarch64, riscv64, s390x |
| NativeAOT | `net8.0` ships with `IsAotCompatible` checks; `AotSmoke/` plus the CI smoke matrix cover win-x64 / linux-x64 / linux-arm64 / linux-la64 (LA64 via QEMU), with win-arm64, macOS and musl RIDs compiled natively. linux-x64 is the blocking gate, the others are under observation — see [AOT Compatibility](Doc/AOT兼容性分析.md) |

## Main Features

<details>
<summary><b>Core Engine</b> (7)</summary>

| Feature | Key characteristics |
| --- | --- |
| Battle-tested at scale | 200+ Redis instances in production, with a daily peak of 100M+ business object writes and 80B+ command calls. |
| Low latency | A single Get/Set round trip takes 200–600µs including network. |
| High throughput | Built-in connection pool (up to 100,000 concurrent) with a synchronous RESP parser and optional auto-pipeline batching. |
| Auto retry and multi-address failover | Comma-separated node lists, fast failover on network errors and a 10-second shielding window by default. |
| Pluggable encoder | JSON by default (`RedisJsonEncoder`), extensible to binary serialization through `IPacketEncoder`. |
| APM tracing | `ITracer` distributed tracing plus `PerfCounter` performance counters. |
| Zero heavy dependencies | Only the NewLife ecosystem (logging, configuration, serialization, security) on top of the BCL. |

</details>

<details>
<summary><b>Data Structures</b> (10)</summary>

| Structure | Key characteristics |
| --- | --- |
| String KV + batch | MGET / MSET / INCR / APPEND / GETRANGE and friends. |
| List | LPUSH / RPOP / LRANGE / LMOVE. |
| Hash | HGET / HSET / HMGET / HSCAN. |
| Set | SADD / SMEMBERS / SUNION / SDIFF. |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN. |
| Stack | LIFO wrapper on top of List. |
| Stream | Full XADD / XREAD / XGROUP / XACK. |
| Geo | GEOADD / GEODIST / GEOSEARCH. |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE. |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD (planned). |

</details>

<details>
<summary><b>Message Queues and Pub/Sub</b> (7)</summary>

| Type | Key characteristics |
| --- | --- |
| `RedisQueue<T>` | Simple queue with at-most-once delivery. |
| `RedisReliableQueue<T>` | At-least-once delivery with RPOPLPUSH + Ack rollback. |
| `RedisDelayQueue<T>` | Delayed delivery backed by a Sorted Set. |
| `RedisStream<T>` | Ordered stream with consumer groups and XACK. |
| `MultipleConsumerGroupsQueue<T>` | Broadcast to multiple consumer groups. |
| `PubSub` + `RedisEventBus` | Real-time publish/subscribe. |
| RedLock distributed lock | Multi-instance Redlock algorithm (in progress). |

</details>

<details>
<summary><b>Clustering and High Availability</b> (7)</summary>

| Mode | Key characteristics |
| --- | --- |
| Standalone | Direct single-instance connection with the smallest footprint. |
| Master-Replica replication | Read/write separation and failover. |
| Sentinel | Sentinel discovery with automatic master switch. |
| Native Cluster | CRC16 slot routing with MOVED / ASK redirection. |
| kvrocks | Auto-adapts to the kvrocks storage engine. |
| Garnet | Auto-detects Microsoft Garnet and degrades gracefully when a feature is missing. |
| Valkey | Detected from server INFO. |

</details>

<details>
<summary><b>Protocol and Server Compatibility</b> (7)</summary>

| Capability | Key characteristics |
| --- | --- |
| RESP2 | Simple string, error, integer, bulk string and array fully implemented. |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error, negotiated through the HELLO handshake (Redis 6.0+). |
| Redis 2.8 – 7.x | Mainstream commands of every version, including COPY / GETDEL / GETEX / GEOSEARCH from 6.2. |
| ACL authentication | Redis 6.0+ user name plus password authentication. |
| Transactions | MULTI / EXEC / WATCH. |
| Lua scripts | EVAL / EVALSHA. |
| Redis Functions | FCALL / FUNCTION (planned). |

</details>

<details>
<summary><b>Cloud, Security and Observability</b> (8)</summary>

| Capability | Key characteristics |
| --- | --- |
| Alibaba Cloud KVStore / Tair | Standard edition, ACL, SSL and proxy-mode guidance plus Tair structures (TairString / TairHash / TairZSet). |
| Tencent Cloud Redis | Uses the instance ID as the user name (`instanceId:password`). |
| Huawei Cloud DCS | Master/standby, SSL and cluster edition differences (no SELECT in cluster mode). |
| TLS/SSL | Configurable protocol version and mutual TLS with client certificates. |
| Structured logging | Component log (`Log`) and protocol-level log (`ClientLog`). |
| Performance counters | `PerfCounter` reports ops/s, latency and error rate. |
| APM tracing | `ITracer` spans everywhere plus an `ActivitySource` (net8.0+) for OpenTelemetry. |
| Connection-string protection | `ProtectedKey` decrypts passwords embedded in connection strings. |

</details>

<details>
<summary><b>Platform and Integration</b> (5)</summary>

| Capability | Key characteristics |
| --- | --- |
| Multi-target | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0; extension packages cover netcoreapp3.1 → net10. |
| Strong naming | Signed with `newlife.snk` so assemblies can be rebuilt and replaced. |
| NativeAOT | `IsAotCompatible` static checks, the `AotSmoke/` project and a multi-RID CI matrix. |
| ASP.NET Core package | `NewLife.Redis.Extensions` registers `IDistributedCache` and `FullRedis` through `AddRedisCaching`. |
| DataProtection | `PersistKeysToRedis` persists data-protection keys in Redis. |

</details>

## Ecosystem Map

NewLife is a layered ecosystem of independently versioned open-source projects. The map below is shared verbatim by every NewLife repository; each project links to its repository and its primary NuGet package.

| Project | What it does | Downloads |
| --- | --- | --- |
| **Foundation** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Logging, configuration, caching, networking, serialization, scheduling and APM tracing. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Data middleware: single tables at the tens-of-billions scale, automatic sharding, read-write splitting, 10+ databases. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | High-performance network library: 1.77M pps / 14.5 Gbps on a single machine (TCP 1KB, 2026-10), 4M long-lived TCP connections. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Protocol communication and RPC framework for client/server applications and IoT devices. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installs .NET applications as OS daemons: Windows services and Linux systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` project templates for console, web, API, service, data and IoT projects. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Data & Messaging** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis client with microsecond latency, millions of ops/s and built-in message queues. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Pure managed RocketMQ client for Apache RocketMQ and Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT client and server, including Alibaba Cloud IoT connectivity. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Hybrid database in C#: embedded or networked, relational, time-series, message queue and KV in one. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql driver and helper layer built on the NewLife data stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI & Distributed** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Rapid admin-platform framework: permissions, SSO, OAuth server and CRUD scaffolding. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Distributed service platform: node management, APM, configuration center, registry and release center. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Distributed task and real-time data scheduling platform with sharding, retries and backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Unified client for 46 LLM providers plus ChatAI, a ready-to-run web chat application. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrial & IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT standard library defining protocol contracts and device abstractions. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII implementation on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Siemens PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Omron PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Mitsubishi Melsec PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC client for industrial data access. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet building-automation protocol stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Lightweight open-source IoT platform with gateway and device management. | — |
| **Vehicle Telematics** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 vehicle positioning protocol: 70+ message bodies, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 platform-to-platform protocol for transport authorities. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 in-vehicle video: RTP parsing, FLV/fMP4 remuxing and live streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Domain Components** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | One package for Excel, Word, PPT, PDF and 15+ document formats. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Cross-platform audio: codecs, device I/O, DSP, containers and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Video codecs, container formats, frame processing and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Unified interface over Baidu, AMap, Tencent and Tianditu with geocoding and coordinate conversion. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 geolocation with an offline memory-mapped database and an online API fallback. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Tools & Products** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Developer toolbox: network, serial port, crypto, regex, Modbus and MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Self-hosted Git source code management system. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installs the matching .NET runtime automatically when launched from an older host. | — |

> Badges show the primary package of each project; theme, extension and satellite packages are listed on their NuGet pages.The 30+ packages not listed here (FTP, LoRa, Office extensions, protocol drivers and more) are available under the [NewLifeX organization](https://github.com/NewLifeX) and on [newlifex.com](https://newlifex.com). This table is maintained in the NewLife.Core README.

## Documentation

**NewLife.Redis** provides .NET developers with full-lifecycle Redis access:

- **Complete Protocol**: Implements RESP2, covers all mainstream commands from Redis 2.8 to 7.x, supports all data structures: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog.
- **Message Queues**: Built-in simple queue, **reliable queue (RPOPLPUSH + Ack semantics)**, delay queue, and Stream multi-consumer-group queue — ready to use, no need to reinvent the wheel.
- **Clustering & HA**: Supports Standalone / Master-Replica / Sentinel / Native Cluster, with automatic failover.
- **Cloud Provider Adapters**: Specialized configuration and adaptation for **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**, covering proxy mode, ACL authentication, SSL encryption, special auth formats, and other differences.
- **Compatible Implementations**: Auto-detects Microsoft Garnet, kvrocks, and other Redis-compatible servers, gracefully degrades with clear alternatives when features are unsupported.
- **Broad Framework Support**: A single package covers **.NET Framework 4.5 to .NET 10+**, suitable for industrial control, government, IoT, and other legacy scenarios.

### Guides

- [Requirements (中文)](Doc/需求文档.md): vision, goals and functional requirements
- [Architecture (中文)](Doc/架构文档.md): layering, components and key design decisions
- [Feature list (中文)](Doc/功能清单.md): implementation, test and comment status of every feature
- [Competitive analysis (中文)](Doc/竞品分析.md): comparison with mainstream Redis clients
- [Garnet compatibility (中文)](Doc/Garnet兼容性.md): supported scope and graceful degradation
- [AOT compatibility (中文)](Doc/AOT兼容性分析.md): NativeAOT scope and measured conclusions
- [Localization (中文)](Doc/多语言本地化.md): message model for exceptions and logs
- [RedisSwitch integration (中文)](Doc/RedisSwitch项目说明.md): zero-downtime Redis switch tool
- [GC allocation analysis (中文)](Doc/dotMemory分析Redis的GC分配.md): dotMemory measurements
- [Official site](https://newlifex.com/core/redis): component page and online documentation

### Protocol and version support

#### RESP Protocol Support

| Protocol | Status | Description |
|----------|--------|-------------|
| RESP2 | ✅ Fully supported | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ Fully supported | Map / Set / Double / Boolean / Null / Blob Error types, negotiated via HELLO handshake (Redis 6.0+) |

#### Redis Version Feature Support

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

### Architecture

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
│  RedisStack<T> · RedisGeo · HyperLogLog · RedisRedLock         │
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

### Usage examples

Every example below runs against a live `FullRedis` instance created in [Quick Start](#quick-start).

<details>
<summary><b>Basic operations</b></summary>

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

// Key prefix (FullRedis.Prefix)
var scoped = rds.GetPrefixed("myapp:");
scoped.Set("user", "Alice");  // Actual key is myapp:user
```

</details>

<details>
<summary><b>Data structures</b></summary>

#### List

```csharp
var list = rds.GetList<String>("queue:demo");
list.Add("job1");
list.Add("job2");
var first = list[0];
var popped = list.RightPop();
```

#### Hash

```csharp
var hash = rds.GetDictionary<String>("user:1001");
hash["Name"] = "Alice";
hash["Email"] = "alice@example.com";
var name = hash["Name"];
var all = hash.GetAll();
```

#### Set / Sorted Set

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

#### Stack

```csharp
var stack = rds.GetStack<String>("undo:ops");
stack.Push("op1");
stack.Push("op2");
var last = stack.Pop();   // "op2"
```

#### Geo

```csharp
var geo = rds.GetGeo("locations");
geo.Add("Beijing",  116.4074, 39.9042);
geo.Add("Shanghai", 121.4737, 31.2304);
var dist = geo.GetDistance("Beijing", "Shanghai", "km");
var nearby = geo.Search("Beijing", 500, "km");
```

#### HyperLogLog

```csharp
var hll = rds.GetHyperLogLog("uv:today");
hll.Add("user1", "user2", "user3");
var count = hll.Count;  // Approximate cardinality
hll.Merge("uv:yesterday");
```

> Note: The base `Redis` instance only supports KV operations. Use `FullRedis` for advanced collections.

</details>

<details>
<summary><b>Pipeline and auto batching</b></summary>

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

</details>

<details>
<summary><b>Message queues</b></summary>

#### Simple Queue

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### Reliable Queue (at-least-once semantics)

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

#### Delay Queue

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### Stream Queue (Redis 5.0+, multi-consumer-group)

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

</details>

<details>
<summary><b>Publish / Subscribe</b></summary>

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

</details>

<details>
<summary><b>Clustering and high availability</b></summary>

#### Multi-Address Auto Failover

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### Auto Cluster Mode Detection

```csharp
// When AutoDetect=true, automatically detect the working mode via INFO
// ⚠️ For public cloud proxy mode, keep the default false to avoid obtaining internal node addresses
var rds = new FullRedis("sentinel-host:26379", "pass", 0)
{
    AutoDetect = true,    // Auto-detect Cluster / Sentinel / Replication
};
```

</details>

<details>
<summary><b>Cloud provider adapters</b></summary>

#### Alibaba Cloud KVStore / Tair

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

#### Tencent Cloud Redis

```csharp
// Tencent Cloud standard/cluster edition: use instance ID as UserName (AUTH instanceId:password format)
var rds = new FullRedis("xxxxxx.redis.tencentcds.com:6379", "instanceId", "YourPassword", 0);

// Or via connection string
var rds = FullRedis.Create("server=xxxxxx.redis.tencentcds.com:6379;username=instanceId;password=YourPassword;db=0");
```

#### Huawei Cloud DCS

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

</details>

<details>
<summary><b>Serialization and encoders</b></summary>

Default encoder: `RedisJsonEncoder` (built-in JSON host), ready to use out of the box.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

Primitive types (`String` / `Int32` / `Int64` / `Double` / `Boolean`) are serialized directly without going through JSON for maximum performance.

</details>

<details>
<summary><b>Security and authentication</b></summary>

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

</details>

<details>
<summary><b>Observability</b></summary>

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

// OpenTelemetry native tracing (net8.0+): subscribe to the built-in ActivitySource
// builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource(Redis.ActivitySourceName));
```

</details>

<details>
<summary><b>Extension packages</b></summary>

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

</details>

<details>
<summary><b>Garnet compatibility</b></summary>

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

</details>

### Reference

<details>
<summary><b>Feature completion status</b></summary>

> Legend: ✅ Completed | 🔄 Planned | ❌ Explicitly Not Planned

#### Data Structures

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

#### Message Queues

| Queue Type | Status | Delivery Semantics |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | At most once |
| `RedisReliableQueue<T>` | ✅ | At least once (RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | Delayed delivery (Sorted Set) |
| `RedisStream<T>` | ✅ | Ordered + Consumer Group + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | Multi-consumer-group broadcast |
| `PubSub` + `RedisEventBus` | ✅ | Real-time push |
| RedLock Distributed Lock | 🔄 | Multi-instance Redlock algorithm |

#### Cluster Support

| Mode | Status |
|------|--------|
| Standalone | ✅ |
| Master-Replica Replication | ✅ |
| Sentinel | ✅ |
| Native Cluster | ✅ |
| kvrocks Auto-Adapt | ✅ |
| Garnet Auto-Detect | ✅ |
| Valkey Auto-Detect | ✅ |

#### Protocol & Advanced Features

| Feature | Status |
|---------|--------|
| RESP2 Full Support | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| ACL Authentication | ✅ |
| Transactions MULTI/EXEC/WATCH | ✅ |
| Lua Scripts EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 Planned |
| Pipeline (Explicit + Auto) | ✅ |
| Pattern Subscription PSUBSCRIBE | 🔄 |
| UNLINK Async Delete | 🔄 |

</details>

<details>
<summary><b>Performance benchmarks</b></summary>

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

</details>

<details>
<summary><b>Best practices</b></summary>

- **Singleton reuse**: `FullRedis` has an internal connection pool; strongly recommend using a single instance for the entire application lifetime — do not `new` each time
- **Key design**: Use `:` as a hierarchical separator, e.g., `user:1001:profile`; use `Prefix` for namespace isolation in production
- **Batch first**: Prefer `GetAll/SetAll` over looping single-key operations; use `AutoPipeline` to auto-merge write operations
- **Value size**: Keep values within 1–2KB; split large objects / compress / use Hash for field-level storage
- **Reliable consumption**: Use `RedisReliableQueue<T>` in production rather than a simple List to avoid message loss
- **Cloud providers**: Keep `AutoDetect = false` on public clouds to avoid internal node address issues
- **Monitoring**: Enable `Counter` statistics in production; only enable `ClientLog` when debugging to avoid hot-path logging overhead
- **SSL**: Enable TLS encrypted transport on public clouds and in production environments

</details>

<details>
<summary><b>FAQ</b></summary>

**Q: Does it support Redis Cluster sharding?**
A: Yes. `FullRedis` auto-detects Cluster mode with `AutoDetect=true`. CRC16 routing, MOVED/ASK redirections are all implemented. For public cloud proxy mode, keep the default `AutoDetect=false`.

**Q: How does it compare to StackExchange.Redis?**
A: NewLife.Redis uses a connection-pool model (vs. StackExchange's multiplexing) for more intuitive connection behavior. Built-in message queue wrappers (reliable queue / delay queue) are a key differentiator. It also supports .NET Framework 4.5. See [Competitive Analysis (中文)](Doc/竞品分析.md).

**Q: How to handle slow queries caused by large values?**
A: Split data structures (e.g., Hash per-field) + batch reads/writes + binary encoder + control `MaxMessageSize` (default 1MB limit).

**Q: What if the connection pool runs out?**
A: The default max is 100,000 connections; normal workloads won't exhaust it. The pool is configurable via `Redis.PoolConfig` (`RedisPoolConfig`): min idle / max capacity / idle cleanup time / max lifetime / borrow wait timeout. Connections idle beyond `IdleTime` are proactively PING-checked when leased; for master-slave switch or rolling proxy update scenarios, lower `MaxLifetime` to recycle connections faster. If you see wait timeouts, check for connection leaks (missing `Dispose`) or operations that hold connections for excessively long periods.

**Q: Why do connections fail on Alibaba Cloud / Tencent Cloud?**
A: Check: ① Is `AutoDetect` set to `false` (correct default)? ② Is the auth format correct (Tencent Cloud requires `username=instanceId`)? ③ Are the network security group / whitelist allowing the client IP? ④ Does the cluster edition prohibit using SELECT to switch databases?

**Q: Does Garnet support all features?**
A: Basic operations, List/Hash/Set/ZSet/HLL/Geo, and queues are all supported. Stream is not yet supported and will automatically throw `NotSupportedException` with a suggestion to use `RedisQueue<T>` as an alternative.

</details>

<details>
<summary><b>Roadmap</b></summary>

- [x] ~~RESP3 protocol support~~ (Completed)
- [ ] Redis Functions (FCALL/FUNCTION, replacing Lua scripts)
- [ ] LMPOP / ZMPOP (Redis 7.0 batch pop)
- [ ] RedLock multi-instance distributed lock
- [ ] Bitmap / Bitfield operations
- [ ] Pattern subscription PSUBSCRIBE full implementation
- [ ] UNLINK async delete
- [ ] Prometheus metrics export adapter
- [x] Alibaba Cloud Tair Extension (TairString / TairHash / TairZSet supported)
- [ ] Huawei Cloud DCS cluster edition node address mapping adapter
- [x] ~~Pub/Sub~~ (Completed)
- [x] ~~RedisStream multi-consumer-group~~ (Completed)
- [x] ~~Garnet server type detection~~ (Completed)

Feedback and feature requests welcome via Issue / PR.

</details>

<details>
<summary><b>Competitive comparison</b></summary>

| Capability | NewLife.Redis | StackExchange.Redis | FreeRedis | Notes |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | Critical for industrial/gov legacy systems |
| Connection Model | Connection Pool | Multiplexing | Connection Pool | Each has its use cases |
| Command Coverage | ~98% | ~95% | ~98% | All documented commands except explicitly unplanned ones |
| Built-in Message Queue Wrappers | ✅ Complete | ❌ | 🔄 Partial | Unique reliable/delay queues |
| Built-in Serialization | ✅ | ❌ Manual | ❌ Manual | Reduces boilerplate code |
| Cloud Provider Specialized Adapters | ✅ (Alibaba/Tencent/Huawei) | Generic | Generic | Dedicated documentation |
| Garnet Explicit Adapter | ✅ | ❌ | ❌ | Type detection + graceful degradation |
| kvrocks Adapter | ✅ | ❌ | ❌ | Auto-detection |
| Valkey Auto-Detect | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| APM Tracing | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | Native OpenTelemetry integration |
| RESP3 | ✅ | ✅ | ✅ | Verified via RESP3 test suite |

See [Competitive Analysis Document (中文)](Doc/竞品分析.md) for details.

</details>

### Release notes

- [ChangeLog.md](ChangeLog.md): version history (Chinese)
- [ChangeLog.en.md](ChangeLog.en.md): version history (English)

## Contributing

### Build and test

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- Integration tests need a live Redis: set `REDIS_CONNECTION` or start one on `127.0.0.1:6379`; otherwise tests marked `[RedisFact]` / `[RedisTopologyFact]` are skipped automatically.
- NativeAOT smoke: `AotSmoke/`, CI in `.github/workflows/aot.yml`.
- Benchmarks: `Samples/Benchmark`.

### Repository layout

| Directory | Description |
| --- | --- |
| `NewLife.Redis/` | Client sources: connection pool, RESP codec, data structures and queues |
| `NewLife.Redis.Extensions/` | ASP.NET Core integration: `IDistributedCache`, DI and data protection |
| `NewLife.Extensions.Caching.Redis/` | Legacy caching extension compatibility package |
| `XUnitTest/` | xUnit test suite (net10.0) |
| `AotSmoke/` | NativeAOT smoke project |
| `Samples/` | Benchmark, WebDemo, QueueDemo, RedisSwitch and mobile smoke samples |
| `Doc/` | Requirements, architecture, feature list and topic documents |

### Release

Push a `v`-prefixed tag (for example `v6.6.2026.0802`) to trigger `.github/workflows/publish.yml`. The workflow packs `NewLife.Redis` and `NewLife.Redis.Extensions` and publishes them to NuGet.org and GitHub Packages; a manual run only publishes packages.

### Feedback

1. Before submitting, make sure `dotnet build` and the relevant tests pass, and follow the repository code style and commit conventions
2. PRs: Keep changes minimal, add necessary comments and test descriptions
3. Issues: Provide version, runtime environment, and a minimal reproduction scenario

Community: QQ groups 1600800 / 1600838; GitHub Discussions / Issues.

## Team

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

Founded in 2002, the NewLife team has spent more than two decades on the .NET and IoT ecosystem, maintains nearly 100 open-source projects (34 of them core to this map), and delivers integrated software and hardware solutions for power, logistics, healthcare, industrial control and internet industries. We live by the motto "learning has no seniority — mastery earns respect".

Open-source components have been downloaded <!--nuget-total:start-->6.48M<!--nuget-total:end--> times from NuGet (ecosystem-wide, as of 2026-10).

Website: https://newlifex.com · Open source organization: https://github.com/NewLifeX

<details>
<summary>WeChat Official Account: 智能大石头 (scan to follow)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## License

MIT License. Free to use commercially / modify / redistribute (no additional authorization required). Just retain the copyright notice.

> If this document doesn't cover your use case, feel free to submit an Issue — let's improve the documentation together!
