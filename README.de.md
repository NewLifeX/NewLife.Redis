# NewLife.Redis — Hochleistungs-Redis-Client

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)

🌐 [中文](Readme.MD) | [English](Readme.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Español](README.es.md) | [Français](README.fr.md) | [Русский](README.ru.md)

---

`NewLife.Redis` ist ein **hochperformanter, hochdurchsatzfähiger und einfach integrierbarer** .NET Redis-Client, entwickelt vom NewLife-Team. Seit 2017 läuft er stabil auf zahlreichen großen Produktionsplattformen und verarbeitet täglich über **80 Milliarden** Befehlsaufrufe.

## Schnellstart

```bash
dotnet add package NewLife.Redis
```

```csharp
using NewLife.Caching;

// Verbindung
var redis = new FullRedis("server=127.0.0.1:6379;password=yourpwd;db=0");

// Grundlegende Operationen
redis.Set("key", "value");
var val = redis.Get<String>("key");

// Schlüsselablauf
redis.Set("temp", data, 60); // 60 Sekunden
```

## Kernfunktionen

- **Vollständiges Protokoll**: RESP2/RESP3, alle Redis 2.8~7.x-Befehle
- **Alle Datenstrukturen**: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog
- **Nachrichtenwarteschlangen**: Einfach, zuverlässig (RPOPLPUSH+Ack), verzögert, Stream-Multi-Gruppe
- **Cluster & Hochverfügbarkeit**: Standalone / Sentinel / Cluster, automatisches Failover
- **Cloud-Anpassung**: Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS
- **Breite Framework-Unterstützung**: .NET Framework 4.5 ~ .NET 10+
- **Garnet-Kompatibilität**: Automatische Erkennung von Microsoft Garnet / kvrocks, elegante Degradierung

## Erweiterungspakete

| Paket | Beschreibung |
|-----------|------|
| `NewLife.Redis` | Kern-Redis-Client |
| `NewLife.Redis.Extensions` | ASP.NET Core DI, `IDistributedCache`, `IDataProtection` |

## Dokumentation

> 📖 Vollständige Dokumentation (auf Chinesisch): [Anforderungen](Doc/需求文档.md) | [Architektur](Doc/架构文档.md) | [Wettbewerbsanalyse](Doc/竞品分析.md)

## Lizenz

MIT License © 2002-2026 [NewLife Entwicklungsteam](https://newlifex.com)

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [Offizielle Website](https://newlifex.com)
