# NewLife.Redis AOT 兼容性分析

> 基线：v6.6 代码库 + NewLife.Core 12.0.2026.1009-beta0916｜评估与实施：2026-10-09｜方式：**分析器扫描 + NativeAOT 真实编译与运行实测**（win-x64，.NET 10，Redis 6.2.5）
>
> 复验：NewLife.Core 12.0.2026.1013-beta0732｜2026-10-15｜分析器与 ILC 均 0 告警，23 场景 JIT/NativeAOT 双形态全通（win-x64，.NET 10，Redis 6.2.5；含 4 节点集群与哨兵拓扑实测），升级未引入新的兼容或 AOT 问题；同日清理本地注解垫片与 Core 公开存根的类型重复（旧 TFM 384 条 CS0436 清零）。

---

## 1. 现状摘要

- **库侧已达标**：主库与扩展包在 net8.0 目标常驻 `IsAotCompatible`，分析器扫描与 ILC 发布均 **0 条 AOT 告警**；核心链路 **23 场景三形态（JIT/裁剪/AOT）全通**（连接、RESP3、字符串、计数、哈希、列表、队列、可靠队列、泛型弹列、延迟队列、Stream、事件总线、多消费组队列、PubSub、JSON 对象/数组/字典、Message 字段解码、集群路由、哨兵主从寻址）。
- **工程化配套就绪**：`AotSmoke` 冒烟工程入仓（JIT/AOT 双跑、非 0 退出码即失败），`aot.yml` CI 防回归（分析器告警阻断 + linux-x64 原生发布运行冒烟）。
- **改造前基线**：主库原目标框架 net45/net461/netstandard2.0/netstandard2.1，不含 net8.0+ 且未启用分析器；net8.0 + IsAotCompatible 实测 **6 条 AOT 告警**（已全部处置，见 §2.2 处置档案）。
- **已知边界**：AOT 下 JSON 主机按动态代码能力显式选择（JIT=SystemJson / AOT=FastJson，属有意设计，见 §2.4）；运行期类型与嵌套类型的反射解码要求调用方以泛型入口或静态引用保根（§3）。

---

## 2. 实测数据

### 2.1 冒烟矩阵（23 场景，JIT/裁剪/AOT 三形态）

| # | 场景 | # | 场景 |
|---|------|---|------|
| 1 | 连接与服务器信息（INFO/版本） | 12 | Object 值字典 JSON 存取（`Dictionary<String,Object>`） |
| 2 | RESP3 协议（HELLO 3 协商） | 13 | Stream 追加/读取（`GetStream<T>`） |
| 3 | 字符串 Set/Get | 14 | 列表范围读取（LRANGE→`T[]`，Array 转换分支） |
| 4 | 整数 INCR | 15 | JSON 数组 存取（`T[]` 经编码器） |
| 5 | Hash 字典 | 16 | 泛型弹列 BLPOP/BRPOP（JSON 解码） |
| 6 | List 列表 | 17 | 延迟队列 延迟投递（`GetDelayQueue<T>`） |
| 7 | 队列 生产/消费（`GetQueue<T>`） | 18 | Message 字段解码（`GetBody<T>` 反射路径，含嵌套类型） |
| 8 | 可靠队列 生产/消费（`GetReliableQueue<T>`） | 19 | 事件总线 发布/订阅（`RedisEventBus<T>`） |
| 9 | JsonHelper 泛型往返 | 20 | 多消费组队列 发布/订阅（`MultipleConsumerGroupsQueue<T>`） |
| 10 | 自定义对象 Redis 存取（编码器链路） | 21 | PubSub 发布/订阅（`PubSub`） |
| 11 | 复杂类型字典 JSON 存取（`Dictionary<String,Int32>`） | 22 | 集群模式 路由存取（`FullRedis` 集群发现 + MOVED 重定向 + 同槽批量） |
|  |  | 23 | 哨兵 节点发现与主从寻址（`RedisSentinel` 发现/切换/数据面） |

