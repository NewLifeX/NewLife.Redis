# NewLife.Redis.Extensions

![Nuget](https://img.shields.io/nuget/v/NewLife.Redis.Extensions?logo=nuget)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis.Extensions?logo=nuget)

## ASP.NET Core Redis Deep Integration Extensions

`NewLife.Redis.Extensions` is the official ASP.NET Core integration package for [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis), providing out-of-the-box support for `IDistributedCache` distributed caching, `IDataProtection` key persistence, and automatic DI registration.

> 🆕 This is the successor to the deprecated `NewLife.Extensions.Caching.Redis` (v5.5). If you are using the old package, see the [Migration Guide](#migrating-from-the-old-package).

---

## Feature Overview

| Feature | Description |
|---------|-------------|
| `IDistributedCache` | ASP.NET Core standard distributed cache interface |
| `IDataProtection` | Persist data protection keys to Redis (`PersistKeysToRedis`) |
| DI Integration | Single `AddDistributedRedisCache` call registers all services |
| Inherits FullRedis | `RedisCache` inherits `FullRedis` — full data structure access |
| Broad Framework Coverage | netcoreapp3.1 / net5.0 ~ net10.0 / netstandard2.0 / netstandard2.1 |

---

## Installation

```bash
dotnet add package NewLife.Redis.Extensions
```

---

## Quick Start

### Register Services

```csharp
using NewLife.Redis.Extensions;

var builder = WebApplication.CreateBuilder(args);

// One-liner: registers FullRedis / IDistributedCache / ICache / ICacheProvider
builder.Services.AddDistributedRedisCache(options =>
{
    options.Server   = "127.0.0.1:6379";
    options.Password = "pass";
    options.Db       = 0;
    options.Prefix   = "myapp:";
});
```

### Using IDistributedCache

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

### Using FullRedis for Complete Redis Access

Since `RedisCache` inherits from `FullRedis`, you can use all Redis data structures directly after injection:

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

### Data Protection Key Persistence

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToRedis(redis, "DataProtection-Keys");
```

> For more Redis operations, see the [NewLife.Redis main documentation](https://github.com/NewLifeX/NewLife.Redis).

---

## Migrating from the Old Package

This is the direct successor to `NewLife.Extensions.Caching.Redis` (v5.5), offering better architecture and more features:

| Comparison | NewLife.Extensions.Caching.Redis (Old) | NewLife.Redis.Extensions (New) |
|------------|---------------------------------------|-------------------------------|
| Version Series | v5.5 | v6.5+ |
| IDistributedCache | ✅ | ✅ |
| IDataProtection Persistence | ❌ | ✅ |
| Framework Coverage | net461; netstandard2.0; netstandard2.1 | netcoreapp3.1 ~ net10.0; netstandard2.0/2.1 |
| RedisCache Base Class | Standalone class, wraps FullRedis | **Inherits FullRedis**, full API available |
| Namespace | `NewLife.Extensions.Caching.Redis` | `NewLife.Redis.Extensions` |

**Migration Steps:**

1. Uninstall the old package, install the new one
2. Change namespace: `NewLife.Extensions.Caching.Redis` → `NewLife.Redis.Extensions`
3. Switch DI registration: `services.AddRedis(configString)` → `services.AddDistributedRedisCache(options => {...})`

---

## Related Links

- [NewLife.Redis Main Repository](https://github.com/NewLifeX/NewLife.Redis)
- [NewLife.Redis Documentation (Chinese)](https://github.com/NewLifeX/NewLife.Redis/blob/master/Readme.MD)
- [NuGet Package](https://www.nuget.org/packages/NewLife.Redis.Extensions/)
- [NewLife Team Website](https://newlifex.com)
