# NewLife.Extensions.Caching.Redis

![Nuget](https://img.shields.io/nuget/v/NewLife.Extensions.Caching.Redis?logo=nuget)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Extensions.Caching.Redis?logo=nuget)

## ⚠️ 该包已停止维护

`NewLife.Extensions.Caching.Redis` 已进入**仅维护模式**，不再新增功能。请迁移至新的替代包：

👉 **[NewLife.Redis.Extensions](https://www.nuget.org/packages/NewLife.Redis.Extensions/)** — 功能更全面，框架支持更宽。

---

## 为什么迁移？

| 对比项 | NewLife.Extensions.Caching.Redis（旧） | NewLife.Redis.Extensions（新） |
|--------|---------------------------------------|-------------------------------|
| 版本 | v5.5 | v6.5+ |
| IDistributedCache | ✅ | ✅ |
| IDataProtection 持久化 | ❌ | ✅（PersistKeysToRedis） |
| 框架支持 | net461; netstandard2.0; netstandard2.1 | netcoreapp3.1 ~ net10.0; netstandard2.0/2.1 |
| 依赖注入（AddRedis） | ✅ | ✅（增强：AddDistributedRedisCache） |
| 命名空间 | `NewLife.Extensions.Caching.Redis` | `NewLife.Redis.Extensions` |
| 连接池复用（继承 FullRedis） | ❌ 包装模式 | ✅ 继承 FullRedis |

---

## 如何迁移？

### 1. 更换 NuGet 包

```bash
# 卸载旧包
dotnet remove package NewLife.Extensions.Caching.Redis

# 安装新包
dotnet add package NewLife.Redis.Extensions
```

### 2. 更新命名空间

```csharp
// 旧
using NewLife.Extensions.Caching.Redis;

// 新
using NewLife.Redis.Extensions;
```

### 3. 更新服务注册（如使用 DI）

```csharp
// 旧方式
services.AddRedis("server=127.0.0.1:6379;password=pass;db=0");

// 新方式（更完整的 DI 集成）
services.AddDistributedRedisCache(options =>
{
    options.Server   = "127.0.0.1:6379";
    options.Password = "pass";
    options.Db       = 0;
    options.Prefix   = "myapp:";
});
// 自动注册 FullRedis / IDistributedCache / ICache / ICacheProvider
```

> 新包中的 `RedisCache` 直接继承 `FullRedis`，你可以像使用 `FullRedis` 一样使用它（List、Hash、Set、消息队列等全部可用），而旧包只是内部包装了一个 `FullRedis` 实例。

---

## 最后版本信息

- **最后版本**：v5.5 系列
- **NuGet**：[NewLife.Extensions.Caching.Redis](https://www.nuget.org/packages/NewLife.Extensions.Caching.Redis/)
- **源码**：[GitHub](https://github.com/NewLifeX/NewLife.Redis/tree/master/NewLife.Extensions.Caching.Redis)

> 如果你仍在生产环境使用此包且无法升级，旧包仍可继续安装使用，但建议规划迁移。