> 冒烟工程：仓库 `AotSmoke/`（`<PublishAot>true</PublishAot>`，JIT/AOT 运行均固定严格 AOT 姿态：`IsDynamicCodeSupported=False`）。
> 环境：.NET SDK 10.0.401 + win-x64 NativeAOT + 本机 Redis 6.2.5；逐场景 PASS/FAIL，非 0 退出码即失败。
> 拓扑场景（22/23）自带 `CheckTopo` TCP 探测：无拓扑时自动 SKIP；CI（`aot.yml`）自动搭建集群/哨兵拓扑，并以 `AOT_REQUIRE_TOPO=1` 严格实跑（拓扑缺失按场景失败处理，防静默 SKIP 丢失防回归）。

### 2.2 分析器扫描（库侧，net8.0 + IsAotCompatible）

> 阶段一（改造前）实测：**0 错误；AOT 告警 6 条**（其余 80 条为存量可空性等警告）。下表同时记录最终处置；改造后实测 **0 条**。

| # | 位置 | 编号 | 说明 | 处置结果 |
|---|------|------|------|---------|
| 1 | `FullRedis.cs:1213`（BRPOP&lt;T&gt;） | IL2087 | `JsonHost.Read(rs[1], typeof(T), null)`，T 缺少与目标参数匹配的保留要求 | 泛型参数注解传导（`DynamicallyAccessedMembers`，BLPOP/BRPOP 全部重载） |
| 2 | `FullRedis.cs:1252`（BLPOP&lt;T&gt;） | IL2087 | 同上 | 同上 |
| 3 | `Queues/QueueExtensions.cs:185`（ConsumeAsync&lt;T&gt;） | IL2091 | `JsonHost.Convert<T>(dic)`，T 缺少保留注解 | 泛型参数注解传导（对齐 `JsonHelper.Convert<T>` 要求集） |
| 4 | `Queues/Message.cs:66`（Message.GetBody&lt;T&gt;） | IL2091 | `Activator.CreateInstance<T>()`，T 缺少 PublicParameterlessConstructor | 泛型参数注解传导（PublicParameterlessConstructor \| PublicProperties） |
| 5 | `Queues/Message.cs:82`（Message.GetBody&lt;T&gt;） | IL2072 | 属性类型经反射（`PropertyInfo.PropertyType`）进入 `JsonHelper.ToJsonEntity(String, Type)`，无法静态传导 | 方法级 `UnconditionalSuppressMessage` 声明边界（运行期属性类型无法静态表达；主链路经泛型 T 保根） |
| 6 | `RedisClient.cs:1408`（TryConvert 数组分支） | IL3050 | `Array.CreateInstance(elmType, n)` 属 `RequiresDynamicCode` | 常见元素类型（String/Object/Int32/Int64/Double/Boolean/IPacket）静态数组构造，其余回退 + 边界抑制 |

- 级联传导：`RedisStream<T>`、`MultipleConsumerGroupsQueue<T>`、`FullRedis.GetStream<T>`、`CacheExtensions.GetStream<T>`、`RedisEventBus<TEvent>` 同步补齐保留注解；`RedisCacheProvider.GetQueue<T>` 与 `FullRedis.CreateEventBus<TEvent>` 因 Core 宽面契约（`CacheProvider`/`Cache` 无注解、重写不可强化）改为实现边界显式声明。
- 低版本目标框架的注解特性由 NewLife.Core 公开存根提供（net8.0+ 使用内置真实特性），源码可统一标注；本地 `Common/AotAnnotations.cs` 垫片与 Core 公开类型重复（旧 TFM 384 条 CS0436），已删除。

### 2.3 发布期（ILC）告警

- 冒烟发布（win-x64，另经 linux-x64 CI）**ILC 告警 0 条**；改造前按可达路径曾报告 2 条（均为 `Message.GetBody<T>`，对应 §2.2 #4/#5），已随处置清零。
- 口径差异保留说明：分析器按**库内全量**报告、ILC 按**下游可达性**报告；下游可达性不可控，**库侧 IsAotCompatible 分析器是必要门禁**（CI 已阻断）。

