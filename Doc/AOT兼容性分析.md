# NewLife.Redis AOT 兼容性分析

> 基线：v6.6 代码库 + NewLife.Core 12.0.2026.1009-beta0916｜评估方式：**分析器扫描 + NativeAOT 真实编译与运行实测**（win-x64，.NET 10，Redis 6.2.5）｜评估日期：2026-10-09

---

## 1. 现状摘要

- **下游使用已实测可用**：NativeAOT 应用经 netstandard2.1 资产使用本库，核心链路 **13/13 场景 JIT/AOT 双通**（连接、字符串、计数、哈希、列表、队列、Stream、JSON 对象与数组）。真 AOT 判真：`Assembly.Location` 为空、`IsDynamicCodeSupported=False`。
- **库侧尚未达标**：主库目标框架为 net45/net461/netstandard2.0/netstandard2.1，不含 net8.0+，未启用 `IsAotCompatible` 分析器常驻校验，也没有发布冒烟与 CI 门禁。
- **分析器实测 6 条 AOT 告警**（net8.0 + IsAotCompatible，0 错误）：集中于消息解码（Message.GetBody）、可靠队列消费（ConsumeAsync）、阻塞弹列（BRPOP/BLPOP）与协议数组转换（RedisClient）。
- **结论**：功能层面 AOT 可用；距离"库侧达标"还差 5 项工程化工作（§4），核心是"新增 net8.0 目标框架 + 告警清零"。

---

## 2. 实测数据

### 2.1 冒烟矩阵（13 场景，JIT/AOT 双通）

| # | 场景 | # | 场景 |
|---|------|---|------|
| 1 | 连接与服务器信息（INFO/版本） | 8 | 自定义对象 Redis 存取（编码器链路） |
| 2 | 字符串 Set/Get | 9 | 复杂类型字典 JSON 存取（`Dictionary<String,Int32>`） |
| 3 | 整数 INCR | 10 | Object 值字典 JSON 存取（`Dictionary<String,Object>`） |
| 4 | Hash 字典 | 11 | Stream 追加/读取（`GetStream<T>`） |
| 5 | List 列表 | 12 | 列表范围读取（LRANGE→`T[]`，Array 转换分支） |
| 6 | 队列 生产/消费（`GetQueue<T>`） | 13 | JSON 数组 存取（`T[]` 经编码器） |
| 7 | JsonHelper 泛型往返 | | |

> 环境：.NET SDK 10.0.401 win-x64 + 本机 Redis 6.2.5；冒烟工程 `<PublishAot>true</PublishAot>`（JIT/AOT 运行均固定严格 AOT 姿态：`IsDynamicCodeSupported=False`）。
> 原生发布件 `AotSmoke.exe` 约 9.4MB，逐场景 PASS/FAIL，非 0 退出码即失败。

### 2.2 分析器扫描（库侧，net8.0 + IsAotCompatible）

- 结果：**0 错误；AOT 告警 6 条**（其余 80 条为存量可空性等警告）

| # | 位置 | 编号 | 说明 | 处置方向 |
|---|------|------|------|---------|
| 1 | `FullRedis.cs:1213`（BRPOP&lt;T&gt;） | IL2087 | `JsonHost.Read(rs[1], typeof(T), null)`，T 缺少与目标参数匹配的保留要求 | 给泛型参数 T 加 `DynamicallyAccessedMembers(PublicParameterlessConstructor/PublicFields/PublicProperties)` 注解传导 |
| 2 | `FullRedis.cs:1252`（BLPOP&lt;T&gt;） | IL2087 | 同上 | 同上 |
| 3 | `Queues/QueueExtensions.cs:185`（ConsumeAsync&lt;T&gt;） | IL2091 | `JsonHost.Convert<T>(dic)`，T 缺少保留注解 | 同上（T 注解传导，或对边界方法声明裁剪语义） |
| 4 | `Queues/Message.cs:66`（Message.GetBody&lt;T&gt;） | IL2091 | `Activator.CreateInstance<T>()`，T 缺少 PublicParameterlessConstructor | T 注解传导，或改用 `Reflect.CreateInstance<T>` 泛型入口 |
| 5 | `Queues/Message.cs:82`（Message.GetBody&lt;T&gt;） | IL2072 | 属性类型经反射（`PropertyInfo.PropertyType`）进入 `JsonHelper.ToJsonEntity(String, Type)`，无法静态传导 | 该分支显式声明裁剪语义，或改由泛型入口承载保留要求 |
| 6 | `RedisClient.cs:1408`（TryConvert 数组分支） | IL3050 | `Array.CreateInstance(elmType, n)` 属 `RequiresDynamicCode` | 常见元素类型走显式数组构造分支，其余类型声明动态语义边界 + 运行守卫 |

