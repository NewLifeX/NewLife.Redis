# NewLife.Redis.Extensions

![Nuget](https://img.shields.io/nuget/v/NewLife.Redis.Extensions?logo=nuget)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis.Extensions?logo=nuget)

## ASP.NET Core Redis 深度集成扩展

`NewLife.Redis.Extensions` 是 [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) 为 ASP.NET Core 提供的官方集成扩展包，提供 `IDistributedCache` 分布式缓存、`IDataProtection` 密钥持久化、DI 自动注入等开箱即用能力。

> 🆕 本包是旧包 `NewLife.Extensions.Caching.Redis`（v5.5）的继任者。如果你正在使用旧包，请参阅[迁移指引](#从旧包迁移)。

---

## 功能概览

| 功能 | 说明 |
|------|------|
| `IDistributedCache` | ASP.NET Core 分布式缓存标准接口实现 |
| `IDataProtection` | 数据保护密钥持久化到 Redis（`PersistKeysToRedis`） |
| DI 依赖注入 | 一行 `AddDistributedRedisCache` 自动注册全部服务 |
| 继承 FullRedis | `RedisCache` 继承 `FullRedis`，可直接调用全部 Redis 数据结构 |
| 宽框架覆盖 | netcoreapp3.1 / net5.0 ~ net10.0 / netstandard2.0 / netstandard2.1 |

---

## 安装

```bash
dotnet add package NewLife.Redis.Extensions
```

---

## 快速开始

### 注册服务

```csharp
using NewLife.Redis.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 一行注册，自动注入 FullRedis / IDistributedCache / ICache / ICacheProvider
builder.Services.AddDistributedRedisCache(options =>
{
    options.Server   = "127.0.0.1:6379";
    options.Password = "pass";
    options.Db       = 0;
    options.Prefix   = "myapp:";
});
```

### 使用 IDistributedCache

```csharp
public class HomeController : Controller
{
    private readonly IDistributedCache _cache;

    public HomeController(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var value = await _cache.GetStringAsync("greeting");
        if (value == null)
        {
            value = "Hello, Redis!";
            await _cache.SetStringAsync("greeting", value,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
        }
        return Content(value);
    }
}
```

### 使用 FullRedis 完整功能

由于 `RedisCache` 继承自 `FullRedis`，注入后可直接使用全部 Redis 数据结构：

```csharp
public class UserService
{
    private readonly FullRedis _redis;

    public UserService(FullRedis redis)
    {
        _redis = redis;
    }

    public void SaveUser(User user)
    {
        var list = _redis.GetList<User>("users");
        list.Add(user);

        var hash = _redis.GetDictionary<String>("user:" + user.Id);
        hash["Name"]  = user.Name;
        hash["Email"] = user.Email;
    }
}
```

### 数据保护密钥持久化

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToRedis(redis, "DataProtection-Keys");
```

> 更多 Redis 操作请参考 [NewLife.Redis 主文档](https://github.com/NewLifeX/NewLife.Redis)。

---

## 从旧包迁移

本包是 `NewLife.Extensions.Caching.Redis`（v5.5）的直接继任者，提供了更好的架构和更多功能：

| 对比项 | NewLife.Extensions.Caching.Redis（旧） | NewLife.Redis.Extensions（新） |
|--------|---------------------------------------|-------------------------------|
| 版本系列 | v5.5 | v6.5+ |
| IDistributedCache | ✅ | ✅ |
| IDataProtection 持久化 | ❌ | ✅ |
| 框架覆盖 | net461; netstandard2.0; netstandard2.1 | netcoreapp3.1 ~ net10.0; netstandard2.0/2.1 |
| RedisCache 基类 | 独立类，包装 FullRedis | **继承 FullRedis**，全功能可用 |
| 命名空间 | `NewLife.Extensions.Caching.Redis` | `NewLife.Redis.Extensions` |

**迁移步骤：**

1. 卸载旧包、安装新包
2. 更换命名空间：`NewLife.Extensions.Caching.Redis` → `NewLife.Redis.Extensions`
3. 更换 DI 注册方式：`services.AddRedis(configString)` → `services.AddDistributedRedisCache(options => {...})`

---

## 相关链接

- [NewLife.Redis 主仓库](https://github.com/NewLifeX/NewLife.Redis)
- [NewLife.Redis 中文文档](https://github.com/NewLifeX/NewLife.Redis/blob/master/Readme.MD)
- [NuGet 包](https://www.nuget.org/packages/NewLife.Redis.Extensions/)
- [新生命团队官网](https://newlifex.com)