### 2.4 JSON 主机的 JIT/AOT 选择（改造前后对照）

| 场景 | JsonHost 实际值 | 说明 |
|------|----------------|------|
| 改造前 JIT | `SystemJson` | `GetTypeEx` 反射加载成功 |
| 改造前 AOT | `FastJson` | 反射被 AOT 阻断，静默降级 |
| 改造后 JIT / 动态代码可用 | `SystemJson` | net8.0 编译期直连，消除反射加载 |
| 改造后 AOT / 动态代码不可用 | `FastJson` | `RuntimeFeature.IsDynamicCodeSupported` 显式守卫回退（有意设计） |

- AOT 回退 FastJson 属有意设计：实测 SystemJson 在原生编译下对 `Dictionary<String,Int32>` 等泛型集合缺少转换器原生代码（`DictionaryOfTKeyTValueConverter` 抛 `NotSupportedException`），而 FastJson 对静态保根类型全通。
- 23 场景在三种发布形态下实测均通过（JIT/裁剪/NativeAOT）；差异影响序列化细节（格式/性能），使用时以实际主机行为为准。

### 2.5 部署形态矩阵（同一冒烟工程）

| 形态 | 发布方式 | 结果 | JSON 主机 | IL 告警 |
|------|---------|------|-----------|---------|
| JIT（严格 AOT 姿态） | `dotnet run -c Release` | 23/23 | FastJson | — |
| 裁剪发布 | `PublishTrimmed`（项目属性内设定） | 23/23 | SystemJson | 0 |
| NativeAOT | `dotnet publish -r win-x64` | 23/23 | FastJson | 0 |

> 注意：`-p:PublishTrimmed=true` / `-p:PublishAot=true` 等发布属性**不能经命令行全局属性传入**——会波及主库 net45 等目标并报 NETSDK1124/NETSDK1207，必须写在应用工程属性组内（与 NewLife.Core 的口径一致）。

### 2.6 拓扑场景带出的库缺陷修复（随 AOT 冒烟扩展一并落地）

集群/哨兵场景在 AOT 姿态下实测时，暴露出三个与 AOT 无关的存量库缺陷，均已修复并留回归测试：

| # | 缺陷 | 影响 | 修复 |
|---|------|------|------|
| 1 | 集群 MOVED/ASK 重定向端点解析错误（`RedisCluster.ReselectNode`） | 分隔符取串语义偏差导致端点串带前导槽号，重定向匹配失败，集群读写随机失败 | 改 `Split(' ')` 取末段并要求含 `:`；新增确定性回归测试 `RedisClusterTests.ReselectNodeMoved` |
| 2 | 哨兵/集群切换后连接池未失效（`Redis.SetSevices`） | 地址变化后旧连接池仍指向旧节点，数据面持续打向哨兵而非主节点（unknown command SET） | 地址变化时清空连接池（`_Pool = null`，与 `Init` 换连接串约定一致），由下次取用重建 |
| 3 | 哨兵节点刷新查询错对象（`RedisSentinel.GetNodes`） | 切换后数据连接已指向主节点，再查 `INFO Sentinel` 打在主节点上取不到监控信息，节点刷新返回空 | 改为显式遍历 `Redis.Server` 原始哨兵地址逐地址查询，失败跳过 |

---

## 3. 已知边界与收口记录（只增不删）

| 项 | 位置 | 现状与理由 |
|----|------|-----------|
| JSON 主机 AOT 回退 | `RedisEncoder.cs` | 已收口：由反射静默降级改为 `RuntimeFeature.IsDynamicCodeSupported` 显式守卫选择；AOT 回退 FastJson 属有意设计（STJ 泛型集合转换器在原生编译缺代码），详见 §2.4 |
| 运行期类型 JSON 路径 | `Message.GetBody<T>` 等 | 泛型 T 已按 `PublicProperties \| PublicParameterlessConstructor` 保根；**嵌套/属性类型不在注解覆盖面内**，要求调用方以泛型入口（如 `JsonHelper.ToJsonEntity<T>`）或静态引用保根——AOT 冒烟以「嵌套类型构造函数被裁剪」的实测案例固化该边界 |
| 协议数组转换 | `RedisClient.TryConvert` | 常见元素类型走静态构造；运行期动态构造的泛型实例化元素类型由 ILC 按静态引用保根，未保根时运行期失败，边界已抑制声明 |
| 动态能力 | 与 NewLife.Core 一致 | 脚本/插件/DLR/表达式编译等动态能力在 AOT 下按空值或明确异常降级，不静默错 |

