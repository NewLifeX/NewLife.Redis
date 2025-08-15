# NewLife.Redis - 高性能 Redis 客户端组件

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.Redis?label=dev%20nuget&logo=nuget)

## [[English]](https://github.com/NewLifeX/NewLife.Redis/blob/master/Readme.en.md)

`NewLife.Redis` 是新生命团队打造的 **高性能 / 高吞吐 / 易集成** 的 Redis 客户端，核心目标：支撑实时计算、海量缓存、可靠消息、分布式基础设施等场景。组件自 2017 年起在多个千万 / 百亿级数据与高并发生产平台稳定运行，经受日均 **80+ 亿次** 调用考验。

---
## 目录
- [核心特性](#核心特性)
- [架构与模块划分](#架构与模块划分)
- [对比说明](#对比说明)
- [安装与快速开始](#安装与快速开始)
- [基础用法](#基础用法)
- [批量与集合操作](#批量与集合操作)
- [管道 Pipeline 与自动合并](#管道-pipeline-与自动合并)
- [消息 / 队列 / 可靠消费](#消息--队列--可靠消费)
- [序列化与编码器](#序列化与编码器)
- [性能测试参考](#性能测试参考)
- [最佳实践与经验](#最佳实践与经验)
- [多实例 & 高可用策略](#多实例--高可用策略)
- [扩展包 (Extensions)](#扩展包-extensions)
- [与 MemoryCache 的协同](#与-memorycache-的协同)
- [常见问题 FAQ](#常见问题-faq)
- [路线图 Roadmap](#路线图-roadmap)
- [新生命项目矩阵](#新生命项目矩阵)
- [贡献指南 & 社区](#贡献指南--社区)
- [许可证](#许可证)

---
## 核心特性
* 经大规模生产验证：200+ Redis 实例，日峰值 1 亿+ 业务对象写入 / 80+ 亿命令调用
* 低延迟：单次 Get/Set 往返 200~600µs（微秒级，含网络）
* 高吞吐：内置 **连接池 (最大 100000 并发)** + 同步高效协议解析 + 可选自动管道合并
* 自动重试与多地址故障切换：Server 支持逗号分隔多节点，网络型异常快速切换
* 丰富高级结构：List / Hash / Set / Queue / Stack / 延迟与可靠消费（基于 RPOPLPUSH/BRPOPLPUSH）
* 批量优化：`GetAll` / `SetAll` / 管道聚合显著降低 RTT
* 可插拔编码器：默认 JSON，可扩展二进制 / 自定义序列化，以减小包长与 GC 压力
* 追踪与监控：支持 `ITracer`（APM 链路），可插入性能计数器 `PerfCounter`
* 强命名 + 多目标框架：一份包覆盖 **net45 / net461 / netstandard2.0 / netstandard2.1 / (扩展包含 netcoreapp3.1→net9)**
* 零外部重量级依赖，充分复用 NewLife 生态（日志、配置、序列化、安全）

---
## 架构与模块划分
```
┌──────────────────────────────────────────┐
│ Redis (基础核心)                         │
│  ├─ 连接池 IPool<ObjectPool<RedisClient>>│
│  ├─ 协议解析 RedisClient / RESP          │
│  ├─ 基础 KV / 批量 / 过期 / 计数器       │
│  ├─ 重试 & 多节点切换 / 超时 / 限制       │
│  ├─ Pipeline 管理 (Start/Stop/Auto)      │
│  └─ 序列化编码 (IPacketEncoder)          │
│                                          │
│ FullRedis (扩展)                         │
│  ├─ 列表 RedisList<T>                    │
│  ├─ 哈希 RedisHash<T> (键值字典)         │
│  ├─ 队列 / 栈 / Set / 延迟消费           │
│  ├─ 发布订阅 (若启用) / 搜索辅助         │
│  └─ 可靠消息 (RPOPLPUSH / BRPOPLPUSH)    │
│                                          │
│ Extensions (DI 集成)                     │
│  ├─ IDistributedCache 实现               │
│  └─ IDataProtection 后端存储             │
└──────────────────────────────────────────┘
```
关键类说明：
- `Redis`：核心客户端，提供协议管线、连接复用、自动管道、批量操作、性能统计、重试与故障转移。
- `RedisClient`：底层连接与 RESP 命令执行单元（内部对象，池化管理）。
- `FullRedis`：在 `Redis` 基础上追加更丰富数据结构及更高级场景能力。
- `RedisList<T>` / `RedisHash<T>` 等：对 Redis 原生命令进行泛型包装，统一编码与序列化策略。
- `IPacketEncoder`：编解码策略，可切换 JSON / 二进制。

---
## 对比说明
| 能力 | NewLife.Redis | StackExchange.Redis | 备注 |
| ---- | ------------- | ------------------- | ---- |
| 连接池 | 内置，支持 Max=100000 | Multiplexer（逻辑复用） | 大并发场景更直观可控 |
| 自动管道 | 支持 AutoPipeline/FullPipeline | 默认批量写入合并 | 精细控制与显式 StartPipeline |
| 性能追踪 | ITracer 接口 | 需外部集成 | 生态一致 APM 链路 |
| 序列化 | 可插拔 Encoder | 仅字节/字符串，需外部序列化 | 降低调用侧样板代码 |
| 可靠消费 | RPOPLPUSH/BRPOPLPUSH 包装 | 需手动实现 | 队列式工作流场景便利 |
| 生态整合 | 与 NewLife.* 组件无缝 | 无 | 同一风格 & 工具链 |

---
## 安装与快速开始
NuGet：`NewLife.Redis`
```bash
# 稳定版
dotnet add package NewLife.Redis
# 或开发版 (包含预发布)
dotnet add package NewLife.Redis -v *-*
```
示例（推荐单例，线程安全）：
```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();
var rds = new FullRedis("127.0.0.1:6379", "pass", 7)
{
    Log = XTrace.Log,
    ClientLog = XTrace.Log,   // 调试阶段打开
    AutoPipeline = 100        // 达到阈值自动提交管道
};

rds.Set("user:1", new { Name = "Alice", Time = DateTime.Now }, 3600);
var user = rds.Get<String>("user:1");
Console.WriteLine(user);
```

---
## 基础用法
```csharp
rds.Set("k1", 123, 600);          // 设置并指定过期
var v = rds.Get<Int32>("k1");
var ok = rds.Add("k2", "init");   // 仅在不存在时写入
var old = rds.Replace("k2", "new");
rds.Increment("counter", 1);
rds.Decrement("counter", 2);
```
过期管理：`SetExpire(key, TimeSpan)` 与 `GetExpire(key)`。

---
## 批量与集合操作
```csharp
rds.SetAll(new Dictionary<String,Object>{{"a",1},{"b",2},{"c",3}}, 300);
var dict = rds.GetAll<Int32>(new[]{"a","b","c"});
```
泛型集合：
```csharp
var list = rds.GetList<String>("queue:demo");
list.Add("job1");
var first = list[0];
```
> 注意：基础 `Redis` 实例仅支持字符串类操作，高级集合请实例化 `FullRedis`。

---
## 管道 Pipeline 与自动合并
场景：减少 RTT、提升批量操作吞吐。
```csharp
var client = rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(); // results 为命令返回集合
```
自动模式：设置 `AutoPipeline = 100` 后，写操作累积到阈值自动提交；`FullPipeline = true` 时读请求也进入管道。

---
## 消息 / 队列 / 可靠消费
利用 List + `RPOPLPUSH` / `BRPOPLPUSH` 保障“取出-处理中-确认”原子性：
```csharp
var src = rds.GetList<String>("jobs:ready");
var bak = rds.GetList<String>("jobs:working");
var job = src.RPOPLPUSH(bak.Key); // 取出放入备份队列
// 处理成功后从备份列表删除
bak.Remove(job);
```
阻塞获取：`BRPOPLPUSH(destKey, timeoutSeconds)`，timeout=0 表示永久阻塞。

---
## 序列化与编码器
默认编码：`RedisJsonEncoder`（内置 JSON 主机）。可通过实现 `IPacketEncoder` 定制二进制格式以减少内存与网络：
```csharp
rds.Encoder = new MyBinaryEncoder();
```
如需共享 JSON 配置或自定义时间/数字格式，可设置：`rds.JsonHost = RedisJsonEncoder.GetJsonHost();`

---
## 性能测试参考
源码内置 Benchmark（`Redis.Bench`），典型结果（40 逻辑处理器，批量优化）：
```
写入 400,000 项 4 线程  ~576,368 ops
读取 800,000 项 8 线程  ~647,249 ops
删除 800,000 项 8 线程  ~1,011,378 ops
```
可执行：
```csharp
rds.Bench(rand:true, batch:100);  // 随机 + 批量
```
> 实际性能受网络 RTT / 序列化复杂度 / Value 大小影响。建议单 Value 控制在 1.4KB 附近提升整体效率。

---
## 最佳实践与经验
* 多实例拆分：按 Key 哈希 (CRC16/CRC32) 分布到多 Redis，提高扩展性
* 合理 Value 大小：控制在 1~2KB；过大分片 / 压缩 / 结构化
* 批量优先：能 `GetAll/SetAll` 不循环单键；利用管道降低往返
* 高可靠消费：`RPOPLPUSH` + 备份列表手动确认
* 序列化：二进制优于 JSON；必要时，用池化缓冲减少 GC
* 性能监控：开启 `Counter` / `Tracer` 仅在需要时，避免热路径开销

---
## 多实例 & 高可用策略
`Server` 可配置：`"10.0.1.10:6379,10.0.1.11:6379"`。
- 发生网络型异常（Socket/IO）时自动切换下一个地址
- `ShieldingTime` 控制不可用节点屏蔽窗口
- 一段时间后自动尝试回切主节点

---
## 扩展包 (Extensions)
`NewLife.Redis.Extensions` 提供 ASP.NET Core 集成：
* `IDistributedCache` 后端实现
* `IDataProtection` 密钥存储

安装：
```bash
dotnet add package NewLife.Redis.Extensions
```
示例：
```csharp
builder.Services.AddRedisCaching(options =>
{
    options.Server = "127.0.0.1:6379";
    options.Password = "pass";
});
```

---
## 与 MemoryCache 的协同
建议以 `ICache` 编程：小数据或临时热点 → `MemoryCache`；规模上升/跨进程共享 → 切换 `Redis`/`FullRedis`，无须修改业务逻辑。

---
## 常见问题 FAQ
Q: 是否支持发布订阅 / Stream / Cluster？  
A: 基础代码已具备扩展点，发布订阅/更多结构可在 FullRedis 扩展层或后续版本完善。Cluster 分片可通过多实例 + Key 路由策略实现。  
Q: 如何处理反序列化失败？  
A: `TryGetValue` 返回是否存在键，即使反序列化失败仍可感知，用于容错与告警。  
Q: 如何降低大 Value 带来的慢查询？  
A: 拆分结构 + 批量 + 二进制编码 + 控制 `MaxMessageSize`（默认 1MB）。

---
## 路线图 Roadmap
- [*] 发布订阅友好封装（模式订阅 / 回调）
- [ ] 更完善的分布式锁 / RedLock 支持
- [*] Stream / 消费组封装
- [ ] 更灵活的二进制/Span Encoder 示例
- [ ] 内置指标导出（Prometheus 适配器）
- [ ] 单测覆盖率提升 & BenchmarkDotNet 场景脚本化

欢迎通过 Issue / PR 参与投票或补充需求。

---
## 新生命项目矩阵
（节选）
| 项目 | 说明 |
| ---- | ---- |
| [NewLife.Core](https://github.com/NewLifeX/X) | 核心库，日志/配置/缓存/序列化/APM |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 大数据 ORM，百亿级 + 分表 + 读写分离 |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 超高性能网络库（千万级吞吐） |
| [Stardust](https://github.com/NewLifeX/Stardust) | 分布式服务/配置/注册/发布中心 |
| [AntJob](https://github.com/NewLifeX/AntJob) | 分布式计算 & 调度平台 |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | RocketMQ 纯托管客户端 |
| ... | 更多见官网与组织首页 |

> 完整矩阵、企业级解决方案与商业支持请访问：https://newlifex.com

---
## 贡献指南 & 社区
1. 提交前阅读仓库 `.github/copilot-instructions.md`（编码规范 & 审核清单）  
2. 提交 PR：保持最小变更、添加必要注释与测试说明  
3. Issue：提供版本、运行环境、最小复现场景  

社区：QQ群 1600800 / 1600838 ；GitHub Discussions / Issues 参与答疑。  

---
## 许可证
MIT License。可自由商用 / 修改 / 再发行（无需额外授权）。保留版权声明即可。

---
## 新生命开发团队
![XCode](https://newlifex.com/logo.png)

团队自 2002 年迄今，维护 80+ .NET / IoT / 分布式相关开源项目，NuGet 累计下载超 400 万。产品与组件已广泛服务于电力、物流、工业控制、教育、通信、文博等行业。  
网站：https://newlifex.com  | 开源：https://github.com/NewLifeX  
微信公众号：

![智能大石头](https://newlifex.com/stone.jpg)

---
> 若本文档未覆盖你的使用场景，欢迎提交 Issue 补充；一起让文档更完善！
