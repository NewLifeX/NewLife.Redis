# NewLife.Redis — عميل Redis عالي الأداء

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)


![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Redis?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Redis?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Redis/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Redis/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64%20%7C%20loongarch64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Redis?logo=github)](https://github.com/NewLifeX/NewLife.Redis/graphs/contributors)


<!-- lang:start -->
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md)
<!-- lang:end -->

---

`NewLife.Redis` هو عميل Redis لـ .NET **عالي الأداء وعالي الإنتاجية وسهل الدمج**، طوره فريق NewLife. منذ عام 2017، يعمل بشكل مستقر على منصات إنتاج واسعة النطاق، ويعالج أكثر من **80 مليار** استدعاء أمر يوميًا.

## بداية سريعة

```bash
dotnet add package NewLife.Redis
```

```csharp
using NewLife.Caching;

// الاتصال
var redis = new FullRedis("server=127.0.0.1:6379;password=yourpwd;db=0");

// العمليات الأساسية
redis.Set("key", "value");
var val = redis.Get<String>("key");

// انتهاء صلاحية المفتاح
redis.Set("temp", data, 60); // 60 ثانية
```

## الميزات الرئيسية

- **بروتوكول كامل**: RESP2/RESP3، جميع أوامر Redis 2.8~7.x
- **جميع هياكل البيانات**: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog
- **طوابير الرسائل**: بسيطة، موثوقة (RPOPLPUSH+Ack)، مؤجلة، Stream متعددة المجموعات
- **التجمع والتوفر العالي**: Standalone / Sentinel / Cluster، تجاوز فشل تلقائي
- **التكيف السحابي**: Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS
- **دعم واسع للإطار**: .NET Framework 4.5 ~ .NET 10+
- **توافق Garnet**: كشف تلقائي لـ Microsoft Garnet / kvrocks، تدهور سلس

## حزم التمديد

| الحزمة | الوصف |
|-----------|------|
| `NewLife.Redis` | عميل Redis الأساسي |
| `NewLife.Redis.Extensions` | حقن التبعية ASP.NET Core، `IDistributedCache`، `IDataProtection` |

## الوثائق

> 📖 الوثائق الكاملة (بالصينية): [المتطلبات](Doc/需求文档.md) | [الهندسة](Doc/架构文档.md) | [تحليل المنافسين](Doc/竞品分析.md)

## الترخيص

MIT License © 2002-2026 [فريق تطوير NewLife](https://newlifex.com)

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [الموقع الرسمي](https://newlifex.com)