---

## 4. 实施记录（原待办清单，已全部完成）

| # | 工作项 | 落地内容 | 状态 |
|---|--------|---------|------|
| 1 | net8.0 目标框架 | 主库新增 net8.0（同一套源码）；主库与扩展包常驻启用 `IsAotCompatible` | ✅ |
| 2 | AOT 告警清零 | 6 条告警全部处置（注解传导 + 边界声明 + 静态数组构造），分析器与 ILC 均 0 告警 | ✅ |
| 3 | 冒烟工程入仓 | `AotSmoke/`（23 场景，含 RESP3、可靠队列、泛型弹列、延迟队列、事件总线、多消费组队列、PubSub、Message 字段解码与集群/哨兵拓扑），纳入解决方案 | ✅ |
| 4 | CI 防回归 | `.github/workflows/aot.yml`：分析器告警阻断 + linux-x64 NativeAOT 发布运行冒烟 | ✅ |
| 5 | 文档与声明 | 本文档更新；Readme 中英文增补 NativeAOT 支持说明 | ✅ |
| 6 | 注解垫片清理 | 删除本地 `Common/AotAnnotations.cs`，旧 TFM 注解特性统一由 NewLife.Core 公开存根提供，384 条 CS0436 类型重复告警清零 | ✅ |
| 7 | CI 拓扑实跑 | `aot.yml` 冒烟任务自动搭建集群（7000-7002）与哨兵（主 7480 / 哨兵 26379）；`AotSmoke` 支持 `AOT_REQUIRE_TOPO=1` 严格模式（拓扑缺失按场景失败处理），CI 23 场景全实跑不再 SKIP | ✅ |

---

## 5. 复现方法

```powershell
# 1) 库侧分析器（期望 0 条 IL 告警）
dotnet build NewLife.Redis\NewLife.Redis.csproj -f net8.0 -c Release
dotnet build NewLife.Redis.Extensions\NewLife.Redis.Extensions.csproj -f net8.0 -c Release

# 2) 冒烟（JIT 基线；需本机 Redis 127.0.0.1:6379）
dotnet run -c Release --project AotSmoke\AotSmoke.csproj

# 2.1) 可选：拓扑场景（集群 7000-7002 / 哨兵主 7480 + 哨兵 26379），未运行时自动 SKIP；CI 由 aot.yml 自动搭建并以 AOT_REQUIRE_TOPO=1 严格实跑
#      启动实例：集群节点 conf 需 cluster-enabled yes；哨兵 conf 需 sentinel monitor <主名> 127.0.0.1 7480 1，经 redis-server <conf> --sentinel 启动（Ubuntu 的 redis-server 包不含独立 redis-sentinel 可执行文件）
redis-cli --cluster create 127.0.0.1:7000 127.0.0.1:7001 127.0.0.1:7002 --cluster-replicas 0 --cluster-yes

# 3) 冒烟（NativeAOT 真编译；期望 0 条 IL 告警）
dotnet publish AotSmoke\AotSmoke.csproj -c Release -r win-x64 -o out
.\out\AotSmoke.exe    # 逐场景 PASS/FAIL，非 0 退出码即失败
```

> CI：`aot.yml` 在 linux-x64 上重复执行上述校验（Redis 6379 由 redis-github-action 提供，集群/哨兵拓扑由工作流自动搭建并以 `AOT_REQUIRE_TOPO=1` 严格实跑）。
