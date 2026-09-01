# NewLife.Redis 变更日志

## v6.6.2026.0901 (2026-09-01)

### Bug 修复
- **[fix]** 修复 `FullRedis` `Eval`/`FCall`/`FCallRO` 参数组装为数组，避免参数数量错误，提升分布式主 key 路由准确性；补充只读函数、Lua 脚本参数展开、空参数等边界场景单元测试

### 安全与依赖
- **依赖包升级**：`Microsoft.Extensions` 系列按各目标框架主版本升级至最新补丁，`net10.0` DataProtection 由 10.0.9 升至 10.0.11，消除 `System.Security.Cryptography.Xml` 高危漏洞；`NewLife.Core`/SourceLink/测试组件等同步升级

---

## v6.6.2026.0802 (2026-08-02)

### 新特性
- **连接池配置与健康检查**：新增 `RedisPoolConfig` 配置类，支持最小空闲数/最大容量/空闲清理时间/最大生命周期/借出等待超时等参数；借出连接时对空闲超时的连接主动 PING 验证存活，提升长连接可靠性
- **命令组包预编码优化**：`GetCommandSize` 对复杂对象预编码并缓存复用，批量命令整体预编码，避免重复序列化，提升命令发送性能

### Bug 修复
- **[fix]** 修复 `Search` 系列 `count=-1`（默认全部）返回空的问题，通配符删除 `Remove("key*")` 恢复可用
- **[fix]** 修复 RedLock 毫秒级锁永不过期：`msExpire < 1000` 时整数除法为 0 导致无过期时间，改用 `SET PX NX`
- **[fix]** 修复 `GetCommandSize` 值类型参数估算不足导致缓冲区溢出（DateTime/大Decimal 参数序列化超过固定 20 字节估算）
- **[fix]** 修复集群 `Execute(String[])` 原地修改调用方数组、结果乱序、`List.Contains` O(n²) 去重问题
- **[fix]** 修复集群 `Search` 跨节点共享 SCAN 游标导致重复/漏键
- **[fix]** 修复扩展包 Async 假异步：`Task.Run` 包装同步实现改为真异步 `ExecuteAsync`，支持取消且不占线程池
- **[fix]** 修复 `ExecuteAsync` 重试使用 `Thread.Sleep` 阻塞线程池线程，改为 `Task.Delay`
- **[fix]** 修复队列阻塞消费运行时修改全局 `Redis.Timeout` 的线程安全问题，改为客户端级超时/链接令牌
- **[fix]** 修复 `ReadLine` 超长响应行静默截断（256 字符），超长降级 StringBuilder
- **[fix]** 修复 `ExecuteCommand` 内存池 buffer 异常时未归还、PubSub 连接异常时未归还连接池
- **[fix]** 修复连接池 catch 中销毁 client 后仍归还已销毁对象的问题

### 架构优化
- **RedLock 合并单实现**：`FullRedis.AcquireRedLock` 委托 `RedisRedLock`，消除双实现逻辑漂移
- **删除 PrefixedRedis 废弃类**：功能已被 `FullRedis.Prefix` 取代，破坏性变更
- **移除 `IncrementWithTtl`/`DecrementWithTtl` 方法**：删除累加/递减并获取 TTL 的 API，与基类保持一致（破坏性变更）
- **标记 `AddPrefixedRedis` 过时**：`[Obsolete]` 标注，改用支持 Prefix 的 `AddRedis` 注册

### 优化改进
- **RedisHash/RedisSet 全量遍历**：支持 `count<=0` 全量扫描，批量拉取数提升至 1000，提升大数据量遍历效率

### 测试与质量
- 新增 Search/通配符删除/批量入参不修改/RedLock 毫秒锁/值类型参数估算等回归测试
- 修复 `RedisCacheTests`/`DependencyInjectionTest` 硬编码 `127.0.0.1` 导致无法使用配置 Redis 的问题

---

## v6.5.2026.0702 (2026-07-02)

### 新特性
- **NuGet 安全审计**：启用 NuGet 审计，自动扫描依赖包已知漏洞，提升供应链安全
- **Redis 6.2~7.4+ 命令补全**：补全更多 Redis 命令与 API，提升命令覆盖率
- **阿里云 Tair 扩展**：支持 TairString/TairHash 等阿里云 Tair 企业版专属数据结构

### 测试与质量
- **RedisFact 智能测试特性**：引入 RedisFact 特性，自动跳过不可用 Redis 集成测试，提升 CI 测试稳定性
- **Redis 版本与 ACL 兼容**：增强对不同 Redis 版本和 ACL 认证场景的兼容性，优化测试健壮性
- **StreamInfo 解析增强**：增强 StreamInfo 等返回信息解析的健壮性，补充单元测试覆盖

### 优化改进
- **Stream/EventBus 埋点增强**：优化 RedisStream 消费埋点与链路追踪，增强 EventBus 消费循环启动与销毁逻辑
- **扩展包文档独立**：为各扩展包添加独立 Readme，标记旧包 `NewLife.Extensions.Caching.Redis` 为废弃
- **分布式锁完善**：支持 Redis 7.0+ 新特性与多云兼容场景下的分布式锁

---

## v6.5.2026.0601 (2026-06-01)

### 发布维护
- **稳定性维护发布**：6 月例行维护发版，无新增公共 API，保持与 6.5 系列完全兼容
- **持续交付**：延续上版对 RedisStream `XINFO` 异常恢复与 `ConsumeAsync` 健壮性增强成果

---

## v6.5.2026.0501 (2026-05-01)

### Bug 修复
- **[fix]** 修复 RedisStream 在 `XINFO` 返回 `ERR no such key` 时的错误处理，增强 `ConsumeAsync` 自动恢复能力

---

## v6.5.2026.0302 (2026-02-02 ~ 2026-03-02)

### 新特性
- 支持RESP3协议并优化核心性能分配
- 增强Garnet兼容性识别与文档说明
- 新增RedisSwitch示例项目（消息队列收发）

### 问题修复
- 修复代码评审中发现的问题（UTC时间、重试逻辑、冗余检查、未使用属性）

### 优化改进
- 重构基准测试，优化文档与启动流程
- 重构文档体系，提升可维护性

---

## v6.4.2026.0201 (2026-02-01)

### 新特性
- RedisStream队列增加信息展示功能
- 支持自动删除空闲消费组

### 问题修复
- 修正FullRedis.CreateEventBus方法
- 修复ShowInfo显示问题

### 优化改进
- 删除空闲消费组时，要求消费者为空，增强安全性

---

## v6.4.2026.0102 (2026-01-02)

### 优化改进
- 优化事件总线支持
