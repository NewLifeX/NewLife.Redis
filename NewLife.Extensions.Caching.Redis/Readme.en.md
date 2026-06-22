# NewLife.Extensions.Caching.Redis

![Nuget](https://img.shields.io/nuget/v/NewLife.Extensions.Caching.Redis?logo=nuget)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Extensions.Caching.Redis?logo=nuget)

## ⚠️ This Package is Deprecated

`NewLife.Extensions.Caching.Redis` has entered **maintenance-only mode** and will not receive new features. Please migrate to the replacement package:

👉 **[NewLife.Redis.Extensions](https://www.nuget.org/packages/NewLife.Redis.Extensions/)** — more features, broader framework support.

---

## Why Migrate?

| Comparison | NewLife.Extensions.Caching.Redis (Old) | NewLife.Redis.Extensions (New) |
|------------|---------------------------------------|-------------------------------|
| Version | v5.5 | v6.5+ |
| IDistributedCache | ✅ | ✅ |
| IDataProtection Persistence | ❌ | ✅ (`PersistKeysToRedis`) |
| Framework Support | net461; netstandard2.0; netstandard2.1 | netcoreapp3.1 ~ net10.0; netstandard2.0/2.1 |
| DI Registration (`AddRedis`) | ✅ | ✅ (enhanced: `AddDistributedRedisCache`) |
| Namespace | `NewLife.Extensions.Caching.Redis` | `NewLife.Redis.Extensions` |
| Connection Pool Reuse (Inherits FullRedis) | ❌ Wrapper pattern | ✅ Inherits `FullRedis` |

---

## How to Migrate

### 1. Swap the NuGet Package

```bash
# Uninstall old package
dotnet remove package NewLife.Extensions.Caching.Redis

# Install new package
dotnet add package NewLife.Redis.Extensions
```

### 2. Update Namespace

```csharp
// Old
using NewLife.Extensions.Caching.Redis;

// New
using NewLife.Redis.Extensions;
```

### 3. Update Service Registration (if using DI)

```csharp
// Old way
services.AddRedis("server=127.0.0.1:6379;password=pass;db=0");

// New way (more complete DI integration)
services.AddDistributedRedisCache(options =>
{
    options.Server   = "127.0.0.1:6379";
    options.Password = "pass";
    options.Db       = 0;
    options.Prefix   = "myapp:";
});
// Automatically registers FullRedis / IDistributedCache / ICache / ICacheProvider
```

> In the new package, `RedisCache` directly inherits `FullRedis`, so you can use it just like `FullRedis` (List, Hash, Set, message queues, etc. all available). The old package merely wraps a `FullRedis` instance internally.

---

## Last Version Info

- **Last Version**: v5.5 series
- **NuGet**: [NewLife.Extensions.Caching.Redis](https://www.nuget.org/packages/NewLife.Extensions.Caching.Redis/)
- **Source**: [GitHub](https://github.com/NewLifeX/NewLife.Redis/tree/master/NewLife.Extensions.Caching.Redis)

> If you are still using this package in production and cannot upgrade, the old package remains installable, but migration is recommended.
