# NewLife.Redis - 高性能 Redis 客户端组件

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
🌐 [English](README.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` 是新生命团队打造的**高性能 / 高吞吐 / 易集成**的 .NET Redis 客户端组件。自 2017 年起在多个千万 / 百亿级数据与高并发生产平台稳定运行，经受日均 **80+ 亿次**命令调用考验。

> 📖 完整文档：[需求文档](Doc/需求文档.md) | [架构文档](Doc/架构文档.md) | [功能清单](Doc/功能清单.md) | [竞品分析](Doc/竞品分析.md) | [Garnet 兼容性](Doc/Garnet兼容性.md) | [AOT 兼容性](Doc/AOT兼容性分析.md)

> ℹ️ 本中文版与 [English README](README.md) 同批维护；若两者内容不一致，以英文版为对外口径。

**目录**：[快速开始](#快速开始) · [主要功能](#主要功能) · [生态版图](#生态版图) · [文档](#文档) · [参与贡献](#参与贡献) · [新生命开发团队](#新生命开发团队) · [许可证](#许可证)

## 快速开始

### 安装

```bash
# 稳定版
dotnet add package NewLife.Redis
# ASP.NET Core 扩展
dotnet add package NewLife.Redis.Extensions
# 开发版（含预发布）
dotnet add package NewLife.Redis --prerelease
```

### 基础用法

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

### 平台支持

| 项目 | 覆盖范围 |
| --- | --- |
| 目标框架 | 单包：net45、net461、netstandard2.0、netstandard2.1、net8.0；扩展包再覆盖 netcoreapp3.1 → net10 |
| 系统与架构 | Windows、Linux、macOS、Android、iOS；x64、arm64、loongarch64、riscv64、s390x |
| NativeAOT | `net8.0` 常驻 `IsAotCompatible` 静态校验；`AotSmoke/` 与 CI 冒烟矩阵覆盖 win-x64 / linux-x64 / linux-arm64 / linux-la64（LA64 经 QEMU 仿真），另含 win-arm64、macOS 与 musl RID 原生编译。linux-x64 为阻断门禁，其余处于观察期，详见 [AOT 兼容性](Doc/AOT兼容性分析.md) |

## 主要功能

<details>
<summary><b>核心引擎</b>（7 项）</summary>

| 特性 | 关键特点 |
| --- | --- |
| 大规模生产验证 | 200+ Redis 实例，日峰值 1 亿+ 业务对象写入、80+ 亿命令调用。 |
| 低延迟 | 单次 Get/Set 往返 200～600µs（含网络）。 |
| 高吞吐 | 内置连接池（最大 100,000 并发）+ 同步 RESP 解析 + 可选自动管道合并。 |
| 自动重试与多地址切换 | Server 支持逗号分隔多节点，网络异常快速切换，默认 10 秒屏蔽窗口。 |
| 可插拔编码器 | 默认 JSON（`RedisJsonEncoder`），可通过 `IPacketEncoder` 扩展二进制序列化。 |
| APM 追踪 | `ITracer` 链路追踪 + `PerfCounter` 性能计数器。 |
| 零重量级依赖 | 仅依赖 NewLife 生态（日志、配置、序列化、安全）与 BCL。 |

</details>

<details>
<summary><b>数据结构</b>（10 项）</summary>

| 结构 | 关键特点 |
| --- | --- |
| String KV + 批量 | MGET / MSET / INCR / APPEND / GETRANGE 等。 |
| List（列表） | LPUSH / RPOP / LRANGE / LMOVE。 |
| Hash（哈希） | HGET / HSET / HMGET / HSCAN。 |
| Set（集合） | SADD / SMEMBERS / SUNION / SDIFF。 |
| Sorted Set（有序集合） | ZADD / ZRANGE / ZPOPMIN / ZSCAN。 |
| Stack（栈） | 基于 List 的 LIFO 封装。 |
| Stream（流） | XADD / XREAD / XGROUP / XACK 全套。 |
| Geo（地理信息） | GEOADD / GEODIST / GEOSEARCH。 |
| HyperLogLog（基数统计） | PFADD / PFCOUNT / PFMERGE。 |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD（规划中）。 |

</details>

<details>
<summary><b>消息队列与发布订阅</b>（7 项）</summary>

| 类型 | 关键特点 |
| --- | --- |
| `RedisQueue<T>` 简单队列 | 至多一次投递，基于 List。 |
| `RedisReliableQueue<T>` 可靠队列 | 至少一次投递，RPOPLPUSH + Ack 回滚。 |
| `RedisDelayQueue<T>` 延迟队列 | 基于 Sorted Set 的定时投递。 |
| `RedisStream<T>` 流队列 | 有序流 + 消费组 + XACK。 |
| `MultipleConsumerGroupsQueue<T>` 多消费组 | 多消费组广播。 |
| `PubSub` + `RedisEventBus` | 实时发布订阅。 |
| RedLock 分布式锁 | 多实例 Redlock 算法（进行中）。 |

</details>

<details>
<summary><b>集群与高可用</b>（7 项）</summary>

| 模式 | 关键特点 |
| --- | --- |
| Standalone 单机 | 直连单实例，依赖最少。 |
| 主从复制 | 读写分离与故障切换。 |
| Sentinel 哨兵 | 哨兵发现与自动主从切换。 |
| Cluster 原生集群 | CRC16 分片路由，MOVED / ASK 重定向。 |
| kvrocks | 自动识别并适配 kvrocks 存储引擎。 |
| Garnet | 自动识别 Microsoft Garnet，功能缺失时优雅降级。 |
| Valkey | 通过 INFO 识别 Valkey 服务端。 |

</details>

<details>
<summary><b>协议与服务器兼容</b>（7 项）</summary>

| 能力 | 关键特点 |
| --- | --- |
| RESP2 | 简单字符串 / 错误 / 整数 / 批量字符串 / 数组完整实现。 |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error，HELLO 握手协商（Redis 6.0+）。 |
| Redis 2.8～7.x | 覆盖各版本主流命令，含 6.2 的 COPY / GETDEL / GETEX / GEOSEARCH。 |
| ACL 认证 | Redis 6.0+ 用户名 + 密码认证。 |
| 事务 | MULTI / EXEC / WATCH。 |
| Lua 脚本 | EVAL / EVALSHA。 |
| Redis Functions | FCALL / FUNCTION（规划中）。 |

</details>

<details>
<summary><b>云厂商、安全与可观测性</b>（8 项）</summary>

| 能力 | 关键特点 |
| --- | --- |
| 阿里云 KVStore / Tair | 标准版、ACL、SSL 与代理模式说明，含 Tair 扩展结构（TairString / TairHash / TairZSet）。 |
| 腾讯云 Redis | 实例 ID 作为用户名（`instanceId:password`）。 |
| 华为云 DCS | 主备 / SSL / 集群版差异说明（集群版不支持 SELECT）。 |
| TLS/SSL | 可指定协议版本，支持 mTLS 客户端证书。 |
| 结构化日志 | 组件日志（`Log`）与协议级日志（`ClientLog`）。 |
| 性能计数器 | `PerfCounter` 输出 ops/s、延迟与错误率。 |
| APM 追踪 | `ITracer` 全链路埋点 + `ActivitySource`（net8.0+）对接 OpenTelemetry。 |
| 连接串加密 | `ProtectedKey` 机制自动解密连接串中的密码。 |

</details>

<details>
<summary><b>平台与集成</b>（5 项）</summary>

| 能力 | 关键特点 |
| --- | --- |
| 多目标框架 | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0；扩展包覆盖 netcoreapp3.1 → net10。 |
| 强命名 | 使用 `newlife.snk` 签名，可自行重编替换程序集。 |
| NativeAOT | `IsAotCompatible` 静态校验 + `AotSmoke/` 冒烟 + 多 RID CI 矩阵。 |
| ASP.NET Core 扩展包 | `NewLife.Redis.Extensions` 通过 `AddRedisCaching` 注册 `IDistributedCache` 与 `FullRedis`。 |
| DataProtection | `PersistKeysToRedis` 将数据保护密钥持久化到 Redis。 |

</details>

## 生态版图

NewLife 生态按依赖分层，各项目独立版本、独立发版。下面的版图在所有 NewLife 仓库中逐字一致；每个项目链接到代码仓库与主 NuGet 包。

| 项目 | 一句话定位 | 下载量 |
| --- | --- | --- |
| **基础底座** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 日志、配置、缓存、网络、序列化、定时调度与 APM 追踪。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 数据中间件：单表百亿级、自动分表、读写分离，支持 10 多种数据库。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高性能网络库：单机 177 万 pps、14.5 Gbps（TCP 1KB，2026-10 实测），400 万 Tcp 长连接。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 协议通信与 RPC 框架，面向 CS 应用与物联网设备。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 把 .NET 应用安装为操作系统守护进程：Windows 服务与 Linux systemd。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` 项目模板：控制台、Web、WebApi、服务、数据与物联网。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **数据存储与消息** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 客户端：微秒级延迟、百万级吞吐，内置消息队列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 纯托管 RocketMQ 客户端，支持 Apache RocketMQ 与阿里云消息队列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 客户端与服务端，客户端支持阿里云物联网。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 混合数据库：嵌入/网络双模，关系型、时序、消息队列与 KV 于一体。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql 驱动与数据访问辅助层，基于 NewLife 数据栈实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web、AI 与分布式** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 魔方快速开发平台：用户权限、SSO、OAuth 服务端与 CRUD 脚手架。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 星尘分布式服务平台：节点管理、APM、配置中心、注册中心与发布中心。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 蚂蚁调度：分布式任务与实时数据平台，支持分片、重试与回溯补算。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 统一接入 46 家大模型服务商，附带开箱即用的 ChatAI Web 对话应用。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **工业与物联网** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT 标准库，定义物联网通信协议契约与设备抽象。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 西门子 PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | 欧姆龙 PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | 三菱 Melsec PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC 客户端，用于工业数据采集。 | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 楼宇自控协议栈。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 开源轻量级 IoT 平台，含网关与设备管理。 | — |
| **车联网** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 车辆定位协议：70+ 消息体，覆盖 2011/2013/2019 三个版本。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 部标平台互联协议，面向运输管理部门。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 车载视频：RTP 解析、FLV/fMP4 转封装与直播推送。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **领域组件** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | 一个包覆盖 Excel、Word、PPT、PDF 等 15+ 文档格式。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 跨平台音频：编解码、设备 IO、DSP、容器与流式处理。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 视频编解码、封装格式、帧处理与流媒体。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度/高德/腾讯/天地图统一接口，支持地理编码与坐标转换。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 归属地：离线内存映射库 + 在线 API 双源。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **工具与产品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 码神工具：网络、串口、加解密、正则、Modbus 与 MQTT。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 自托管 Git 源代码管理系统。 | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 从低版本宿主启动时，自动安装匹配的 .NET 运行时。 | — |

> 徽章取各项目主包；主题包、扩展包与卫星包见对应 NuGet 页面。未在表中列出的 30 多个包（FTP、LoRa、Office 扩展、各类协议驱动等）见 [NewLifeX 组织](https://github.com/NewLifeX) 与 [newlifex.com](https://newlifex.com)。

## 文档

**NewLife.Redis** 为 .NET 开发者提供对 Redis 全生命周期的访问能力：

- **协议完整**：实现 RESP2 协议，覆盖 Redis 2.8 ～ 7.x 全版本主流命令，支持 String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog 所有数据结构。
- **消息队列**：内置简单队列、**可靠队列（RPOPLPUSH + Ack 语义）**、延迟队列、Stream 多消费组队列，开箱即用，无需重复造轮子。
- **集群高可用**：支持 Standalone / 主从复制 / Sentinel 哨兵 / Cluster 原生集群，自动故障切换。
- **云厂商适配**：针对**阿里云 KVStore / 腾讯云 Redis / 华为云 DCS** 提供专项配置说明与适配，覆盖代理模式、ACL 认证、SSL 加密、特殊认证格式等差异。
- **兼容实现**：自动识别 Microsoft Garnet、kvrocks 等 Redis 兼容实现，功能不支持时优雅降级并提供明确替代方案。
- **宽框架支持**：一个包覆盖 **.NET Framework 4.5 ～ .NET 10+**，适配工控、政务、IoT 等遗留场景。

### 使用文档

- [需求文档](Doc/需求文档.md)：愿景、目标与功能需求
- [架构文档](Doc/架构文档.md)：分层、组件与关键设计决策
- [功能清单](Doc/功能清单.md)：每个功能的实现、测试与注释状态
- [竞品分析](Doc/竞品分析.md)：与主流 Redis 客户端的对比与取舍
- [Garnet 兼容性](Doc/Garnet兼容性.md)：支持范围与优雅降级策略
- [AOT 兼容性](Doc/AOT兼容性分析.md)：NativeAOT 覆盖范围与实测结论
- [多语言本地化](Doc/多语言本地化.md)：异常与日志的多语言模型
- [RedisSwitch 集成](Doc/RedisSwitch项目说明.md)：平滑切换工具说明
- [GC 分配分析](Doc/dotMemory分析Redis的GC分配.md)：dotMemory 实测记录
- [官方网站](https://newlifex.com/core/redis)：组件主页与在线文档

### 协议与版本支持

#### RESP 协议支持

| 协议 | 状态 | 说明 |
|------|------|------|
| RESP2 | ✅ 完整支持 | 简单字符串 / 错误 / 整数 / 批量字符串 / 数组 |
| RESP3 | ✅ 完整支持 | Map / Set / Double / Boolean / Null / Blob Error 类型，HELLO 握手协商（Redis 6.0+）|

#### Redis 版本特性支持

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

### 架构与模块划分

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
│  RedisStack<T> · RedisGeo · HyperLogLog · RedisRedLock         │
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

### 用法示例

以下示例均基于[快速开始](#快速开始)中创建的 `FullRedis` 实例。

<details>
<summary><b>基础用法</b></summary>

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

// 键前缀（FullRedis.Prefix）
var scoped = rds.GetPrefixed("myapp:");
scoped.Set("user", "Alice");  // 实际 key 为 myapp:user
```

</details>

<details>
<summary><b>数据结构操作</b></summary>

#### List（列表）

```csharp
var list = rds.GetList<String>("queue:demo");
list.Add("job1");
list.Add("job2");
var first = list[0];
var popped = list.RightPop();
```

#### Hash（哈希）

```csharp
var hash = rds.GetDictionary<String>("user:1001");
hash["Name"] = "Alice";
hash["Email"] = "alice@example.com";
var name = hash["Name"];
var all = hash.GetAll();
```

#### Set（集合）/ SortedSet（有序集合）

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

#### Stack（栈）

```csharp
var stack = rds.GetStack<String>("undo:ops");
stack.Push("op1");
stack.Push("op2");
var last = stack.Pop();   // "op2"
```

#### Geo（地理信息）

```csharp
var geo = rds.GetGeo("locations");
geo.Add("Beijing",  116.4074, 39.9042);
geo.Add("Shanghai", 121.4737, 31.2304);
var dist = geo.GetDistance("Beijing", "Shanghai", "km");
var nearby = geo.Search("Beijing", 500, "km");
```

#### HyperLogLog（基数统计）

```csharp
var hll = rds.GetHyperLogLog("uv:today");
hll.Add("user1", "user2", "user3");
var count = hll.Count;  // 近似基数
hll.Merge("uv:yesterday");
```

> 注意：基础 `Redis` 实例仅支持 KV 操作，高级集合请使用 `FullRedis`。

</details>

<details>
<summary><b>管道与自动合并</b></summary>

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

</details>

<details>
<summary><b>消息队列</b></summary>

#### 简单队列

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// 生产者
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// 消费者（阻塞等待，timeout=0 永久阻塞）
var msg = queue.TakeOne(timeout: 30);
```

#### 可靠队列（至少一次语义）

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

#### 延迟队列

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// 60 秒后可被消费
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// 消费（自动等待到期消息）
var msg = delay.TakeOne(timeout: 5);
```

#### Stream 流队列（Redis 5.0+，多消费组）

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

</details>

<details>
<summary><b>发布订阅</b></summary>

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

</details>

<details>
<summary><b>集群与高可用</b></summary>

#### 多地址自动切换

```csharp
// 逗号分隔多地址，网络异常时自动切换，60 秒后切回主节点
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // 不可用节点屏蔽 10 秒（默认）
    Retry = 3,            // 重试次数（默认）
};
```

#### 自动集群模式检测

```csharp
// AutoDetect=true 时，通过 INFO 自动识别工作模式
// ⚠️ 公有云代理模式下请保持默认 false，避免获取到内网节点地址
var rds = new FullRedis("sentinel-host:26379", "pass", 0)
{
    AutoDetect = true,    // 自动识别 Cluster / Sentinel / Replication
};
```

</details>

<details>
<summary><b>云厂商适配</b></summary>

#### 阿里云 KVStore / Tair

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

#### 腾讯云 Redis

```csharp
// 腾讯云标准/集群版：UserName 填写实例 ID（AUTH instanceId:password 格式）
var rds = new FullRedis("xxxxxx.redis.tencentcds.com:6379", "instanceId", "YourPassword", 0);

// 或连接字符串形式
var rds = FullRedis.Create("server=xxxxxx.redis.tencentcds.com:6379;username=instanceId;password=YourPassword;db=0");
```

#### 华为云 DCS

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

</details>

<details>
<summary><b>序列化与编码器</b></summary>

默认编码：`RedisJsonEncoder`（内置 JSON 主机），开箱即用。

```csharp
// 查看当前 JSON 配置
var jsonHost = RedisJsonEncoder.GetJsonHost();

// 切换为自定义编码器（如 MessagePack）
rds.Encoder = new MyMessagePackEncoder();
```

基元类型（`String` / `Int32` / `Int64` / `Double` / `Boolean`）直接序列化，不经 JSON，性能最优。

</details>

<details>
<summary><b>安全与认证</b></summary>

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

</details>

<details>
<summary><b>可观测性</b></summary>

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

// OpenTelemetry 原生埋点（net8.0+）：订阅内置 ActivitySource
// builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource(Redis.ActivitySourceName));
```

</details>

<details>
<summary><b>扩展包</b></summary>

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

</details>

<details>
<summary><b>Garnet 兼容性</b></summary>

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

</details>

### 参考资料

<details>
<summary><b>功能完成情况</b></summary>

> 图例：✅ 已完成 | 🔄 规划中 | ❌ 明确不计划

#### 数据结构

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

#### 消息队列

| 队列类型 | 完成状态 | 语义保障 |
|---------|---------|---------|
| `RedisQueue<T>` | ✅ | 至多一次 |
| `RedisReliableQueue<T>` | ✅ | 至少一次（RPOPLPUSH + Ack）|
| `RedisDelayQueue<T>` | ✅ | 延迟投递（Sorted Set）|
| `RedisStream<T>` | ✅ | 有序 + 消费组 + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | 多消费组广播 |
| `PubSub` + `RedisEventBus` | ✅ | 实时推送 |
| RedLock 分布式锁 | 🔄 | 多实例 Redlock 算法 |

#### 集群支持

| 模式 | 完成状态 |
|------|---------|
| Standalone 单机 | ✅ |
| 主从复制 Replication | ✅ |
| Sentinel 哨兵 | ✅ |
| Cluster 原生集群 | ✅ |
| kvrocks 自动适配 | ✅ |
| Garnet 自动检测 | ✅ |
| Valkey 自动检测 | ✅ |

#### 协议与高级特性

| 功能 | 完成状态 |
|------|---------|
| RESP2 完整支持 | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| ACL 认证 | ✅ |
| 事务 MULTI/EXEC/WATCH | ✅ |
| Lua 脚本 EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 规划中 |
| Pipeline（显式+自动）| ✅ |
| 模式订阅 PSUBSCRIBE | 🔄 |
| UNLINK 异步删除 | 🔄 |

</details>

<details>
<summary><b>性能测试参考</b></summary>

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

</details>

<details>
<summary><b>最佳实践</b></summary>

- **单例复用**：`FullRedis` 内部有连接池，强烈建议单例供全程使用，不要每次 `new`
- **Key 设计**：使用 `:` 分层，如 `user:1001:profile`；生产环境使用 `Prefix` 隔离命名空间
- **批量优先**：能 `GetAll/SetAll` 不循环单键；利用 `AutoPipeline` 自动合并写操作
- **Value 大小**：控制在 1～2KB；大对象拆分 / 压缩 / 用 Hash 分字段存储
- **可靠消费**：生产场景使用 `RedisReliableQueue<T>` 而非简单 List，避免消息丢失
- **云厂商**：公有云保持 `AutoDetect = false`，避免内网节点地址问题
- **监控**：生产启用 `Counter` 统计；`ClientLog` 仅调试时打开，避免热路径日志开销
- **SSL**：公有云 / 生产环境启用 TLS 加密传输

</details>

<details>
<summary><b>常见问题 FAQ</b></summary>

**Q: 是否支持 Redis Cluster 分片？**  
A: 支持。`FullRedis` 通过 `AutoDetect=true` 自动识别 Cluster 模式，CRC16 路由、MOVED/ASK 重定向均已实现。公有云代理模式建议保持默认 `AutoDetect=false`。

**Q: 和 StackExchange.Redis 相比有什么区别？**  
A: NewLife.Redis 采用连接池模型（对比 StackExchange 的多路复用），连接行为更直观；内置消息队列封装（可靠队列/延迟队列）是核心差异化；支持 .NET Framework 4.5。详见 [竞品分析](Doc/竞品分析.md)。

**Q: 如何处理大 Value 导致的慢查询？**  
A: 拆分数据结构（如 Hash 分字段）+ 批量读写 + 二进制编码器 + 控制 `MaxMessageSize`（默认 1MB 上限）。

**Q: 连接池用完了怎么办？**  
A: 默认最大 100,000 连接，正常业务不会耗尽。连接池可通过 `Redis.PoolConfig`（`RedisPoolConfig`）配置最小空闲数/最大容量/空闲清理时间/最大生命周期/借出等待超时，借出连接时会对空闲超时的连接主动 PING 健康检查，主从切换/代理滚动更新场景可调小 `MaxLifetime` 加快连接回收。若出现等待超时，检查是否有连接泄漏（未 `Dispose`）或某些操作持续长时间占用。

**Q: 使用阿里云 / 腾讯云为什么连接失败？**  
A: 检查：① `AutoDetect` 是否为 false（默认正确）；② 认证格式是否正确（腾讯云需要 `username=instanceId`）；③ 网络安全组 / 白名单是否放开客户端 IP；④ 集群版是否禁止使用 SELECT 切库。

**Q: Garnet 服务器是否支持所有功能？**  
A: 基础操作、List/Hash/Set/ZSet/HLL/Geo/队列均支持；Stream 暂不支持，会自动抛出 `NotSupportedException` 并提示用 `RedisQueue<T>` 替代。

</details>

<details>
<summary><b>路线图</b></summary>

- [x] ~~RESP3 协议支持~~（已完成）
- [ ] Redis Functions（FCALL/FUNCTION，替代 Lua 脚本）
- [ ] LMPOP / ZMPOP（Redis 7.0 批量弹出）
- [ ] RedLock 多实例分布式锁
- [ ] Bitmap / Bitfield 操作
- [ ] 模式订阅 PSUBSCRIBE 完整实现
- [ ] UNLINK 异步删除
- [ ] Prometheus 指标导出适配器
- [x] 阿里云 Tair 扩展（TairString / TairHash / TairZSet 已支持）
- [ ] 华为云 DCS 集群版节点地址映射适配
- [x] ~~发布订阅 PubSub~~（已完成）
- [x] ~~RedisStream 多消费组~~（已完成）
- [x] ~~Garnet 服务器类型检测~~（已完成）

欢迎通过 Issue / PR 参与投票或补充需求。

</details>

<details>
<summary><b>竞品对比</b></summary>

| 能力 | NewLife.Redis | StackExchange.Redis | FreeRedis | 备注 |
|------|--------------|--------------------|-----------|----|
| .NET Framework 4.5 | ✅ | ❌（4.6.1+）| ❌ | 工控 / 政务遗留系统关键 |
| 连接模型 | 连接池 | 多路复用 | 连接池 | 各有适用场景 |
| 命令覆盖率 | ~98% | ~95% | ~98% | 文档收录命令除明确不计划项外全部支持 |
| 内置消息队列封装 | ✅ 完整 | ❌ | 🔄 部分 | 可靠 / 延迟队列独有 |
| 内置序列化 | ✅ | ❌ 手动 | ❌ 手动 | 降低样板代码 |
| 云厂商专项适配 | ✅（阿里/腾讯/华为）| 通用 | 通用 | 有专项文档 |
| Garnet 显式适配 | ✅ | ❌ | ❌ | 类型检测 + 降级 |
| kvrocks 适配 | ✅ | ❌ | ❌ | 自动识别 |
| Valkey 自动检测 | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| APM 追踪 | ITracer 接口 + ActivitySource（net8.0+）| ActivitySource | ❌ | 原生对接 OpenTelemetry |
| RESP3 | ✅ | ✅ | ✅ | 已通过 RESP3 测试套件验证 |

详细分析见 [竞品分析文档](Doc/竞品分析.md)。

</details>

### 版本记录

- [ChangeLog.md](ChangeLog.md)：版本变更记录

## 参与贡献

### 构建与测试

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- 集成测试需要可用的 Redis：设置 `REDIS_CONNECTION` 环境变量，或在本机 `127.0.0.1:6379` 启动服务；未就绪时带 `[RedisFact]` / `[RedisTopologyFact]` 的用例会自动跳过。
- NativeAOT 冒烟：`AotSmoke/`，CI 见 `.github/workflows/aot.yml`。
- 性能基准：`Samples/Benchmark`。

### 仓库结构

| 目录 | 说明 |
| --- | --- |
| `NewLife.Redis/` | 客户端源码：连接池、RESP 编解码、数据结构与队列 |
| `NewLife.Redis.Extensions/` | ASP.NET Core 集成：`IDistributedCache`、DI 与数据保护 |
| `NewLife.Extensions.Caching.Redis/` | 旧版缓存扩展兼容包 |
| `XUnitTest/` | xUnit 测试套件（net10.0） |
| `AotSmoke/` | NativeAOT 冒烟工程 |
| `Samples/` | Benchmark、WebDemo、QueueDemo、RedisSwitch 与移动端冒烟示例 |
| `Doc/` | 需求、架构、功能清单与专题文档 |

### 发版

推送 `v` 前缀标签（如 `v6.7.2026.1024`）触发 `.github/workflows/publish.yml`。工作流打包 `NewLife.Redis` 与 `NewLife.Redis.Extensions`，发布到 NuGet.org 与 GitHub Packages，并按 `ChangeLog.md` 对应版本段落创建 GitHub Release；手动运行只发布包、不创建 Release。

### 反馈

1. 提交前确保 `dotnet build` 与相关测试通过，并遵循仓库编码规范与提交约定
2. PR：保持最小变更、添加必要注释与测试说明
3. Issue：提供版本、运行环境、最小复现场景

社区：QQ 群 1600800 / 1600838；GitHub Discussions / Issues 参与答疑。

## 新生命开发团队

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

新生命团队成立于 2002 年，二十余年专注 .NET 与物联网技术生态，主导开源项目近百个（生态版图收录 34 个主干项目），为电力、物流、医疗、工控、互联网等行业提供软硬件一体化解决方案；以“学无先后达者为师”为信条。

开源组件在 NuGet 累计下载 <!--nuget-total:start-->6.48M<!--nuget-total:end--> 次（全生态口径，截至 2026-10）。

网站：https://newlifex.com · 开源组织：https://github.com/NewLifeX

<details>
<summary>微信公众号：智能大石头（扫码关注）</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## 许可证

MIT License。可自由商用 / 修改 / 再发行（无需额外授权）。保留版权声明即可。

> 如果本文档没有覆盖你的使用场景，欢迎提交 Issue，一起把文档补全。
