# NewLife.Redis — Высокопроизводительный клиент Redis

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
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

---

`NewLife.Redis` — это **высокопроизводительный, высокопропускной и легко интегрируемый** .NET клиент Redis, разработанный командой NewLife. С 2017 года он стабильно работает на множестве крупных производственных платформ, обрабатывая более **80 миллиардов** вызовов команд в день.

## Быстрый Старт

```bash
dotnet add package NewLife.Redis
```

```csharp
using NewLife.Caching;

// Подключение
var redis = new FullRedis("server=127.0.0.1:6379;password=yourpwd;db=0");

// Основные операции
redis.Set("key", "value");
var val = redis.Get<String>("key");

// Срок действия ключа
redis.Set("temp", data, 60); // 60 секунд
```

## Ключевые Возможности

- **Полный Протокол**: RESP2/RESP3, все команды Redis 2.8~7.x
- **Все Структуры Данных**: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog
- **Очереди Сообщений**: Простая, надёжная (RPOPLPUSH+Ack), отложенная, Stream с группами
- **Кластер и Высокая Доступность**: Standalone / Sentinel / Cluster, автоматическое переключение
- **Облачная Адаптация**: Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS
- **Широкая Поддержка Framework**: .NET Framework 4.5 ~ .NET 10+
- **Совместимость с Garnet**: Автоопределение Microsoft Garnet / kvrocks, плавная деградация

## Расширения

| Пакет | Описание |
|-----------|------|
| `NewLife.Redis` | Основной клиент Redis |
| `NewLife.Redis.Extensions` | ASP.NET Core DI, `IDistributedCache`, `IDataProtection` |

## Документация

> 📖 Полная документация (на китайском): [Требования](Doc/需求文档.md) | [Архитектура](Doc/架构文档.md) | [Анализ Конкурентов](Doc/竞品分析.md)

## Лицензия

MIT License © 2002-2026 [Команда Разработки NewLife](https://newlifex.com)

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [Официальный Сайт](https://newlifex.com)
