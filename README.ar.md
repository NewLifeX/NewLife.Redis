# NewLife.Redis — عميل Redis عالي الأداء

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)

🌐 [中文](Readme.MD) | [English](Readme.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Русский](README.ru.md) | [Português](README.pt.md)

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