### 2.3 发布期（ILC）告警

- 冒烟发布（win-x64）ILC 告警：**可达路径上库代码 2 条**（均为 `Message.GetBody<T>`，对应 §2.2 #4/#5），其余库代码 0 条（另有冒烟工程自身 2 条 IL3000，属探针代码）。
- 口径差异：分析器按**库内全量**报告（6 条），ILC 按**下游可达性**报告（2 条）；下游可达性不可控，**库侧 IsAotCompatible 分析器是必要门禁**。

### 2.4 关键差异观察（JIT vs AOT）

| 指标 | JIT | AOT |
|------|-----|-----|
| `RedisJsonEncoder.JsonHost` 实际主机 | `SystemJson`（反射加载成功） | `FastJson`（反射加载被 AOT 阻断，自动降级） |
| `JsonHelper.Default` | `FastJson` | `FastJson` |
| 13 场景结果 | 全通过 | 全通过 |

- `RedisJsonEncoder.GetJsonHost` 通过 `GetTypeEx("…SystemJson")` + `CreateInstance()` 反射优选 SystemJson；AOT 下类型发现失败，按空值降级为 `JsonHelper.Default`（FastJson），**设计容错、不抛异常**。两种主机实测均可完成本库全部 JSON 场景。
- **建议**：net8.0+ 目标改为编译期静态选择 JSON 主机（或显式保留 SystemJson 类型），消除 JIT/AOT 的序列化主机差异（见 §4-2）。

---

## 3. 已知边界与收口记录（只增不删）

| 项 | 位置 | 现状与理由 |
|----|------|-----------|
| JSON 主机 JIT/AOT 差异 | `RedisEncoder.cs` | 反射优选 SystemJson，AOT 降级 FastJson；实测两主机全场景可用，暂容忍，待 §4-2 一并收口 |
| 运行期类型 JSON 路径 | `RedisEncoder`/`JsonHost` | 自定义类型经静态引用（`typeof(T)`/泛型可达）保根时可正常往返；仅以运行期 `Type` 进入且未被静态引用的类型需 `JsonSerializerContext` 透传（NewLife.Core v12 IMP-28），未声明类型回退默认解析器 |
| 协议数组转换 | `RedisClient.TryConvert` | `Order[]` 等常见元素类型实测在 AOT 下可用；动态构造的泛型实例化元素类型可能不可用，属 IL3050 提示范围 |
| 动态能力 | 与 NewLife.Core 一致 | 脚本/插件/DLR/表达式编译等动态能力在 AOT 下按空值或明确异常降级，不静默错 |

---

## 4. 待办工作清单（评估结论）

| # | 工作项 | 内容 | 规模 |
|---|--------|------|------|
| 1 | 主库新增 net8.0 目标框架 | csproj 增加 net8.0（同一套源码），并对其常驻启用 `IsAotCompatible`；NuGet 包与 CI 构建矩阵同步 | 小 |
| 2 | AOT 告警清零 | 按 §2.2 处置表逐条处理（含 §2.4 JSON 主机静态化），目标分析器 0 告警、发布 0 库告警 | 中 |
| 3 | 冒烟工程入仓 | `AotSmoke`（JIT/AOT 双跑）；以 §2.1 的 13 场景为基础，后续补可靠队列/事件总线/集群等 | 小 |
| 4 | CI 防回归 | 新增 `aot.yml`：win-x64 发布 + 运行冒烟（复用 redis-github-action），分析器告警改阻断 | 小 |
| 5 | 文档与声明 | 本文档持续维护；实现完成后在 Readme 增补 NativeAOT 支持说明 | 小 |

---

## 5. 复现方法

```powershell
# 1) 库侧分析器（需 csproj 增加 net8.0 + IsAotCompatible，见 §4-1）
dotnet build NewLife.Redis\NewLife.Redis.csproj -f net8.0 -c Release

# 2) 冒烟（JIT 基线）
dotnet run -c Release --project AotSmoke\AotSmoke.csproj

# 3) 冒烟（NativeAOT 真编译；期望库代码 0 条 ILC 告警）
dotnet publish AotSmoke\AotSmoke.csproj -c Release -r win-x64 -o out
.\out\AotSmoke.exe    # 逐场景 PASS/FAIL，非 0 退出码即失败
```

> 前置：本机 Redis（127.0.0.1:6379）；AotSmoke 工程需 `<PublishAot>true</PublishAot>` 并引用本库（当前经 netstandard2.1 资产）。
