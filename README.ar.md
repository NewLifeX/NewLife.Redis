# NewLife.Redis — مكوّن عميل Redis عالي الأداء

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Redis?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Redis?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Redis/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Redis/actions/workflows/test.yml)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Redis?logo=github)](https://github.com/NewLifeX/NewLife.Redis/graphs/contributors)

<!-- lang:start -->
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md)
<!-- lang:end -->

`NewLife.Redis` هو مكوّن عميل Redis لـ .NET **عالي الأداء وعالي الإنتاجية وسهل الدمج**، طوّره فريق NewLife. منذ عام 2017 وهو يعمل بثبات على منصات إنتاج متعددة تتعامل مع مليارات السجلات ودرجات عالية من التزامن، ويعالج أكثر من **80 مليار** استدعاء أمر يوميًا.

> 📖 التوثيق الكامل: [المتطلبات (中文)](Doc/需求文档.md) | [البنية المعمارية (中文)](Doc/架构文档.md) | [قائمة الميزات (中文)](Doc/功能清单.md) | [تحليل المنافسين (中文)](Doc/竞品分析.md) | [توافق Garnet (中文)](Doc/Garnet兼容性.md) | [توافق AOT (中文)](Doc/AOT兼容性分析.md)

**المحتويات**: [بداية سريعة](#بداية-سريعة) · [الميزات الرئيسية](#الميزات-الرئيسية) · [خريطة المنظومة](#خريطة-المنظومة) · [التوثيق](#التوثيق) · [المساهمة](#المساهمة) · [الفريق](#الفريق) · [الترخيص](#الترخيص)

## بداية سريعة

### التثبيت

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### الاستخدام الأساسي

**موصى به: نسخة وحيدة (singleton) آمنة لمؤشرات الترابط، يعاد استخدامها طوال عمر التطبيق:**

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();

var rds = new FullRedis("127.0.0.1:6379", "password", 0)
{
    Log = XTrace.Log,           // Optional: component-level logging
    AutoPipeline = 100,         // Optional: auto-commit pipeline when command count reaches 100
};

// Write an object (auto JSON serialization)
rds.Set("user:1", new { Name = "Alice", Age = 30 }, expire: 3600);

// Read (auto deserialization)
var name = rds.Get<String>("user:1");
Console.WriteLine(name);
```

**وضع سلسلة الاتصال (موصى به عند استخدام مركز التهيئة):**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### دعم المنصات

| العنصر | التغطية |
| --- | --- |
| أطر العمل المستهدفة | حزمة واحدة: net45 وnet461 وnetstandard2.0 وnetstandard2.1 وnet8.0؛ وتضيف حزم الامتدادات netcoreapp3.1 → net10 |
| أنظمة التشغيل والمعماريات | Windows وLinux وmacOS وAndroid وiOS؛ x64 وarm64 وloongarch64 وriscv64 وs390x |
| NativeAOT | تأتي `net8.0` بفحوص `IsAotCompatible`؛ ويغطي `AotSmoke/` مع مصفوفة الفحص في CI منصات win-x64 / linux-x64 / linux-arm64 / linux-la64 (LA64 عبر QEMU)، مع ترجمة أصلية لمعرّفات win-arm64 وmacOS وmusl. منصة linux-x64 هي البوّابة المانعة، والباقي قيد المراقبة — انظر [توافق AOT](Doc/AOT兼容性分析.md) |

## الميزات الرئيسية

<details>
<summary><b>النواة الأساسية</b> (7)</summary>

| الميزة | الخصائص الرئيسية |
| --- | --- |
| مُجرَّب على نطاق واسع | أكثر من 200 نسخة Redis في بيئة الإنتاج، بذروة يومية تتجاوز 100 مليون كتابة لكائنات الأعمال وأكثر من 80 مليار استدعاء أمر. |
| زمن استجابة منخفض | تستغرق رحلة ذهاب وعودة واحدة لـ Get/Set من 200 إلى 600 ميكروثانية شاملًا الشبكة. |
| إنتاجية عالية | مجمّع اتصالات مدمج (حتى 100,000 اتصال متزامن) مع محلّل RESP متزامن وتجميع تلقائي اختياري للـ pipeline. |
| إعادة محاولة تلقائية وتجاوز فشل متعدد العناوين | قوائم عقد مفصولة بفواصل، وتجاوز سريع للفشل عند أخطاء الشبكة، ونافذة حجب افتراضية مدتها 10 ثوانٍ. |
| مرمّز قابل للتوصيل | JSON افتراضيًا (`RedisJsonEncoder`)، وقابل للتوسيع إلى تسلسل ثنائي عبر `IPacketEncoder`. |
| تتبّع APM | تتبّع موزّع عبر `ITracer` إضافةً إلى عدّادات الأداء `PerfCounter`. |
| بلا اعتماديات ثقيلة | لا شيء فوق BCL سوى منظومة NewLife (التسجيل والتهيئة والتسلسل والأمان). |

</details>

<details>
<summary><b>هياكل البيانات</b> (10)</summary>

| البنية | الخصائص الرئيسية |
| --- | --- |
| String KV + الدفعات | MGET / MSET / INCR / APPEND / GETRANGE وما شابهها. |
| List | LPUSH / RPOP / LRANGE / LMOVE. |
| Hash | HGET / HSET / HMGET / HSCAN. |
| Set | SADD / SMEMBERS / SUNION / SDIFF. |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN. |
| Stack | غلاف LIFO فوق List. |
| Stream | دعم كامل لـ XADD / XREAD / XGROUP / XACK. |
| Geo | GEOADD / GEODIST / GEOSEARCH. |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE. |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD (مخطط لها). |

</details>

<details>
<summary><b>طوابير الرسائل والنشر/الاشتراك</b> (7)</summary>

| النوع | الخصائص الرئيسية |
| --- | --- |
| `RedisQueue<T>` | طابور بسيط بدلالة تسليم مرة واحدة على الأكثر. |
| `RedisReliableQueue<T>` | تسليم مرة واحدة على الأقل عبر RPOPLPUSH + تراجع Ack. |
| `RedisDelayQueue<T>` | تسليم مؤجّل مدعوم بـ Sorted Set. |
| `RedisStream<T>` | Stream مرتّب مع مجموعات مستهلكين وXACK. |
| `MultipleConsumerGroupsQueue<T>` | بثّ إلى مجموعات مستهلكين متعددة. |
| `PubSub` + `RedisEventBus` | نشر/اشتراك في الزمن الحقيقي. |
| قفل RedLock الموزّع | خوارزمية Redlock متعددة النسخ (قيد التنفيذ). |

</details>

<details>
<summary><b>التجمع والتوفر العالي</b> (7)</summary>

| الوضع | الخصائص الرئيسية |
| --- | --- |
| Standalone | اتصال مباشر بنسخة واحدة بأصغر بصمة. |
| نسخ Master-Replica | فصل القراءة/الكتابة وتجاوز الفشل. |
| Sentinel | اكتشاف Sentinel مع تبديل تلقائي للنسخة الرئيسية. |
| Native Cluster | توجيه الفتحات بـ CRC16 مع إعادة توجيه MOVED / ASK. |
| kvrocks | تكيّف تلقائي مع محرك التخزين kvrocks. |
| Garnet | كشف تلقائي لـ Microsoft Garnet وتدهور سلس عند غياب إحدى الميزات. |
| Valkey | يُكشف من أمر INFO في الخادم. |

</details>

<details>
<summary><b>البروتوكول وتوافق الخوادم</b> (7)</summary>

| القدرة | الخصائص الرئيسية |
| --- | --- |
| RESP2 | تنفيذ كامل لأنواع Simple string وerror وinteger وbulk string وarray. |
| RESP3 | أنواع Map / Set / Double / Boolean / Null / Blob Error، يُتفق عليها عبر مصافحة HELLO (Redis 6.0+). |
| Redis 2.8 – 7.x | الأوامر الشائعة في كل إصدار، بما فيها COPY / GETDEL / GETEX / GEOSEARCH منذ 6.2. |
| مصادقة ACL | مصادقة Redis 6.0+ باسم المستخدم وكلمة المرور. |
| المعاملات | MULTI / EXEC / WATCH. |
| سكربتات Lua | EVAL / EVALSHA. |
| دوال Redis | FCALL / FUNCTION (مخطط لها). |

</details>

<details>
<summary><b>السحابة والأمان والرصد</b> (8)</summary>

| القدرة | الخصائص الرئيسية |
| --- | --- |
| Alibaba Cloud KVStore / Tair | إرشادات الإصدار القياسي وACL وSSL ووضع الوكيل، إضافةً إلى هياكل Tair (TairString / TairHash / TairZSet). |
| Tencent Cloud Redis | يستخدم معرّف النسخة اسمًا للمستخدم (`instanceId:password`). |
| Huawei Cloud DCS | فروق الإصدار الرئيسي/الاحتياطي وSSL وإصدار التجمع (لا يدعم SELECT في وضع التجمع). |
| TLS/SSL | إصدار بروتوكول قابل للتهيئة وTLS متبادل بشهادات العميل. |
| تسجيل منظّم | سجل المكوّن (`Log`) وسجل مستوى البروتوكول (`ClientLog`). |
| عدّادات الأداء | يعرض `PerfCounter` العمليات/الثانية وزمن الاستجابة ومعدّل الأخطاء. |
| تتبّع APM | امتدادات `ITracer` في كل مكان إضافةً إلى `ActivitySource` (net8.0+) لـ OpenTelemetry. |
| حماية سلسلة الاتصال | يفكّ `ProtectedKey` تشفير كلمات المرور المضمّنة في سلاسل الاتصال. |

</details>

<details>
<summary><b>المنصات والتكامل</b> (5)</summary>

| القدرة | الخصائص الرئيسية |
| --- | --- |
| أهداف متعددة | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0؛ وتغطي حزم الامتدادات netcoreapp3.1 → net10. |
| توقيع قوي | موقّع بـ `newlife.snk` بحيث يمكن إعادة بناء التجميعات واستبدالها. |
| NativeAOT | فحوص `IsAotCompatible` الساكنة، ومشروع `AotSmoke/`، ومصفوفة CI متعددة RID. |
| حزمة ASP.NET Core | يسجّل `NewLife.Redis.Extensions` كلا `IDistributedCache` و`FullRedis` عبر `AddRedisCaching`. |
| DataProtection | يحفظ `PersistKeysToRedis` مفاتيح حماية البيانات في Redis. |

</details>

## خريطة المنظومة

NewLife منظومة متعددة الطبقات من مشاريع مفتوحة المصدر تُصدَر بنسخ مستقلة. الخريطة أدناه مشتركة حرفيًا بين جميع مستودعات NewLife؛ وكل مشروع يرتبط بمستودعه وحزمة NuGet الأساسية الخاصة به.

| المشروع | الوظيفة | التنزيلات |
| --- | --- | --- |
| **الأساس** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | التسجيل والتهيئة والتخزين المؤقت والشبكات والتسلسل والجدولة وتتبّع أداء APM. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | وسيط بيانات: جداول واحدة بمستوى عشرات المليارات من الصفوف، تقسيم تلقائي للجداول، فصل القراءة عن الكتابة، وأكثر من 10 قواعد بيانات. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | مكتبة شبكات عالية الأداء: 1.77 مليون pps / 14.5 Gbps على جهاز واحد (TCP 1KB، 2026-10)، و4 ملايين اتصال TCP طويل الأمد. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | إطار اتصال بروتوكولي وRPC لتطبيقات العميل/الخادم وأجهزة إنترنت الأشياء. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | يثبّت تطبيقات .NET كخدمات حارسة لنظام التشغيل: خدمات Windows وsystemd في Linux. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | قوالب مشاريع `dotnet new` للطرفية وweb وAPI والخدمات والبيانات وإنترنت الأشياء. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **البيانات والمراسلة** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | عميل Redis بزمن استجابة بالميكروثانية، وملايين العمليات في الثانية، وقوائم رسائل مدمجة. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | عميل RocketMQ مُدار بالكامل لـ Apache RocketMQ وخدمة رسائل Alibaba Cloud. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | عميل وخادم MQTT، بما في ذلك الاتصال بـ Alibaba Cloud IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | قاعدة بيانات هجينة بلغة C#: مدمجة أو شبكية، وعلائقية، وسلاسل زمنية، وقائمة رسائل، وKV في واحد. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | برنامج تشغيل MySql وطبقة مساعدة مبنية على مكدّس بيانات NewLife. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web والذكاء الاصطناعي والأنظمة الموزّعة** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | إطار منصة إدارة سريعة: الصلاحيات، والدخول الموحّد SSO، وخادم OAuth، وتوليد واجهات CRUD. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | منصة خدمات موزّعة: إدارة العقد، وAPM، ومركز الإعدادات، ومركز التسجيل، ومركز النشر. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | منصة موزّعة للمهام وجدولة البيانات الفورية، مع التقسيم وإعادة المحاولة وإعادة التعبئة. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | عميل موحّد لـ 46 مزوّد LLM بالإضافة إلى ChatAI، تطبيق ويب جاهز للمحادثة. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **الصناعة وإنترنت الأشياء** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | مكتبة معايير إنترنت الأشياء التي تعرّف عقود البروتوكولات وتجريدات الأجهزة. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | تنفيذ Modbus TCP/RTU/ASCII فوق NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | بروتوكول Siemens PLC فوق NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | بروتوكول Omron PLC فوق NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | بروتوكول Mitsubishi Melsec PLC فوق NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | عميل OPC للوصول إلى البيانات الصناعية. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | مكدّس بروتوكول BACnet لأتمتة المباني. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | منصة إنترنت أشياء مفتوحة المصدر وخفيفة، مع إدارة البوابات والأجهزة. | — |
| **تليماتيكا المركبات** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | بروتوكول تحديد مواقع المركبات JT/T 808: أكثر من 70 جسم رسالة، 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | بروتوكول JT/T 809 بين المنصات لجهات إدارة النقل. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | فيديو داخل المركبة JT/T 1078: تحليل RTP وإعادة التجميع إلى FLV/fMP4 والبث المباشر. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **مكوّنات المجال** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | حزمة واحدة لـ Excel وWord وPPT وPDF وأكثر من 15 صيغة مستندات. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | صوت عبر المنصات: مرمّزات، وإدخال/إخراج الأجهزة، وDSP، وحاويات، وبث. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | مرمّزات الفيديو وصيغ الحاويات ومعالجة الإطارات والبث. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | واجهة موحّدة فوق Baidu وAMap وTencent وTianditu مع الترميز الجغرافي وتحويل الإحداثيات. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | تحديد الموقع الجغرافي لعناوين IPv4 بقاعدة بيانات غير متصلة مُعيّنة في الذاكرة، وواجهة API عبر الإنترنت كبديل. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **الأدوات والمنتجات** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | صندوق أدوات المطوّر: الشبكات والمنفذ التسلسلي والتشفير والتعبيرات النمطية وModbus وMQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | نظام استضافة ذاتية لإدارة الشيفرة المصدرية Git. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | يثبّت بيئة تشغيل .NET المطابقة تلقائيًا عند التشغيل من مضيف أقدم. | — |

> تعرض الشارات الحزمة الرئيسية لكل مشروع؛ أما حزم السمات والإضافات والحزم التابعة فمذكورة في صفحاتها على NuGet. الحزم التي تتجاوز 30 حزمة وغير المذكورة هنا (FTP وLoRa وإضافات Office وبرامج تشغيل البروتوكولات وغيرها) متاحة في [منظمة NewLifeX](https://github.com/NewLifeX) وعلى [newlifex.com](https://newlifex.com).

## التوثيق

يوفّر **NewLife.Redis** لمطوّري .NET وصولًا إلى Redis على امتداد دورة الحياة الكاملة:

- **بروتوكول كامل**: ينفّذ RESP2، ويغطي جميع الأوامر الشائعة من Redis 2.8 إلى 7.x، ويدعم كل هياكل البيانات: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog.
- **طوابير الرسائل**: طابور بسيط مدمج، و**طابور موثوق (RPOPLPUSH + دلالة Ack)**، وطابور مؤجّل، وطابور Stream متعدد مجموعات المستهلكين — جاهزة للاستخدام دون إعادة اختراع العجلة.
- **التجمع والتوفر العالي**: يدعم Standalone / Master-Replica / Sentinel / Native Cluster مع تجاوز فشل تلقائي.
- **محوّلات مزوّدي السحابة**: تهيئة وتكييف مخصّصتان لـ **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**، ويغطيان وضع الوكيل ومصادقة ACL وتشفير SSL وصيغ المصادقة الخاصة وغيرها من الفروق.
- **تنفيذات متوافقة**: كشف تلقائي لـ Microsoft Garnet وkvrocks وغيرهما من الخوادم المتوافقة مع Redis، مع تدهور سلس وبدائل واضحة عند عدم دعم إحدى الميزات.
- **دعم واسع لأطر العمل**: حزمة واحدة تغطي **.NET Framework 4.5 حتى .NET 10+**، وهي مناسبة للتحكّم الصناعي والقطاع الحكومي وإنترنت الأشياء وغيرها من السيناريوهات القديمة.

### الأدلة

- [المتطلبات (中文)](Doc/需求文档.md): الرؤية والأهداف والمتطلبات الوظيفية
- [البنية المعمارية (中文)](Doc/架构文档.md): الطبقات والمكوّنات وقرارات التصميم الرئيسية
- [قائمة الميزات (中文)](Doc/功能清单.md): حالة التنفيذ والاختبار والتوثيق لكل ميزة
- [تحليل المنافسين (中文)](Doc/竞品分析.md): مقارنة مع عملاء Redis السائدين
- [توافق Garnet (中文)](Doc/Garnet兼容性.md): النطاق المدعوم والتدهور السلس
- [توافق AOT (中文)](Doc/AOT兼容性分析.md): نطاق NativeAOT والنتائج المقاسة
- [التوطين (中文)](Doc/多语言本地化.md): نموذج الرسائل للاستثناءات والسجلات
- [تكامل RedisSwitch (中文)](Doc/RedisSwitch项目说明.md): أداة تبديل Redis دون توقف
- [تحليل تخصيص GC (中文)](Doc/dotMemory分析Redis的GC分配.md): قياسات dotMemory
- [الموقع الرسمي](https://newlifex.com/core/redis): صفحة المكوّن والتوثيق عبر الإنترنت

### دعم البروتوكول والإصدارات

#### دعم بروتوكول RESP

| البروتوكول | الحالة | الوصف |
|----------|--------|-------------|
| RESP2 | ✅ مدعوم بالكامل | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ مدعوم بالكامل | أنواع Map / Set / Double / Boolean / Null / Blob Error، يُتفق عليها عبر مصافحة HELLO (Redis 6.0+) |

#### دعم ميزات إصدارات Redis

| إصدار Redis | الميزات الرئيسية | الحالة |
|---------------|--------------|--------|
| 2.8 | الأوامر الأساسية، SCAN، HyperLogLog، Pub/Sub | ✅ مدعوم بالكامل |
| 3.0 | Native Cluster، توجيه التجزئة بـ CRC16 | ✅ مدعوم بالكامل |
| 3.2 | بيانات GEO المكانية | ✅ مدعوم بالكامل |
| 4.0 | الحذف غير المتزامن UNLINK، MODULE | 🔄 دعم جزئي |
| 5.0 | هيكل بيانات Stream (دعم كامل لـ XADD/XREAD/XGROUP/XACK) | ✅ مدعوم بالكامل |
| 6.0 | التحكّم بالوصول ACL، TLS/SSL، LMOVE، RESET | ✅ مدعوم بالكامل |
| 6.2 | COPY، GETDEL، GETEX، GEOSEARCH | ✅ الأوامر الأساسية مدعومة |
| 7.0 | Functions (FCALL)، LMPOP/ZMPOP، Pub/Sub مجزّأ | 🔄 مخطط له |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 مخطط له |

### البنية المعمارية

```
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                            │
│  ASP.NET Core / Console / WinForms / IoT / Worker Service        │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│          Extension Integration (NewLife.Redis.Extensions)        │
│  IDistributedCache · IDataProtection · AddRedisCaching DI        │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                 Advanced Features (FullRedis)                    │
│  RedisList<T> · RedisHash<T> · RedisSet<T> · RedisSortedSet<T>  │
│  RedisStack<T> · RedisGeo · HyperLogLog · RedisRedLock         │
│  RedisQueue<T> · RedisReliableQueue<T> · RedisDelayQueue<T>      │
│  RedisStream<T> · PubSub · RedisEventBus                         │
│  RedisCluster · RedisSentinel · RedisReplication                 │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                  Core Client Layer (Redis)                       │
│  Connection Pool ObjectPool<RedisClient> (Min=10, Max=100,000)   │
│  Multi-Node Failover · Pipeline Mgmt · Encoder · Config Parsing  │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                  Protocol Layer (RedisClient)                    │
│  TCP/TLS Connection · RESP2 Codec · ArrayPool<byte> Buffers      │
└──────────────────────────────┬──────────────────────────────────┘
                               │ TCP/TLS
┌──────────────────────────────▼──────────────────────────────────┐
│  Redis Server / Garnet / kvrocks / Alibaba / Tencent / Huawei   │
└─────────────────────────────────────────────────────────────────┘
```

وصف الأصناف الرئيسية:

| الصنف | الدور |
|-------|------|
| `Redis` | العميل الأساسي: مجمّع الاتصالات، وتجاوز الفشل متعدد العقد، وخط الأنابيب pipeline، وإدارة التهيئة |
| `FullRedis` | عميل معزّز: توجيه التجمع، وبادئة المفاتيح، ودوال إنشاء هياكل البيانات |
| `RedisClient` | وحدة بروتوكول RESP: اتصال TCP، وترميز/فكّ ترميز الأوامر (داخلية، يديرها المجمّع) |
| `RedisList<T>` etc. | أغلفة عامة لهياكل البيانات باستراتيجية ترميز موحّدة |
| `RedisReliableQueue<T>` | طابور رسائل موثوق (RPOPLPUSH + تأكيد Ack) |
| `RedisStream<T>` | طابور Stream (Redis 5.0+، وضع مجموعات المستهلكين) |
| `IPacketEncoder` | استراتيجية ترميز/فكّ ترميز قابلة للتوصيل |

### أمثلة الاستخدام

كل مثال أدناه يعمل على نسخة `FullRedis` حيّة أُنشئت في [بداية سريعة](#بداية-سريعة).

<details>
<summary><b>العمليات الأساسية</b></summary>

```csharp
// Write / Read
rds.Set("k1", 123, expire: 600);
var v = rds.Get<Int32>("k1");

// Set only if key does not exist
var ok = rds.Add("k2", "init");

// Replace and return old value
var old = rds.Replace("k2", "new");

// Counters
rds.Increment("counter", 1);
rds.Decrement("counter", 2);

// Expiry management
rds.SetExpire("k1", TimeSpan.FromMinutes(10));
var ttl = rds.GetExpire("k1");

// Batch read/write
rds.SetAll(new Dictionary<String, Object> { ["a"] = 1, ["b"] = 2 }, expire: 300);
var dict = rds.GetAll<Int32>(["a", "b"]);

// Key prefix (FullRedis.Prefix)
var scoped = rds.GetPrefixed("myapp:");
scoped.Set("user", "Alice");  // Actual key is myapp:user
```

</details>

<details>
<summary><b>هياكل البيانات</b></summary>

#### List (القائمة)

```csharp
var list = rds.GetList<String>("queue:demo");
list.Add("job1");
list.Add("job2");
var first = list[0];
var popped = list.RightPop();
```

#### Hash (التجزئة)

```csharp
var hash = rds.GetDictionary<String>("user:1001");
hash["Name"] = "Alice";
hash["Email"] = "alice@example.com";
var name = hash["Name"];
var all = hash.GetAll();
```

#### Set / Sorted Set (المجموعة / المجموعة المرتّبة)

```csharp
var set = rds.GetSet<String>("tags");
set.Add("redis");
set.Add("cache");
var contains = set.Contains("redis");

var zset = rds.GetSortedSet<String>("leaderboard");
zset.Add("Player1", 100.0);
zset.Add("Player2", 95.5);
var top3 = zset.GetByRank(0, 2);
```

#### Stack (المكدس)

```csharp
var stack = rds.GetStack<String>("undo:ops");
stack.Push("op1");
stack.Push("op2");
var last = stack.Pop();   // "op2"
```

#### Geo (الجغرافيا)

```csharp
var geo = rds.GetGeo("locations");
geo.Add("Beijing",  116.4074, 39.9042);
geo.Add("Shanghai", 121.4737, 31.2304);
var dist = geo.GetDistance("Beijing", "Shanghai", "km");
var nearby = geo.Search("Beijing", 500, "km");
```

#### HyperLogLog

```csharp
var hll = rds.GetHyperLogLog("uv:today");
hll.Add("user1", "user2", "user3");
var count = hll.Count;  // Approximate cardinality
hll.Merge("uv:yesterday");
```

> ملاحظة: نسخة `Redis` الأساسية تدعم عمليات KV فقط. استخدم `FullRedis` للمجموعات المتقدمة.

</details>

<details>
<summary><b>Pipeline والتجميع التلقائي</b></summary>

يقلّل Pipeline بشكل كبير رحلات الشبكة (RTT) ويرفع إنتاجية العمليات المجمّعة:

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**الوضع التلقائي** (موصى به في الإنتاج):

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>طوابير الرسائل</b></summary>

#### طابور بسيط

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### طابور موثوق (دلالة مرة واحدة على الأقل)

```csharp
var queue = rds.GetReliableQueue<OrderMsg>("orders");

// Produce
queue.Add(new OrderMsg { OrderId = "12345" });

// Consume + Acknowledge
var msg = queue.TakeOne(timeout: 30);
if (msg != null)
{
    // Process the message...
    queue.Acknowledge(msg);   // Acknowledge consumption; removed from Ack queue
}
// If the process crashes without acknowledging, the message is automatically rolled back
// to the main queue after 60 seconds
```

#### طابور مؤجّل

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### طابور Stream (Redis 5.0+، متعدد مجموعات المستهلكين)

```csharp
var stream = rds.GetStream<LogEntry>("logs");
stream.Group = "processor-group";

// Produce
stream.Add(new LogEntry { Level = "INFO", Message = "Application started" });

// Consume (multiple instances compete within the same group)
var entry = stream.TakeOne(timeout: 15);
if (entry != null)
{
    // Process...
    stream.Acknowledge(entry.Id);
}
```

</details>

<details>
<summary><b>النشر / الاشتراك</b></summary>

```csharp
var pubsub = rds.GetPubSub("events:user");

// Publish
pubsub.Publish("login:Alice");

// Subscribe (async loop)
var cts = new CancellationTokenSource();
await pubsub.SubscribeAsync((channel, message) =>
{
    Console.WriteLine($"[{channel}] {message}");
}, cts.Token);
```

</details>

<details>
<summary><b>التجمع والتوفر العالي</b></summary>

#### تجاوز الفشل التلقائي متعدد العناوين

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### الكشف التلقائي لوضع التجمع

```csharp
// When AutoDetect=true, automatically detect the working mode via INFO
// ⚠️ For public cloud proxy mode, keep the default false to avoid obtaining internal node addresses
var rds = new FullRedis("sentinel-host:26379", "pass", 0)
{
    AutoDetect = true,    // Auto-detect Cluster / Sentinel / Replication
};
```

</details>

<details>
<summary><b>محوّلات مزوّدي السحابة</b></summary>

#### Alibaba Cloud KVStore / Tair

```csharp
// Standard edition (Redis 2.8/4.0/5.0/6.0/7.0)
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6379", "YourPassword", 0);

// Redis 6.0 ACL user authentication
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6379", "YourUser", "YourPassword", 0);

// SSL encrypted connection
var rds = new FullRedis("xxxxxx.redis.rds.aliyuncs.com:6380", "YourPassword", 0)
{
    SslProtocol = SslProtocols.Tls12,
};

// ⚠️ Public cloud proxy mode: Keep AutoDetect at its default false
// ⚠️ Cluster edition: Proxy handles shard routing; clients don't need to know the internal topology
```

#### Tencent Cloud Redis

```csharp
// Tencent Cloud standard/cluster edition: use instance ID as UserName (AUTH instanceId:password format)
var rds = new FullRedis("xxxxxx.redis.tencentcds.com:6379", "instanceId", "YourPassword", 0);

// Or via connection string
var rds = FullRedis.Create("server=xxxxxx.redis.tencentcds.com:6379;username=instanceId;password=YourPassword;db=0");
```

#### Huawei Cloud DCS

```csharp
// Master/Standby edition (standard AUTH)
var rds = new FullRedis("xxxxxx.dcs.myhuaweicloud.com:6379", "YourPassword", 0);

// SSL edition (use after downloading the PEM certificate)
var rds = new FullRedis("xxxxxx.dcs.myhuaweicloud.com:6380", "YourPassword", 0)
{
    SslProtocol = SslProtocols.Tls12,
    Certificate = new X509Certificate2("huawei-dcs.pem"),
};

// ⚠️ Cluster edition does not support SELECT; Db must be 0
// ⚠️ AutoDetect = false (default) to avoid internal node address issues
```

**ملاحظات عامة حول مزوّدي السحابة:**
- وضع الوكيل في السحابة العامة: أبقِ `AutoDetect = false` (القيمة الافتراضية) لمنع الكشف التلقائي للعقد الداخلية
- حصّة الاتصالات: لكل مزوّد سحابة ولكل حجم نسخة حدود اتصالات؛ اضبط معامل `Max` في المجمّع وفقًا لها
- إبقاء الاتصال حيًا (TCP keepalive): يستخدم Huawei Cloud مهلة خمول TCP افتراضية مدتها 300 ثانية؛ اضبط `Timeout` بأقل من هذه القيمة وأرسل PING دوريًا

</details>

<details>
<summary><b>التسلسل والمرمّزات</b></summary>

المرمّز الافتراضي: `RedisJsonEncoder` (مضيف JSON مدمج)، جاهز للاستخدام مباشرة.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

تُسلسل الأنواع الأولية (`String` / `Int32` / `Int64` / `Double` / `Boolean`) مباشرة دون المرور عبر JSON لأقصى أداء.

</details>

<details>
<summary><b>الأمان والمصادقة</b></summary>

```csharp
// Redis 6.0+ ACL authentication
var rds = new FullRedis("host:6379", "username", "password", 0);

// TLS encryption (specify protocol version)
rds.SslProtocol = SslProtocols.Tls12;

// Certificate verification (mutual mTLS)
rds.Certificate = new X509Certificate2("client.pem", "certPassword");

// Connection string password encryption (ProtectedKey mechanism)
// Configure ProtectedKey.Instance.Secret; passwords in connection strings are auto-decrypted
```

</details>

<details>
<summary><b>الرصد</b></summary>

```csharp
// Structured logging
rds.Log       = XTrace.Log;      // Component-level logs (connection changes, cluster changes, retries)
rds.ClientLog = XTrace.Log;      // Protocol-level logs (every command; enable only when debugging)

// Performance counters
rds.Counter = new PerfCounter();
var stats = rds.Counter.GetAll();   // Get ops/s, latency, error rate

// APM distributed tracing (compatible with NewLife.Stardust / OpenTelemetry)
rds.Tracer = DefaultTracer.Instance;
// All Execute / queue produce-consume operations are automatically instrumented, with key as tag

// OpenTelemetry native tracing (net8.0+): subscribe to the built-in ActivitySource
// builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource(Redis.ActivitySourceName));
```

</details>

<details>
<summary><b>حزم الامتدادات</b></summary>

يوفّر `NewLife.Redis.Extensions` تكاملًا عميقًا مع ASP.NET Core:

```bash
dotnet add package NewLife.Redis.Extensions
```

```csharp
// Program.cs
builder.Services.AddRedisCaching(options =>
{
    options.Server   = "127.0.0.1:6379";
    options.Password = "pass";
    options.Db       = 0;
    options.Prefix   = "myapp:";
});

// Automatically registers IDistributedCache + FullRedis as singletons

// Persist data protection keys to Redis
builder.Services.AddDataProtection()
    .PersistKeysToRedis(redis, "DataProtection-Keys");
```

</details>

<details>
<summary><b>توافق Garnet</b></summary>

يدعم NewLife.Redis بالكامل خادم [Garnet](https://github.com/microsoft/garnet) عالي الأداء والمتوافق مع Redis من Microsoft، مع كشف تلقائي وتدهور سلس.

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| الميزة | حالة الدعم في Garnet |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ مدعومة بالكامل |
| HyperLogLog / Geo | ✅ مدعومة بالكامل |
| Pub/Sub / Transactions / Lua Scripts | ✅ مدعومة بالكامل |
| Pipeline | ✅ مدعوم بالكامل |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ مدعومة بالكامل |
| `RedisReliableQueue<T>` | ✅ مدعوم بالكامل |
| `RedisStream<T>` | ❌ لا يدعم Garnet بنية Stream بعد؛ يُطرح `NotSupportedException` تلقائيًا مع اقتراح بدائل |

انظر: [وثائق توافق Garnet (中文)](Doc/Garnet兼容性.md)

</details>

### المرجع

<details>
<summary><b>حالة اكتمال الميزات</b></summary>

> المفتاح: ✅ مكتمل | 🔄 مخطط له | ❌ غير مخطط له صراحةً

#### هياكل البيانات

| البنية | الحالة | الوصف |
|-----------|--------|-------------|
| String KV + الدفعات | ✅ | MGET/MSET/INCR/APPEND/GETRANGE، وغيرها. |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE، وغيرها. |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN، وغيرها. |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF، وغيرها. |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN، وغيرها. |
| Stream | ✅ | دعم كامل لـ XADD/XREAD/XGROUP/XACK |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH، وغيرها. |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

#### طوابير الرسائل

| نوع الطابور | الحالة | دلالة التسليم |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | مرة واحدة على الأكثر |
| `RedisReliableQueue<T>` | ✅ | مرة واحدة على الأقل (RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | تسليم مؤجّل (Sorted Set) |
| `RedisStream<T>` | ✅ | مرتّب + مجموعة مستهلكين + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | بثّ متعدد مجموعات المستهلكين |
| `PubSub` + `RedisEventBus` | ✅ | دفع في الزمن الحقيقي |
| قفل RedLock الموزّع | 🔄 | خوارزمية Redlock متعددة النسخ |

#### دعم التجمعات

| الوضع | الحالة |
|------|--------|
| Standalone | ✅ |
| نسخ Master-Replica | ✅ |
| Sentinel | ✅ |
| Native Cluster | ✅ |
| التكيّف التلقائي مع kvrocks | ✅ |
| الكشف التلقائي لـ Garnet | ✅ |
| الكشف التلقائي لـ Valkey | ✅ |

#### البروتوكول والميزات المتقدمة

| الميزة | الحالة |
|---------|--------|
| دعم RESP2 الكامل | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| مصادقة ACL | ✅ |
| المعاملات MULTI/EXEC/WATCH | ✅ |
| سكربتات Lua EVAL/EVALSHA | ✅ |
| دوال Redis | 🔄 مخطط لها |
| Pipeline (صريح + تلقائي) | ✅ |
| الاشتراك بالنمط PSUBSCRIBE | 🔄 |
| الحذف غير المتزامن UNLINK | 🔄 |

</details>

<details>
<summary><b>قياسات الأداء</b></summary>

يوجد قياس أداء مدمج في الشيفرة (`Samples/Benchmark`)، ونتائج نموذجية (40 معالجًا منطقيًا، Redis 7.0 محلي):

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

تشغيل القياس يدويًا:

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> يتأثر الأداء الفعلي بزمن رحلة الشبكة (RTT) وحجم القيمة وتعقيد التسلسل. يُنصح بإبقاء حجم القيمة الواحدة بين 1 و1.4 كيلوبايت.

</details>

<details>
<summary><b>أفضل الممارسات</b></summary>

- **إعادة استخدام نسخة وحيدة**: يمتلك `FullRedis` مجمّع اتصالات داخليًا؛ يُوصى بشدة باستخدام نسخة واحدة طوال عمر التطبيق — لا تنشئ نسخة جديدة في كل مرة
- **تصميم المفاتيح**: استخدم `:` فاصلًا هرميًا، مثل `user:1001:profile`؛ واستخدم `Prefix` لعزل النطاقات في الإنتاج
- **الدفعات أولًا**: فضّل `GetAll/SetAll` على تكرار عمليات المفتاح الواحد في حلقة؛ واستخدم `AutoPipeline` لدمج عمليات الكتابة تلقائيًا
- **حجم القيمة**: أبقِ القيم بين 1 و2 كيلوبايت؛ وقسّم الكائنات الكبيرة أو اضغطها أو استخدم Hash للتخزين على مستوى الحقل
- **استهلاك موثوق**: استخدم `RedisReliableQueue<T>` في الإنتاج بدلًا من List بسيطة لتجنّب فقدان الرسائل
- **مزوّدو السحابة**: أبقِ `AutoDetect = false` على السحابات العامة لتجنّب مشكلات عناوين العقد الداخلية
- **المراقبة**: فعّل إحصاءات `Counter` في الإنتاج؛ ولا تفعّل `ClientLog` إلا عند التشخيص لتجنّب تكلفة التسجيل في المسار الحار
- **SSL**: فعّل نقل TLS المشفّر على السحابات العامة وفي بيئات الإنتاج

</details>

<details>
<summary><b>الأسئلة الشائعة</b></summary>

**س: هل يدعم تجزئة Redis Cluster؟**
ج: نعم. يكتشف `FullRedis` وضع Cluster تلقائيًا عند `AutoDetect=true`. توجيه CRC16 وإعادة توجيه MOVED/ASK كلها منفّذة. أما في وضع الوكيل بالسحابة العامة فأبقِ `AutoDetect=false` الافتراضي.

**س: كيف يقارن بـ StackExchange.Redis؟**
ج: يستخدم NewLife.Redis نموذج مجمّع الاتصالات (مقابل التعدّد Multiplexing في StackExchange) لسلوك اتصال أكثر وضوحًا. وتُعدّ أغلفة طوابير الرسائل المدمجة (الطابور الموثوق / الطابور المؤجّل) فارقًا رئيسيًا. كما يدعم .NET Framework 4.5. انظر [تحليل المنافسين (中文)](Doc/竞品分析.md).

**س: كيف أتعامل مع الاستعلامات البطيئة الناتجة عن القيم الكبيرة؟**
ج: قسّم هياكل البيانات (مثل Hash لكل حقل) + قراءة/كتابة على دفعات + مرمّز ثنائي + اضبط `MaxMessageSize` (الحد الافتراضي 1 ميجابايت).

**س: ماذا لو نفد مجمّع الاتصالات؟**
ج: الحد الأقصى الافتراضي 100,000 اتصال؛ ولن تستنفده أحمال العمل الطبيعية. والمجمّع قابل للتهيئة عبر `Redis.PoolConfig` (`RedisPoolConfig`): الحد الأدنى للخامل / السعة القصوى / زمن تنظيف الخامل / أقصى عمر / مهلة انتظار الاستعارة. وتُفحص الاتصالات التي تتجاوز `IdleTime` بـ PING استباقي عند استعارتها؛ وفي سيناريوهات تبديل الرئيسي/التابع أو التحديث التدريجي للوكيل، اخفض `MaxLifetime` لإعادة تدوير الاتصالات أسرع. وإذا ظهرت مهلات انتظار فتحقّق من تسرّب اتصالات (نسيان `Dispose`) أو من عمليات تحتفظ بالاتصال لفترات طويلة جدًا.

**س: لماذا تفشل الاتصالات على Alibaba Cloud / Tencent Cloud؟**
ج: تحقّق من: ① هل `AutoDetect` مضبوط على `false` (الافتراضي الصحيح)؟ ② هل صيغة المصادقة صحيحة (Tencent Cloud تتطلب `username=instanceId`)؟ ③ هل مجموعة أمان الشبكة / القائمة البيضاء تسمح بعنوان IP الخاص بالعميل؟ ④ هل إصدار التجمع يمنع استخدام SELECT لتبديل قواعد البيانات؟

**س: هل يدعم Garnet كل الميزات؟**
ج: العمليات الأساسية وList/Hash/Set/ZSet/HLL/Geo والطوابير كلها مدعومة. أما Stream فغير مدعوم بعد، وسيُطرح `NotSupportedException` تلقائيًا مع اقتراح استخدام `RedisQueue<T>` كبديل.

</details>

<details>
<summary><b>خارطة الطريق</b></summary>

- [x] ~~دعم بروتوكول RESP3~~ (مكتمل)
- [ ] دوال Redis (FCALL/FUNCTION، بديلًا عن سكربتات Lua)
- [ ] LMPOP / ZMPOP (سحب دفعي في Redis 7.0)
- [ ] قفل RedLock الموزّع متعدد النسخ
- [ ] عمليات Bitmap / Bitfield
- [ ] تنفيذ كامل للاشتراك بالنمط PSUBSCRIBE
- [ ] الحذف غير المتزامن UNLINK
- [ ] محوّل تصدير مقاييس Prometheus
- [x] امتداد Alibaba Cloud Tair (دعم TairString / TairHash / TairZSet)
- [ ] محوّل ربط عناوين العقد لإصدار التجمع في Huawei Cloud DCS
- [x] ~~النشر/الاشتراك~~ (مكتمل)
- [x] ~~RedisStream متعدد مجموعات المستهلكين~~ (مكتمل)
- [x] ~~كشف نوع خادم Garnet~~ (مكتمل)

نرحّب بالملاحظات وطلبات الميزات عبر Issue / PR.

</details>

<details>
<summary><b>المقارنة التنافسية</b></summary>

| القدرة | NewLife.Redis | StackExchange.Redis | FreeRedis | ملاحظات |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | حاسم للأنظمة الصناعية والحكومية القديمة |
| نموذج الاتصال | مجمّع اتصالات | تعدّد Multiplexing | مجمّع اتصالات | لكلٍّ حالاته المناسبة |
| تغطية الأوامر | ~98% | ~95% | ~98% | جميع الأوامر الموثّقة باستثناء ما لم يُخطَّط له صراحةً |
| أغلفة طوابير رسائل مدمجة | ✅ كاملة | ❌ | 🔄 جزئية | طوابير موثوقة/مؤجّلة فريدة |
| تسلسل مدمج | ✅ | ❌ يدوي | ❌ يدوي | يقلّل الشيفرة المتكرّرة |
| محوّلات مخصّصة لمزوّدي السحابة | ✅ (Alibaba/Tencent/Huawei) | عامة | عامة | توثيق مخصّص |
| محوّل صريح لـ Garnet | ✅ | ❌ | ❌ | كشف النوع + تدهور سلس |
| محوّل kvrocks | ✅ | ❌ | ❌ | كشف تلقائي |
| الكشف التلقائي لـ Valkey | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| تتبّع APM | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | تكامل أصلي مع OpenTelemetry |
| RESP3 | ✅ | ✅ | ✅ | تم التحقق عبر مجموعة اختبار RESP3 |

انظر [وثيقة تحليل المنافسين (中文)](Doc/竞品分析.md) للتفاصيل.

</details>

### ملاحظات الإصدار

- [ChangeLog.md](ChangeLog.md): سجل الإصدارات (بالصينية)
- [ChangeLog.en.md](ChangeLog.en.md): سجل الإصدارات (بالإنجليزية)

## المساهمة

### البناء والاختبار

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- تحتاج اختبارات التكامل إلى Redis حيّ: اضبط `REDIS_CONNECTION` أو شغّل نسخة على `127.0.0.1:6379`؛ وإلا فسيتم تخطّي الاختبارات الموسومة بـ `[RedisFact]` / `[RedisTopologyFact]` تلقائيًا.
- فحص NativeAOT: `AotSmoke/`، وCI في `.github/workflows/aot.yml`.
- قياسات الأداء: `Samples/Benchmark`.

### بنية المستودع

| الدليل | الوصف |
| --- | --- |
| `NewLife.Redis/` | مصادر العميل: مجمّع الاتصالات، ومرمّز RESP، وهياكل البيانات والطوابير |
| `NewLife.Redis.Extensions/` | تكامل ASP.NET Core: `IDistributedCache` وDI وحماية البيانات |
| `NewLife.Extensions.Caching.Redis/` | حزمة توافق قديمة لامتداد التخزين المؤقت |
| `XUnitTest/` | مجموعة اختبارات xUnit (net10.0) |
| `AotSmoke/` | مشروع فحص NativeAOT |
| `Samples/` | نماذج Benchmark وWebDemo وQueueDemo وRedisSwitch وفحص الأجهزة المحمولة |
| `Doc/` | المتطلبات والبنية المعمارية وقائمة الميزات والوثائق الموضوعية |

### الإصدار

ادفع وسمًا يبدأ بـ `v` (مثل `v7.0.2026.1024`) لتشغيل `.github/workflows/publish.yml`. تحزم سير العمل `NewLife.Redis` و`NewLife.Redis.Extensions` وتنشرهما على NuGet.org وGitHub Packages، وتُنشئ GitHub Release بمحتوى القسم المطابق من `ChangeLog.md`؛ أما التشغيل اليدوي فينشر الحزم فقط.

### الملاحظات

1. قبل الإرسال، تأكد من نجاح `dotnet build` والاختبارات ذات الصلة، واتبع أسلوب شيفرة المستودع واتفاقيات الإيداع
2. طلبات السحب: أبقِ التغييرات في أدنى حد، وأضف التعليقات الضرورية وأوصاف الاختبارات
3. المشكلات: قدّم الإصدار وبيئة التشغيل وسيناريو إعادة إنتاج مصغّرًا

المجتمع: مجموعات QQ ‏1600800 / 1600838؛ وGitHub Discussions / Issues.

## الفريق

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

تأسّست NewLife في عام 2002، وأمضت أكثر من عقدين في منظومة .NET وإنترنت الأشياء، وتدير ما يقارب 100 مشروع مفتوح المصدر (34 منها مشاريع رئيسية)، وتقدّم حلولًا متكاملة للبرمجيات والأجهزة لقطاعات الكهرباء والخدمات اللوجستية والرعاية الصحية والتحكّم الصناعي والإنترنت. ونلتزم بمبدأ "لا أسبقية في التعلّم — الإتقان يكسب الاحترام".

نُزّلت المكوّنات مفتوحة المصدر <!--nuget-total:start-->6.48M<!--nuget-total:end--> مرة من NuGet (على مستوى المنظومة بأكملها، حتى 2026-10).

الموقع: https://newlifex.com · المنظمة مفتوحة المصدر: https://github.com/NewLifeX

<details>
<summary>حساب WeChat الرسمي: 智能大石头 (امسح للاشتراك)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## الترخيص

رخصة MIT. الاستخدام التجاري / التعديل / إعادة التوزيع مجاني (دون إذن إضافي). ما عليك سوى الإبقاء على إشعار حقوق النشر.

> إذا لم تغطِّ هذه الوثيقة حالتك، فلا تتردد في إرسال Issue — لنُحسّن التوثيق معًا!
