# NewLife.Redis — Высокопроизводительный клиент Redis

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` — это **высокопроизводительный, высокопропускной и легко интегрируемый** клиент Redis для .NET, разработанный командой NewLife. С 2017 года он стабильно работает на множестве производственных платформ с миллиардами записей и высокой конкурентностью, обрабатывая более **80 миллиардов** вызовов команд ежедневно.

> 📖 Полная документация: [Требования (中文)](Doc/需求文档.md) | [Архитектура (中文)](Doc/架构文档.md) | [Список функций (中文)](Doc/功能清单.md) | [Конкурентный анализ (中文)](Doc/竞品分析.md) | [Совместимость с Garnet (中文)](Doc/Garnet兼容性.md) | [Совместимость с AOT (中文)](Doc/AOT兼容性分析.md)

**Содержание**: [Быстрый старт](#быстрый-старт) · [Основные возможности](#основные-возможности) · [Карта экосистемы](#карта-экосистемы) · [Документация](#документация) · [Участие в разработке](#участие-в-разработке) · [Команда](#команда) · [Лицензия](#лицензия)

## Быстрый старт

### Установка

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### Базовое использование

**Рекомендуется: singleton, потокобезопасный, переиспользуйте в течение всего времени жизни приложения:**

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

**Режим строки подключения (рекомендуется для центров конфигурации):**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### Поддержка платформ

| Элемент | Покрытие |
| --- | --- |
| Целевые платформы | Один пакет: net45, net461, netstandard2.0, netstandard2.1, net8.0; расширения добавляют netcoreapp3.1 → net10 |
| ОС и архитектуры | Windows, Linux, macOS, Android, iOS; x64, arm64, loongarch64, riscv64, s390x |
| NativeAOT | `net8.0` поставляется с проверками `IsAotCompatible`; `AotSmoke/` вместе с матрицей CI smoke покрывают win-x64 / linux-x64 / linux-arm64 / linux-la64 (LA64 через QEMU), а RID win-arm64, macOS и musl компилируются нативно. linux-x64 — блокирующий шлюз, остальные находятся под наблюдением — см. [Совместимость с AOT](Doc/AOT兼容性分析.md) |

## Основные возможности

<details>
<summary><b>Ядро движка</b> (7)</summary>

| Возможность | Ключевые характеристики |
| --- | --- |
| Проверено на больших масштабах | 200+ экземпляров Redis в производстве, суточный пик — более 100M записей бизнес-объектов и 80B+ вызовов команд. |
| Низкая задержка | Один цикл Get/Set занимает 200–600 мкс с учётом сети. |
| Высокая пропускная способность | Встроенный пул соединений (до 100 000 одновременных) с синхронным парсером RESP и опциональным автоматическим пакетированием (auto-pipeline). |
| Автоповтор и переключение при нескольких адресах | Список узлов через запятую, быстрое переключение при сетевых ошибках и 10-секундное окно экранирования по умолчанию. |
| Подключаемый кодировщик | По умолчанию JSON (`RedisJsonEncoder`), расширяется до бинарной сериализации через `IPacketEncoder`. |
| Трассировка APM | Распределённая трассировка `ITracer` плюс счётчики производительности `PerfCounter`. |
| Без тяжёлых зависимостей | Только экосистема NewLife (логирование, конфигурация, сериализация, безопасность) поверх BCL. |

</details>

<details>
<summary><b>Структуры данных</b> (10)</summary>

| Структура | Ключевые характеристики |
| --- | --- |
| String KV + пакетные операции | MGET / MSET / INCR / APPEND / GETRANGE и другие. |
| List | LPUSH / RPOP / LRANGE / LMOVE. |
| Hash | HGET / HSET / HMGET / HSCAN. |
| Set | SADD / SMEMBERS / SUNION / SDIFF. |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN. |
| Stack | Обёртка LIFO поверх List. |
| Stream | Полный XADD / XREAD / XGROUP / XACK. |
| Geo | GEOADD / GEODIST / GEOSEARCH. |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE. |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD (в планах). |

</details>

<details>
<summary><b>Очереди сообщений и Pub/Sub</b> (7)</summary>

| Тип | Ключевые характеристики |
| --- | --- |
| `RedisQueue<T>` | Простая очередь с доставкой не более одного раза (at-most-once). |
| `RedisReliableQueue<T>` | Доставка не менее одного раза (at-least-once) с RPOPLPUSH + откатом по Ack. |
| `RedisDelayQueue<T>` | Отложенная доставка на основе Sorted Set. |
| `RedisStream<T>` | Упорядоченный stream с потребительскими группами и XACK. |
| `MultipleConsumerGroupsQueue<T>` | Широковещательная рассылка в несколько потребительских групп. |
| `PubSub` + `RedisEventBus` | Публикация и подписка в реальном времени. |
| Распределённая блокировка RedLock | Многоэкземплярный алгоритм Redlock (в работе). |

</details>

<details>
<summary><b>Кластеризация и высокая доступность</b> (7)</summary>

| Режим | Ключевые характеристики |
| --- | --- |
| Standalone | Прямое подключение к одному экземпляру с минимальными накладными расходами. |
| Репликация Master-Replica | Разделение чтения и записи, переключение при отказе. |
| Sentinel | Обнаружение через Sentinel с автоматическим переключением мастера. |
| Native Cluster | Маршрутизация слотов CRC16 с перенаправлениями MOVED / ASK. |
| kvrocks | Автоматически адаптируется к движку хранения kvrocks. |
| Garnet | Автоматически определяет Microsoft Garnet и корректно деградирует при отсутствии возможности. |
| Valkey | Определяется по INFO сервера. |

</details>

<details>
<summary><b>Совместимость протокола и серверов</b> (7)</summary>

| Возможность | Ключевые характеристики |
| --- | --- |
| RESP2 | Полностью реализованы simple string, error, integer, bulk string и array. |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error, согласуется через рукопожатие HELLO (Redis 6.0+). |
| Redis 2.8 – 7.x | Основные команды каждой версии, включая COPY / GETDEL / GETEX / GEOSEARCH из 6.2. |
| Аутентификация ACL | Аутентификация по имени пользователя и паролю в Redis 6.0+. |
| Транзакции | MULTI / EXEC / WATCH. |
| Lua-скрипты | EVAL / EVALSHA. |
| Redis Functions | FCALL / FUNCTION (в планах). |

</details>

<details>
<summary><b>Облака, безопасность и наблюдаемость</b> (8)</summary>

| Возможность | Ключевые характеристики |
| --- | --- |
| Alibaba Cloud KVStore / Tair | Рекомендации по стандартной версии, ACL, SSL и режиму прокси, а также структуры Tair (TairString / TairHash / TairZSet). |
| Tencent Cloud Redis | Идентификатор экземпляра используется как имя пользователя (`instanceId:password`). |
| Huawei Cloud DCS | Различия версий master/standby, SSL и кластерной версии (в кластерном режиме нет SELECT). |
| TLS/SSL | Настраиваемая версия протокола и взаимный TLS с клиентскими сертификатами. |
| Структурированное логирование | Лог компонента (`Log`) и лог уровня протокола (`ClientLog`). |
| Счётчики производительности | `PerfCounter` сообщает ops/s, задержку и частоту ошибок. |
| Трассировка APM | Спаны `ITracer` повсюду плюс `ActivitySource` (net8.0+) для OpenTelemetry. |
| Защита строк подключения | `ProtectedKey` расшифровывает пароли, встроенные в строки подключения. |

</details>

<details>
<summary><b>Платформа и интеграция</b> (5)</summary>

| Возможность | Ключевые характеристики |
| --- | --- |
| Мультицелевая сборка | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0; пакеты расширений покрывают netcoreapp3.1 → net10. |
| Строгое имя | Подписано `newlife.snk`, поэтому сборки можно пересобирать и заменять. |
| NativeAOT | Статические проверки `IsAotCompatible`, проект `AotSmoke/` и мульти-RID матрица CI. |
| Пакет ASP.NET Core | `NewLife.Redis.Extensions` регистрирует `IDistributedCache` и `FullRedis` через `AddRedisCaching`. |
| DataProtection | `PersistKeysToRedis` сохраняет ключи защиты данных в Redis. |

</details>

## Карта экосистемы

NewLife — это многоуровневая экосистема независимо версионируемых open-source проектов. Приведённая ниже карта дословно используется во всех репозиториях NewLife; каждый проект содержит ссылку на свой репозиторий и основной пакет NuGet.

| Проект | Что делает | Загрузки |
| --- | --- | --- |
| **Базовый** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Логирование, конфигурация, кэширование, сеть, сериализация, планирование и APM-трассировка. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Промежуточное ПО данных: отдельные таблицы масштаба десятков миллиардов строк, автоматическое секционирование, разделение чтения и записи, более 10 баз данных. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Высокопроизводительная сетевая библиотека: 1,77M pps / 14,5 Гбит/с на машину (TCP 1KB, 2026-10), 4M долгоживущих TCP-соединений. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Протокольная связь и RPC-фреймворк для клиент-серверных приложений и IoT-устройств. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Устанавливает .NET-приложения как демоны ОС: службы Windows и systemd в Linux. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Шаблоны проектов `dotnet new` для консоли, web, API, службы, данных и IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Данные и обмен сообщениями** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Клиент Redis с микросекундной задержкой, миллионами операций в секунду и встроенными очередями сообщений. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Полностью управляемый клиент RocketMQ для Apache RocketMQ и Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Клиент и сервер MQTT, включая подключение к Alibaba Cloud IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Гибридная база данных на C#: встроенная или сетевая, реляционная, временных рядов, очередь сообщений и KV в одном. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Драйвер MySql и вспомогательный слой на основе стека данных NewLife. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI и распределённые системы** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Фреймворк быстрой разработки админ-платформ: права, SSO, сервер OAuth и CRUD-скаффолдинг. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Распределённая сервисная платформа: управление узлами, APM, центр конфигурации, реестр и центр публикации. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Распределённая платформа задач и планирования данных в реальном времени с секционированием, повторами и дозаполнением. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Единый клиент для 46 поставщиков LLM плюс ChatAI — готовое веб-приложение для чата. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Промышленность и IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Стандартная библиотека IoT, определяющая контракты протоколов и абстракции устройств. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Реализация Modbus TCP/RTU/ASCII поверх NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Протокол Siemens PLC поверх NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Протокол Omron PLC поверх NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Протокол Mitsubishi Melsec PLC поверх NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Клиент OPC для доступа к промышленным данным. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Стек протокола автоматизации зданий BACnet. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Лёгкая open-source платформа IoT с управлением шлюзами и устройствами. | — |
| **Телематика транспорта** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | Протокол позиционирования транспорта JT/T 808: более 70 типов сообщений, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | Межплатформенный протокол JT/T 809 для органов управления транспортом. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | Автомобильное видео JT/T 1078: разбор RTP, ремультиплексирование в FLV/fMP4 и прямая трансляция. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Прикладные компоненты** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Один пакет для Excel, Word, PPT, PDF и более 15 форматов документов. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Кроссплатформенное аудио: кодеки, ввод-вывод устройств, DSP, контейнеры и потоковая передача. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Видеокодеки, форматы контейнеров, обработка кадров и потоковая передача. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Единый интерфейс к Baidu, AMap, Tencent и Tianditu с геокодированием и преобразованием координат. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Геолокация IPv4 с офлайн-базой, отображаемой в память, и онлайн-API как резервом. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Инструменты и продукты** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Инструменты разработчика: сеть, последовательный порт, шифрование, регулярные выражения, Modbus и MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Самостоятельно размещаемая система управления исходным кодом Git. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Автоматически устанавливает подходящую среду выполнения .NET при запуске на старом хосте. | — |

> Бейджи показывают основной пакет каждого проекта; пакеты тем, расширений и сателлитов перечислены на соответствующих страницах NuGet. Более 30 пакетов, не перечисленных здесь (FTP, LoRa, расширения Office, драйверы протоколов и другие), доступны в [организации NewLifeX](https://github.com/NewLifeX) и на [newlifex.com](https://newlifex.com).

## Документация

**NewLife.Redis** предоставляет .NET-разработчикам доступ к Redis на протяжении всего жизненного цикла:

- **Полный протокол**: реализует RESP2, покрывает все основные команды от Redis 2.8 до 7.x, поддерживает все структуры данных: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog.
- **Очереди сообщений**: встроенные простая очередь, **надёжная очередь (RPOPLPUSH + семантика Ack)**, очередь задержки и Stream-очередь с несколькими потребительскими группами — готовы к использованию, не нужно изобретать заново.
- **Кластеризация и HA**: поддерживает Standalone / Master-Replica / Sentinel / Native Cluster с автоматическим переключением при отказе.
- **Адаптеры облачных провайдеров**: специализированная конфигурация и адаптация для **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**, включая режим прокси, аутентификацию ACL, шифрование SSL, особые форматы аутентификации и другие различия.
- **Совместимые реализации**: автоматически определяет Microsoft Garnet, kvrocks и другие Redis-совместимые серверы, корректно деградирует с понятными альтернативами, когда возможности не поддерживаются.
- **Широкая поддержка фреймворков**: один пакет покрывает **.NET Framework 4.5 – .NET 10+**, подходит для промышленной автоматизации, государственных систем, IoT и других унаследованных сценариев.

### Руководства

- [Требования (中文)](Doc/需求文档.md): видение, цели и функциональные требования
- [Архитектура (中文)](Doc/架构文档.md): слои, компоненты и ключевые проектные решения
- [Список функций (中文)](Doc/功能清单.md): состояние реализации, тестов и комментариев каждой функции
- [Конкурентный анализ (中文)](Doc/竞品分析.md): сравнение с основными клиентами Redis
- [Совместимость с Garnet (中文)](Doc/Garnet兼容性.md): поддерживаемый объём и корректная деградация
- [Совместимость с AOT (中文)](Doc/AOT兼容性分析.md): объём NativeAOT и измеренные выводы
- [Локализация (中文)](Doc/多语言本地化.md): модель сообщений для исключений и логов
- [Интеграция с RedisSwitch (中文)](Doc/RedisSwitch项目说明.md): инструмент переключения Redis без простоя
- [Анализ GC-аллокаций (中文)](Doc/dotMemory分析Redis的GC分配.md): измерения dotMemory
- [Официальный сайт](https://newlifex.com/core/redis): страница компонента и онлайн-документация

### Поддержка протоколов и версий

#### Поддержка протокола RESP

| Протокол | Статус | Описание |
|----------|--------|-------------|
| RESP2 | ✅ Полностью поддерживается | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ Полностью поддерживается | Типы Map / Set / Double / Boolean / Null / Blob Error, согласуются через рукопожатие HELLO (Redis 6.0+) |

#### Поддержка возможностей версий Redis

| Версия Redis | Ключевые возможности | Статус |
|---------------|--------------|--------|
| 2.8 | Базовые команды, SCAN, HyperLogLog, Pub/Sub | ✅ Полностью поддерживается |
| 3.0 | Native Cluster, маршрутизация шардов CRC16 | ✅ Полностью поддерживается |
| 3.2 | Пространственные данные GEO | ✅ Полностью поддерживается |
| 4.0 | Асинхронное удаление UNLINK, MODULE | 🔄 Частичная поддержка |
| 5.0 | Структура данных Stream (полный XADD/XREAD/XGROUP/XACK) | ✅ Полностью поддерживается |
| 6.0 | Управление доступом ACL, TLS/SSL, LMOVE, RESET | ✅ Полностью поддерживается |
| 6.2 | COPY, GETDEL, GETEX, GEOSEARCH | ✅ Поддерживаются основные команды |
| 7.0 | Functions (FCALL), LMPOP/ZMPOP, шардированный Pub/Sub | 🔄 В планах |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 В планах |

### Архитектура

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

Описание ключевых классов:

| Класс | Роль |
|-------|------|
| `Redis` | Базовый клиент: пул соединений, переключение при отказе между узлами, конвейер, управление конфигурацией |
| `FullRedis` | Расширенный клиент: маршрутизация кластера, префикс ключей, фабричные методы структур данных |
| `RedisClient` | Единица протокола RESP: TCP-соединение, кодирование и декодирование команд (internal, управляется пулом) |
| `RedisList<T>` и другие | Обёртки дженерик-структур данных с единой стратегией кодирования |
| `RedisReliableQueue<T>` | Надёжная очередь сообщений (RPOPLPUSH + подтверждение Ack) |
| `RedisStream<T>` | Stream-очередь (Redis 5.0+, режим потребительских групп) |
| `IPacketEncoder` | Подключаемая стратегия кодирования и декодирования |

### Примеры использования

Каждый из приведённых ниже примеров работает с живым экземпляром `FullRedis`, созданным в разделе [Быстрый старт](#быстрый-старт).

<details>
<summary><b>Базовые операции</b></summary>

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
<summary><b>Структуры данных</b></summary>

#### List

```csharp
var list = rds.GetList<String>("queue:demo");
list.Add("job1");
list.Add("job2");
var first = list[0];
var popped = list.RightPop();
```

#### Hash

```csharp
var hash = rds.GetDictionary<String>("user:1001");
hash["Name"] = "Alice";
hash["Email"] = "alice@example.com";
var name = hash["Name"];
var all = hash.GetAll();
```

#### Set / Sorted Set

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

#### Stack

```csharp
var stack = rds.GetStack<String>("undo:ops");
stack.Push("op1");
stack.Push("op2");
var last = stack.Pop();   // "op2"
```

#### Geo

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

> Примечание: базовый экземпляр `Redis` поддерживает только KV-операции. Для продвинутых коллекций используйте `FullRedis`.

</details>

<details>
<summary><b>Конвейер и автоматическое пакетирование</b></summary>

Конвейер (pipeline) значительно сокращает количество сетевых круговых задержек (RTT) и повышает пропускную способность массовых операций:

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**Автоматический режим** (рекомендуется для производственной среды):

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>Очереди сообщений</b></summary>

#### Простая очередь

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### Надёжная очередь (семантика at-least-once)

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

#### Очередь задержки

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### Stream-очередь (Redis 5.0+, несколько потребительских групп)

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
<summary><b>Публикация и подписка</b></summary>

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
<summary><b>Кластеризация и высокая доступность</b></summary>

#### Автопереключение при нескольких адресах

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### Автоопределение режима кластера

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
<summary><b>Адаптеры облачных провайдеров</b></summary>

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

**Общие замечания по облачным провайдерам:**
- Режим прокси публичного облака: оставляйте `AutoDetect = false` (по умолчанию), чтобы предотвратить автоопределение внутренних узлов
- Квота соединений: у каждого облачного провайдера и размера экземпляра есть ограничения на число соединений; задавайте параметр пула `Max` соответственно
- TCP keepalive: в Huawei Cloud по умолчанию действует 300-секундный тайм-аут простоя TCP; задавайте `Timeout` меньше этого значения и периодически отправляйте PING

</details>

<details>
<summary><b>Сериализация и кодировщики</b></summary>

Кодировщик по умолчанию: `RedisJsonEncoder` (встроенный JSON-хост), готов к использованию из коробки.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

Примитивные типы (`String` / `Int32` / `Int64` / `Double` / `Boolean`) сериализуются напрямую, минуя JSON, для максимальной производительности.

</details>

<details>
<summary><b>Безопасность и аутентификация</b></summary>

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
<summary><b>Наблюдаемость</b></summary>

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
<summary><b>Пакеты расширений</b></summary>

`NewLife.Redis.Extensions` обеспечивает глубокую интеграцию с ASP.NET Core:

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
<summary><b>Совместимость с Garnet</b></summary>

NewLife.Redis полностью поддерживает высокопроизводительный Redis-совместимый сервер Microsoft [Garnet](https://github.com/microsoft/garnet) с автоопределением и корректной деградацией.

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| Возможность | Статус поддержки в Garnet |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ Полностью поддерживается |
| HyperLogLog / Geo | ✅ Полностью поддерживается |
| Pub/Sub / транзакции / Lua-скрипты | ✅ Полностью поддерживается |
| Конвейер (pipeline) | ✅ Полностью поддерживается |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ Полностью поддерживается |
| `RedisReliableQueue<T>` | ✅ Полностью поддерживается |
| `RedisStream<T>` | ❌ Garnet пока не поддерживает Stream; автоматически выбрасывается `NotSupportedException` с предложением альтернатив |

См.: [Документация по совместимости с Garnet (中文)](Doc/Garnet兼容性.md)

</details>

### Справочник

<details>
<summary><b>Статус готовности функций</b></summary>

> Обозначения: ✅ Готово | 🔄 В планах | ❌ Явно не планируется

#### Структуры данных

| Структура | Статус | Описание |
|-----------|--------|-------------|
| String KV + пакетные операции | ✅ | MGET/MSET/INCR/APPEND/GETRANGE и другие |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE и другие |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN и другие |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF и другие |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN и другие |
| Stream | ✅ | Полный XADD/XREAD/XGROUP/XACK |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH и другие |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

#### Очереди сообщений

| Тип очереди | Статус | Семантика доставки |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | Не более одного раза |
| `RedisReliableQueue<T>` | ✅ | Не менее одного раза (RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | Отложенная доставка (Sorted Set) |
| `RedisStream<T>` | ✅ | Упорядоченность + потребительские группы + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | Широковещательная рассылка в несколько групп |
| `PubSub` + `RedisEventBus` | ✅ | Push в реальном времени |
| Распределённая блокировка RedLock | 🔄 | Многоэкземплярный алгоритм Redlock |

#### Поддержка кластера

| Режим | Статус |
|------|--------|
| Standalone | ✅ |
| Репликация Master-Replica | ✅ |
| Sentinel | ✅ |
| Native Cluster | ✅ |
| Автоадаптация kvrocks | ✅ |
| Автоопределение Garnet | ✅ |
| Автоопределение Valkey | ✅ |

#### Протокол и дополнительные возможности

| Возможность | Статус |
|---------|--------|
| Полная поддержка RESP2 | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| Аутентификация ACL | ✅ |
| Транзакции MULTI/EXEC/WATCH | ✅ |
| Lua-скрипты EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 В планах |
| Конвейер (явный и автоматический) | ✅ |
| Подписка по шаблону PSUBSCRIBE | 🔄 |
| Асинхронное удаление UNLINK | 🔄 |

</details>

<details>
<summary><b>Сравнение производительности</b></summary>

Встроенный бенчмарк в исходниках (`Samples/Benchmark`), типичные результаты (40 логических процессоров, локальный Redis 7.0):

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

Ручной запуск бенчмарка:

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> Фактическая производительность зависит от сетевого RTT / размера значения / сложности сериализации. Рекомендуется держать отдельные значения в диапазоне 1–1,4 КБ.

</details>

<details>
<summary><b>Рекомендации по использованию</b></summary>

- **Переиспользование singleton**: `FullRedis` имеет внутренний пул соединений; настоятельно рекомендуется использовать один экземпляр на всё время жизни приложения — не создавайте `new` каждый раз
- **Проектирование ключей**: используйте `:` как иерархический разделитель, например `user:1001:profile`; в производственной среде применяйте `Prefix` для изоляции пространств имён
- **Сначала пакетные операции**: предпочитайте `GetAll/SetAll` вместо циклических операций с одиночными ключами; используйте `AutoPipeline` для автоматического объединения операций записи
- **Размер значений**: держите значения в пределах 1–2 КБ; большие объекты разбивайте / сжимайте / используйте Hash для хранения на уровне полей
- **Надёжное потребление**: в производственной среде используйте `RedisReliableQueue<T>`, а не простой List, чтобы избежать потери сообщений
- **Облачные провайдеры**: в публичных облаках оставляйте `AutoDetect = false`, чтобы избежать проблем с адресами внутренних узлов
- **Мониторинг**: включайте статистику `Counter` в производственной среде; `ClientLog` включайте только при отладке, чтобы избежать накладных расходов на логирование в горячем пути
- **SSL**: включайте шифрованный транспорт TLS в публичных облаках и производственных средах

</details>

<details>
<summary><b>Часто задаваемые вопросы (FAQ)</b></summary>

**В: Поддерживается ли шардирование Redis Cluster?**
О: Да. `FullRedis` автоматически определяет режим Cluster при `AutoDetect=true`. Маршрутизация CRC16 и перенаправления MOVED/ASK полностью реализованы. Для режима прокси публичного облака оставляйте значение по умолчанию `AutoDetect=false`.

**В: Чем это отличается от StackExchange.Redis?**
О: NewLife.Redis использует модель пула соединений (а не мультиплексирование, как StackExchange) для более предсказуемого поведения соединений. Встроенные обёртки очередей сообщений (надёжная очередь / очередь задержки) — ключевое отличие. Кроме того, поддерживается .NET Framework 4.5. См. [Конкурентный анализ (中文)](Doc/竞品分析.md).

**В: Как бороться с медленными запросами из-за больших значений?**
О: Разделяйте структуры данных (например, Hash на уровне полей) + пакетные чтение и запись + бинарный кодировщик + контроль `MaxMessageSize` (по умолчанию ограничение 1 МБ).

**В: Что делать, если пул соединений исчерпан?**
О: По умолчанию максимум 100 000 соединений; нормальная нагрузка его не исчерпает. Пул настраивается через `Redis.PoolConfig` (`RedisPoolConfig`): минимум простаивающих / максимальная ёмкость / время очистки простаивающих / максимальное время жизни / тайм-аут ожидания выдачи. Соединения, простаивающие дольше `IdleTime`, при выдаче активно проверяются через PING; для сценариев переключения master-slave или последовательного обновления прокси уменьшайте `MaxLifetime`, чтобы быстрее перерабатывать соединения. Если вы видите тайм-ауты ожидания, проверьте утечки соединений (отсутствующий `Dispose`) или операции, удерживающие соединения чрезмерно долго.

**В: Почему соединения не проходят на Alibaba Cloud / Tencent Cloud?**
О: Проверьте: ① Установлен ли `AutoDetect` в `false` (правильное значение по умолчанию)? ② Корректен ли формат аутентификации (в Tencent Cloud требуется `username=instanceId`)? ③ Разрешают ли группа безопасности / белый список IP клиента? ④ Не запрещает ли кластерная версия использовать SELECT для переключения баз данных?

**В: Поддерживает ли Garnet все возможности?**
О: Базовые операции, List/Hash/Set/ZSet/HLL/Geo и очереди поддерживаются полностью. Stream пока не поддерживается и автоматически выбрасывает `NotSupportedException` с предложением использовать в качестве альтернативы `RedisQueue<T>`.

</details>

<details>
<summary><b>План развития</b></summary>

- [x] ~~Поддержка протокола RESP3~~ (Готово)
- [ ] Redis Functions (FCALL/FUNCTION, замена Lua-скриптов)
- [ ] LMPOP / ZMPOP (пакетный pop в Redis 7.0)
- [ ] Многоэкземплярная распределённая блокировка RedLock
- [ ] Операции Bitmap / Bitfield
- [ ] Полная реализация подписки по шаблону PSUBSCRIBE
- [ ] Асинхронное удаление UNLINK
- [ ] Адаптер экспорта метрик Prometheus
- [x] Расширение Alibaba Cloud Tair (поддерживаются TairString / TairHash / TairZSet)
- [ ] Адаптер сопоставления адресов узлов кластерной версии Huawei Cloud DCS
- [x] ~~Pub/Sub~~ (Готово)
- [x] ~~RedisStream с несколькими потребительскими группами~~ (Готово)
- [x] ~~Определение типа сервера Garnet~~ (Готово)

Отзывы и пожелания по функциям приветствуются через Issue / PR.

</details>

<details>
<summary><b>Сравнение с конкурентами</b></summary>

| Возможность | NewLife.Redis | StackExchange.Redis | FreeRedis | Примечания |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | Критично для унаследованных систем промышленности и госсектора |
| Модель соединений | Пул соединений | Мультиплексирование | Пул соединений | У каждого свои сценарии применения |
| Покрытие команд | ~98% | ~95% | ~98% | Все документированные команды, кроме явно не запланированных |
| Встроенные обёртки очередей сообщений | ✅ Полностью | ❌ | 🔄 Частично | Уникальные надёжные очереди и очереди задержки |
| Встроенная сериализация | ✅ | ❌ Вручную | ❌ Вручную | Снижает объём шаблонного кода |
| Специализированные адаптеры облачных провайдеров | ✅ (Alibaba/Tencent/Huawei) | Общие | Общие | Выделенная документация |
| Явный адаптер Garnet | ✅ | ❌ | ❌ | Определение типа + корректная деградация |
| Адаптер kvrocks | ✅ | ❌ | ❌ | Автоопределение |
| Автоопределение Valkey | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| Трассировка APM | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | Нативная интеграция с OpenTelemetry |
| RESP3 | ✅ | ✅ | ✅ | Проверено набором тестов RESP3 |

Подробности см. в [документе конкурентного анализа (中文)](Doc/竞品分析.md).

</details>

### История выпусков

- [ChangeLog.md](ChangeLog.md): история версий (китайский)
- [ChangeLog.en.md](ChangeLog.en.md): история версий (английский)

## Участие в разработке

### Сборка и тестирование

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- Интеграционным тестам нужен работающий Redis: задайте `REDIS_CONNECTION` или запустите его на `127.0.0.1:6379`; иначе тесты с пометками `[RedisFact]` / `[RedisTopologyFact]` автоматически пропускаются.
- Smoke-тесты NativeAOT: `AotSmoke/`, CI в `.github/workflows/aot.yml`.
- Бенчмарки: `Samples/Benchmark`.

### Структура репозитория

| Каталог | Описание |
| --- | --- |
| `NewLife.Redis/` | Исходники клиента: пул соединений, кодек RESP, структуры данных и очереди |
| `NewLife.Redis.Extensions/` | Интеграция с ASP.NET Core: `IDistributedCache`, DI и защита данных |
| `NewLife.Extensions.Caching.Redis/` | Пакет совместимости для устаревшего расширения кэширования |
| `XUnitTest/` | Набор тестов xUnit (net10.0) |
| `AotSmoke/` | Smoke-проект NativeAOT |
| `Samples/` | Примеры Benchmark, WebDemo, QueueDemo, RedisSwitch и mobile smoke |
| `Doc/` | Требования, архитектура, список функций и тематические документы |

### Выпуск

Отправьте тег с префиксом `v` (например, `v6.7.2026.1024`), чтобы запустить `.github/workflows/publish.yml`. Workflow собирает пакеты `NewLife.Redis` и `NewLife.Redis.Extensions`, публикует их в NuGet.org и GitHub Packages и создаёт GitHub Release с примечаниями из соответствующего раздела `ChangeLog.md`; ручной запуск только публикует пакеты.

### Обратная связь

1. Перед отправкой убедитесь, что `dotnet build` и соответствующие тесты проходят, и следуйте стилю кода и соглашениям о коммитах репозитория
2. PR: делайте изменения минимальными, добавляйте необходимые комментарии и описания тестов
3. Issues: указывайте версию, окружение выполнения и минимальный сценарий воспроизведения

Сообщество: группы QQ 1600800 / 1600838; GitHub Discussions / Issues.

## Команда

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

Команда NewLife основана в 2002 году и более двух десятилетий работает в экосистеме .NET и IoT, поддерживает около 100 проектов с открытым кодом (34 из них — основные) и предлагает интегрированные программно-аппаратные решения для энергетики, логистики, здравоохранения, промышленной автоматизации и интернет-отрасли. Мы следуем девизу «в учении нет старших — мастерство приносит уважение».

Open-source компоненты скачали <!--nuget-total:start-->6.48M<!--nuget-total:end--> раз с NuGet (по всей экосистеме, по состоянию на 2026-10).

Сайт: https://newlifex.com · Организация open source: https://github.com/NewLifeX

<details>
<summary>Официальный аккаунт WeChat: 智能大石头 (отсканируйте, чтобы подписаться)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## Лицензия

MIT License. Свободно для коммерческого использования / изменения / распространения (дополнительная авторизация не требуется). Достаточно сохранить уведомление об авторских правах.

> Если этот документ не покрывает ваш сценарий, смело открывайте Issue — давайте улучшать документацию вместе!
