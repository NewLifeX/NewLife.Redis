# NewLife.Redis — Hochleistungs-Redis-Client-Komponente

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` ist eine **hochperformante, hochdurchsatzfähige und einfach zu integrierende** Redis-Client-Komponente für .NET, entwickelt vom NewLife-Team. Seit 2017 läuft sie stabil auf mehreren Produktionsplattformen mit Milliarden von Datensätzen und hoher Parallelität und verarbeitet täglich **über 80 Milliarden** Befehlsaufrufe.

> 📖 Vollständige Dokumentation: [Anforderungen (中文)](Doc/需求文档.md) | [Architektur (中文)](Doc/架构文档.md) | [Funktionsliste (中文)](Doc/功能清单.md) | [Wettbewerbsanalyse (中文)](Doc/竞品分析.md) | [Garnet-Kompatibilität (中文)](Doc/Garnet兼容性.md) | [AOT-Kompatibilität (中文)](Doc/AOT兼容性分析.md)

**Inhalt**: [Schnellstart](#schnellstart) · [Hauptfunktionen](#hauptfunktionen) · [Ökosystem-Landkarte](#ökosystem-landkarte) · [Dokumentation](#dokumentation) · [Mitwirken](#mitwirken) · [Team](#team) · [Lizenz](#lizenz)

## Schnellstart

### Installation

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### Grundlegende Verwendung

**Empfohlen: Singleton, threadsicher, über die gesamte Anwendungslebensdauer wiederverwenden:**

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

**Modus über Verbindungszeichenfolge (empfohlen für Konfigurationszentralen):**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### Plattformunterstützung

| Element | Abdeckung |
| --- | --- |
| Zielframeworks | Ein Paket: net45, net461, netstandard2.0, netstandard2.1, net8.0; die Erweiterungen ergänzen netcoreapp3.1 → net10 |
| Betriebssystem und Architektur | Windows, Linux, macOS, Android, iOS; x64, arm64, loongarch64, riscv64, s390x |
| NativeAOT | `net8.0` wird mit `IsAotCompatible`-Prüfungen ausgeliefert; `AotSmoke/` plus die CI-Smoke-Matrix decken win-x64 / linux-x64 / linux-arm64 / linux-la64 ab (LA64 über QEMU), während win-arm64, macOS und musl-RIDs nativ kompiliert werden. linux-x64 ist das blockierende Gate, die übrigen stehen unter Beobachtung — siehe [AOT-Kompatibilität](Doc/AOT兼容性分析.md) |

## Hauptfunktionen

<details>
<summary><b>Kern-Engine</b> (7)</summary>

| Merkmal | Wichtigste Eigenschaften |
| --- | --- |
| Im großen Maßstab bewährt | Über 200 Redis-Instanzen im Produktivbetrieb, mit einer Tagesspitze von über 100 Mio. Geschäftsobjekt-Schreibvorgängen und über 80 Mrd. Befehlsaufrufen. |
| Geringe Latenz | Ein einzelner Get/Set-Roundtrip dauert inklusive Netzwerk 200–600 µs. |
| Hoher Durchsatz | Integrierter Verbindungspool (bis zu 100.000 gleichzeitig) mit synchronem RESP-Parser und optionaler automatischer Pipeline-Bündelung. |
| Automatische Wiederholung und Failover über mehrere Adressen | Kommagetrennte Knotenlisten, schnelles Failover bei Netzwerkfehlern und standardmäßig ein 10-sekündiges Abschirmfenster. |
| Austauschbarer Encoder | Standardmäßig JSON (`RedisJsonEncoder`), über `IPacketEncoder` bis zur binären Serialisierung erweiterbar. |
| APM-Tracing | Verteiltes Tracing mit `ITracer` sowie Leistungsindikatoren über `PerfCounter`. |
| Keine schwergewichtigen Abhängigkeiten | Nur das NewLife-Ökosystem (Protokollierung, Konfiguration, Serialisierung, Sicherheit) zusätzlich zur BCL. |

</details>

<details>
<summary><b>Datenstrukturen</b> (10)</summary>

| Struktur | Wichtigste Eigenschaften |
| --- | --- |
| String-KV + Batch | MGET / MSET / INCR / APPEND / GETRANGE und Verwandte. |
| List | LPUSH / RPOP / LRANGE / LMOVE. |
| Hash | HGET / HSET / HMGET / HSCAN. |
| Set | SADD / SMEMBERS / SUNION / SDIFF. |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN. |
| Stack | LIFO-Wrapper auf Basis von List. |
| Stream | Vollständiges XADD / XREAD / XGROUP / XACK. |
| Geo | GEOADD / GEODIST / GEOSEARCH. |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE. |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD (geplant). |

</details>

<details>
<summary><b>Nachrichtenwarteschlangen und Pub/Sub</b> (7)</summary>

| Typ | Wichtigste Eigenschaften |
| --- | --- |
| `RedisQueue<T>` | Einfache Warteschlange mit At-most-once-Zustellung. |
| `RedisReliableQueue<T>` | At-least-once-Zustellung mit RPOPLPUSH + Ack-Rollback. |
| `RedisDelayQueue<T>` | Verzögerte Zustellung auf Basis eines Sorted Set. |
| `RedisStream<T>` | Geordneter Stream mit Consumergruppen und XACK. |
| `MultipleConsumerGroupsQueue<T>` | Broadcast an mehrere Consumergruppen. |
| `PubSub` + `RedisEventBus` | Echtzeit-Publish/Subscribe. |
| Verteiltes RedLock-Sperrsystem | Redlock-Algorithmus über mehrere Instanzen (in Arbeit). |

</details>

<details>
<summary><b>Clustering und Hochverfügbarkeit</b> (7)</summary>

| Modus | Wichtigste Eigenschaften |
| --- | --- |
| Standalone | Direkte Verbindung zu einer Einzelinstanz mit minimalem Ressourcenbedarf. |
| Master-Replica-Replikation | Trennung von Lese- und Schreibzugriffen sowie Failover. |
| Sentinel | Sentinel-Erkennung mit automatischem Master-Wechsel. |
| Native Cluster | CRC16-Slot-Routing mit MOVED-/ASK-Umleitung. |
| kvrocks | Passt sich automatisch an die kvrocks-Speicher-Engine an. |
| Garnet | Erkennt Microsoft Garnet automatisch und degradiert elegant, wenn eine Funktion fehlt. |
| Valkey | Wird aus den Server-INFO-Daten erkannt. |

</details>

<details>
<summary><b>Protokoll- und Serverkompatibilität</b> (7)</summary>

| Fähigkeit | Wichtigste Eigenschaften |
| --- | --- |
| RESP2 | Simple String, Error, Integer, Bulk String und Array vollständig implementiert. |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error, ausgehandelt über den HELLO-Handshake (Redis 6.0+). |
| Redis 2.8 – 7.x | Alle gängigen Befehle jeder Version, einschließlich COPY / GETDEL / GETEX / GEOSEARCH ab 6.2. |
| ACL-Authentifizierung | Authentifizierung mit Benutzername und Passwort ab Redis 6.0. |
| Transaktionen | MULTI / EXEC / WATCH. |
| Lua-Skripte | EVAL / EVALSHA. |
| Redis Functions | FCALL / FUNCTION (geplant). |

</details>

<details>
<summary><b>Cloud, Sicherheit und Beobachtbarkeit</b> (8)</summary>

| Fähigkeit | Wichtigste Eigenschaften |
| --- | --- |
| Alibaba Cloud KVStore / Tair | Standard-Edition, ACL, SSL und Hinweise zum Proxy-Modus sowie Tair-Strukturen (TairString / TairHash / TairZSet). |
| Tencent Cloud Redis | Verwendet die Instanz-ID als Benutzernamen (`instanceId:password`). |
| Huawei Cloud DCS | Unterschiede zwischen Master/Standby-, SSL- und Cluster-Edition (kein SELECT im Cluster-Modus). |
| TLS/SSL | Konfigurierbare Protokollversion und gegenseitige TLS-Authentifizierung mit Client-Zertifikaten. |
| Strukturierte Protokollierung | Komponentenprotokoll (`Log`) und Protokoll auf Protokollebene (`ClientLog`). |
| Leistungsindikatoren | `PerfCounter` meldet Ops/s, Latenz und Fehlerrate. |
| APM-Tracing | `ITracer`-Spans an allen Stellen plus eine `ActivitySource` (net8.0+) für OpenTelemetry. |
| Schutz von Verbindungszeichenfolgen | `ProtectedKey` entschlüsselt Passwörter, die in Verbindungszeichenfolgen eingebettet sind. |

</details>

<details>
<summary><b>Plattform und Integration</b> (5)</summary>

| Fähigkeit | Wichtigste Eigenschaften |
| --- | --- |
| Mehrere Zielframeworks | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0; die Erweiterungspakete decken netcoreapp3.1 → net10 ab. |
| Starke Namensgebung | Mit `newlife.snk` signiert, sodass Assemblys neu gebaut und ersetzt werden können. |
| NativeAOT | Statische `IsAotCompatible`-Prüfungen, das Projekt `AotSmoke/` und eine CI-Matrix über mehrere RIDs. |
| ASP.NET Core-Paket | `NewLife.Redis.Extensions` registriert `IDistributedCache` und `FullRedis` über `AddRedisCaching`. |
| DataProtection | `PersistKeysToRedis` speichert Schlüssel zum Datenschutz in Redis. |

</details>

## Ökosystem-Landkarte

NewLife ist ein geschichtetes Ökosystem unabhängig versionierter Open-Source-Projekte. Die folgende Landkarte wird von jedem NewLife-Repository wortgleich geteilt; jedes Projekt verlinkt auf sein Repository und sein primäres NuGet-Paket.

| Projekt | Funktion | Downloads |
| --- | --- | --- |
| **Grundlage** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Protokollierung, Konfiguration, Caching, Netzwerk, Serialisierung, Zeitplanung und APM-Tracing. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Daten-Middleware: einzelne Tabellen in der Größenordnung von zig Milliarden Zeilen, automatisches Sharding, Lese-/Schreibtrennung, über 10 Datenbanken. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Hochperformante Netzwerkbibliothek: 1,77 Mio. pps / 14,5 Gbps auf einer Maschine (TCP 1KB, 2026-10), 4 Mio. langlebige TCP-Verbindungen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Protokollkommunikation und RPC-Framework für Client/Server-Anwendungen und IoT-Geräte. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installiert .NET-Anwendungen als Betriebssystem-Dienste: Windows-Dienste und Linux-systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new`-Projektvorlagen für Konsole, Web, API, Dienst, Daten und IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Daten & Messaging** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis-Client mit Mikrosekunden-Latenz, Millionen Operationen pro Sekunde und integrierten Nachrichtenwarteschlangen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Vollständig verwalteter RocketMQ-Client für Apache RocketMQ und Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT-Client und -Server, einschließlich Alibaba Cloud IoT-Anbindung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Hybriddatenbank in C#: eingebettet oder im Netzwerk, relational, Zeitreihen, Nachrichtenwarteschlange und KV in einem. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql-Treiber und Hilfsschicht auf Basis des NewLife-Datenstacks. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, KI & verteilt** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Schnelles Framework für Administrationsplattformen: Berechtigungen, SSO, OAuth-Server und CRUD-Gerüst. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Verteilte Dienstplattform: Knotenverwaltung, APM, Konfigurationszentrum, Registry und Release-Center. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plattform für verteilte Aufgaben und Echtzeit-Datenplanung mit Sharding, Wiederholungen und Backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Einheitlicher Client für 46 LLM-Anbieter plus ChatAI, eine sofort einsatzbereite Web-Chat-Anwendung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrie & IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT-Standardbibliothek mit Protokollverträgen und Geräteabstraktionen. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus-TCP/RTU/ASCII-Implementierung auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Siemens-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Omron-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Mitsubishi-Melsec-SPS-Protokoll auf Basis von NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC-Client für industriellen Datenzugriff. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet-Protokollstack für die Gebäudeautomation. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Leichtgewichtige Open-Source-IoT-Plattform mit Gateway- und Geräteverwaltung. | — |
| **Fahrzeugtelematik** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T-808-Fahrzeugortungsprotokoll: über 70 Nachrichtenkörper, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T-809-Plattform-zu-Plattform-Protokoll für Verkehrsbehörden. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T-1078-Fahrzeugvideo: RTP-Parsing, FLV/fMP4-Remuxing und Live-Streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Domänenkomponenten** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Ein Paket für Excel, Word, PPT, PDF und über 15 Dokumentformate. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Plattformübergreifendes Audio: Codecs, Geräte-I/O, DSP, Container und Streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Videocodecs, Containerformate, Frame-Verarbeitung und Streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Einheitliche Schnittstelle für Baidu, AMap, Tencent und Tianditu mit Geokodierung und Koordinatenumrechnung. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4-Geolokalisierung mit einer offline speichergemappten Datenbank und einer Online-API als Fallback. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Werkzeuge & Produkte** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Entwickler-Toolbox: Netzwerk, serielle Schnittstelle, Krypto, Regex, Modbus und MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Selbst gehostetes Git-Quellcode-Verwaltungssystem. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installiert automatisch die passende .NET-Laufzeit, wenn es von einem älteren Host gestartet wird. | — |

> Die Badges zeigen das primäre Paket jedes Projekts; Theme-, Erweiterungs- und Satellitenpakete sind auf den jeweiligen NuGet-Seiten aufgeführt. Die hier nicht aufgeführten über 30 Pakete (FTP, LoRa, Office-Erweiterungen, Protokolltreiber und mehr) sind in der [NewLifeX-Organisation](https://github.com/NewLifeX) und auf [newlifex.com](https://newlifex.com) verfügbar.

## Dokumentation

**NewLife.Redis** bietet .NET-Entwicklern Redis-Zugriff über den gesamten Lebenszyklus:

- **Vollständiges Protokoll**: Implementiert RESP2, deckt alle gängigen Befehle von Redis 2.8 bis 7.x ab und unterstützt alle Datenstrukturen: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog.
- **Nachrichtenwarteschlangen**: Integrierte einfache Warteschlange, **zuverlässige Warteschlange (RPOPLPUSH + Ack-Semantik)**, Verzögerungswarteschlange und Stream-Warteschlange mit mehreren Consumergruppen — sofort einsatzbereit, kein Nachbau nötig.
- **Clustering & Hochverfügbarkeit**: Unterstützt Standalone / Master-Replica / Sentinel / Native Cluster mit automatischem Failover.
- **Cloud-Provider-Adapter**: Spezielle Konfiguration und Anpassung für **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**, einschließlich Proxy-Modus, ACL-Authentifizierung, SSL-Verschlüsselung, besonderer Auth-Formate und weiterer Unterschiede.
- **Kompatible Implementierungen**: Erkennt Microsoft Garnet, kvrocks und andere Redis-kompatible Server automatisch und degradiert bei nicht unterstützten Funktionen elegant mit klaren Alternativen.
- **Breite Framework-Unterstützung**: Ein einziges Paket deckt **.NET Framework 4.5 bis .NET 10+** ab und eignet sich für Industrieautomation, Behörden, IoT und andere Legacy-Szenarien.

### Leitfäden

- [Anforderungen (中文)](Doc/需求文档.md): Vision, Ziele und funktionale Anforderungen
- [Architektur (中文)](Doc/架构文档.md): Schichtung, Komponenten und zentrale Designentscheidungen
- [Funktionsliste (中文)](Doc/功能清单.md): Implementierungs-, Test- und Kommentarstatus jeder Funktion
- [Wettbewerbsanalyse (中文)](Doc/竞品分析.md): Vergleich mit gängigen Redis-Clients
- [Garnet-Kompatibilität (中文)](Doc/Garnet兼容性.md): unterstützter Umfang und elegante Degradierung
- [AOT-Kompatibilität (中文)](Doc/AOT兼容性分析.md): NativeAOT-Umfang und gemessene Ergebnisse
- [Lokalisierung (中文)](Doc/多语言本地化.md): Nachrichtenmodell für Ausnahmen und Protokolle
- [RedisSwitch-Integration (中文)](Doc/RedisSwitch项目说明.md): Werkzeug für unterbrechungsfreien Redis-Wechsel
- [GC-Allokationsanalyse (中文)](Doc/dotMemory分析Redis的GC分配.md): dotMemory-Messungen
- [Offizielle Website](https://newlifex.com/core/redis): Komponentenseite und Online-Dokumentation

### Protokoll- und Versionsunterstützung

#### RESP-Protokollunterstützung

| Protokoll | Status | Beschreibung |
|----------|--------|-------------|
| RESP2 | ✅ Vollständig unterstützt | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ Vollständig unterstützt | Typen Map / Set / Double / Boolean / Null / Blob Error, ausgehandelt über den HELLO-Handshake (Redis 6.0+) |

#### Funktionsunterstützung nach Redis-Version

| Redis-Version | Zentrale Funktionen | Status |
|---------------|--------------|--------|
| 2.8 | Grundbefehle, SCAN, HyperLogLog, Pub/Sub | ✅ Vollständig unterstützt |
| 3.0 | Native Cluster, CRC16-Shard-Routing | ✅ Vollständig unterstützt |
| 3.2 | GEO-Raumdaten | ✅ Vollständig unterstützt |
| 4.0 | UNLINK asynchrones Löschen, MODULE | 🔄 Teilweise unterstützt |
| 5.0 | Stream-Datenstruktur (vollständiges XADD/XREAD/XGROUP/XACK) | ✅ Vollständig unterstützt |
| 6.0 | ACL-Zugriffskontrolle, TLS/SSL, LMOVE, RESET | ✅ Vollständig unterstützt |
| 6.2 | COPY, GETDEL, GETEX, GEOSEARCH | ✅ Kernbefehle unterstützt |
| 7.0 | Functions (FCALL), LMPOP/ZMPOP, Sharded Pub/Sub | 🔄 Geplant |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 Geplant |

### Architektur

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

Beschreibung der zentralen Klassen:

| Klasse | Rolle |
|-------|------|
| `Redis` | Kern-Client: Verbindungspool, Failover über mehrere Knoten, Pipeline, Konfigurationsverwaltung |
| `FullRedis` | Erweiterter Client: Cluster-Routing, Schlüsselpräfix, Factory-Methoden für Datenstrukturen |
| `RedisClient` | RESP-Protokolleinheit: TCP-Verbindung, Kodierung/Dekodierung von Befehlen (intern, vom Pool verwaltet) |
| `RedisList<T>` usw. | Generische Wrapper für Datenstrukturen mit einheitlicher Kodierungsstrategie |
| `RedisReliableQueue<T>` | Zuverlässige Nachrichtenwarteschlange (RPOPLPUSH + Ack-Bestätigung) |
| `RedisStream<T>` | Stream-Warteschlange (Redis 5.0+, Consumergruppen-Modus) |
| `IPacketEncoder` | Austauschbare Kodierungs-/Dekodierungsstrategie |

### Anwendungsbeispiele

Jedes der folgenden Beispiele läuft gegen eine aktive `FullRedis`-Instanz, die im [Schnellstart](#schnellstart) erstellt wird.

<details>
<summary><b>Grundlegende Operationen</b></summary>

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
<summary><b>Datenstrukturen</b></summary>

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

> Hinweis: Die Basisinstanz `Redis` unterstützt nur KV-Operationen. Für erweiterte Sammlungen verwenden Sie `FullRedis`.

</details>

<details>
<summary><b>Pipeline und automatische Bündelung</b></summary>

Pipelines reduzieren die Netzwerk-Roundtrips (RTT) erheblich und erhöhen den Durchsatz von Massenoperationen:

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**Automatikmodus** (für die Produktion empfohlen):

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>Nachrichtenwarteschlangen</b></summary>

#### Einfache Warteschlange

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### Zuverlässige Warteschlange (At-least-once-Semantik)

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

#### Verzögerungswarteschlange

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### Stream-Warteschlange (Redis 5.0+, mehrere Consumergruppen)

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
<summary><b>Publish / Subscribe</b></summary>

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
<summary><b>Clustering und Hochverfügbarkeit</b></summary>

#### Automatisches Failover über mehrere Adressen

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### Automatische Erkennung des Cluster-Modus

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
<summary><b>Cloud-Provider-Adapter</b></summary>

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

**Allgemeine Hinweise zu Cloud-Providern:**
- Proxy-Modus öffentlicher Clouds: `AutoDetect = false` (Standard) beibehalten, um die automatische Erkennung interner Knoten zu verhindern
- Verbindungskontingent: Jeder Cloud-Anbieter und jede Instanzgröße hat Verbindungsgrenzen; setzen Sie den Parameter `Max` des Pools entsprechend
- TCP-Keepalive: Huawei Cloud verwendet standardmäßig eine TCP-Leerlaufzeit von 300 Sekunden; setzen Sie `Timeout` unter diesen Wert und senden Sie regelmäßig PING

</details>

<details>
<summary><b>Serialisierung und Encoder</b></summary>

Standard-Encoder: `RedisJsonEncoder` (integrierter JSON-Host), sofort einsatzbereit.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

Primitive Typen (`String` / `Int32` / `Int64` / `Double` / `Boolean`) werden für maximale Leistung direkt und ohne den Umweg über JSON serialisiert.

</details>

<details>
<summary><b>Sicherheit und Authentifizierung</b></summary>

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
<summary><b>Beobachtbarkeit</b></summary>

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
<summary><b>Erweiterungspakete</b></summary>

`NewLife.Redis.Extensions` bietet eine tiefe Integration in ASP.NET Core:

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
<summary><b>Garnet-Kompatibilität</b></summary>

NewLife.Redis unterstützt den hochperformanten Redis-kompatiblen Server [Garnet](https://github.com/microsoft/garnet) von Microsoft vollständig, einschließlich automatischer Erkennung und eleganter Degradierung.

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| Funktion | Garnet-Unterstützungsstatus |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ Vollständig unterstützt |
| HyperLogLog / Geo | ✅ Vollständig unterstützt |
| Pub/Sub / Transaktionen / Lua-Skripte | ✅ Vollständig unterstützt |
| Pipeline | ✅ Vollständig unterstützt |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ Vollständig unterstützt |
| `RedisReliableQueue<T>` | ✅ Vollständig unterstützt |
| `RedisStream<T>` | ❌ Garnet unterstützt Stream noch nicht; es wird automatisch eine `NotSupportedException` mit vorgeschlagenen Alternativen ausgelöst |

Siehe: [Dokumentation zur Garnet-Kompatibilität (中文)](Doc/Garnet兼容性.md)

</details>

### Referenz

<details>
<summary><b>Implementierungsstatus der Funktionen</b></summary>

> Legende: ✅ Abgeschlossen | 🔄 Geplant | ❌ Ausdrücklich nicht geplant

#### Datenstrukturen

| Struktur | Status | Beschreibung |
|-----------|--------|-------------|
| String KV + Batch | ✅ | MGET/MSET/INCR/APPEND/GETRANGE usw. |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE usw. |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN usw. |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF usw. |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN usw. |
| Stream | ✅ | Vollständiges XADD/XREAD/XGROUP/XACK |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH usw. |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

#### Nachrichtenwarteschlangen

| Warteschlangentyp | Status | Zustellsemantik |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | Höchstens einmal |
| `RedisReliableQueue<T>` | ✅ | Mindestens einmal (RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | Verzögerte Zustellung (Sorted Set) |
| `RedisStream<T>` | ✅ | Geordnet + Consumergruppe + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | Broadcast an mehrere Consumergruppen |
| `PubSub` + `RedisEventBus` | ✅ | Echtzeit-Push |
| Verteiltes RedLock-Sperrsystem | 🔄 | Redlock-Algorithmus über mehrere Instanzen |

#### Cluster-Unterstützung

| Modus | Status |
|------|--------|
| Standalone | ✅ |
| Master-Replica-Replikation | ✅ |
| Sentinel | ✅ |
| Native Cluster | ✅ |
| kvrocks-Autoanpassung | ✅ |
| Garnet-Autoerkennung | ✅ |
| Valkey-Autoerkennung | ✅ |

#### Protokoll & erweiterte Funktionen

| Funktion | Status |
|---------|--------|
| RESP2 vollständige Unterstützung | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| ACL-Authentifizierung | ✅ |
| Transaktionen MULTI/EXEC/WATCH | ✅ |
| Lua-Skripte EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 Geplant |
| Pipeline (explizit + automatisch) | ✅ |
| Musterabonnement PSUBSCRIBE | 🔄 |
| UNLINK asynchrones Löschen | 🔄 |

</details>

<details>
<summary><b>Leistungsbenchmarks</b></summary>

Integrierter Benchmark im Quellcode (`Samples/Benchmark`), typische Ergebnisse (40 logische Prozessoren, lokales Redis 7.0):

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

Manuelles Auslösen des Benchmarks:

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> Die tatsächliche Leistung hängt von Netzwerk-RTT, Wertgröße und Komplexität der Serialisierung ab. Es wird empfohlen, einzelne Werte zwischen 1–1,4 KB zu halten.

</details>

<details>
<summary><b>Best Practices</b></summary>

- **Singleton-Wiederverwendung**: `FullRedis` verfügt über einen internen Verbindungspool; verwenden Sie dringend eine einzige Instanz über die gesamte Anwendungslebensdauer — nicht jedes Mal `new`
- **Schlüsseldesign**: Verwenden Sie `:` als hierarchisches Trennzeichen, z. B. `user:1001:profile`; nutzen Sie `Prefix` zur Namespace-Isolierung in der Produktion
- **Batch zuerst**: Bevorzugen Sie `GetAll/SetAll` gegenüber Schleifen mit Einzelschlüsseloperationen; mit `AutoPipeline` werden Schreibvorgänge automatisch zusammengefasst
- **Wertgröße**: Halten Sie Werte im Bereich von 1–2 KB; große Objekte aufteilen / komprimieren / Hash für feldweise Speicherung verwenden
- **Zuverlässiger Verbrauch**: Verwenden Sie in der Produktion `RedisReliableQueue<T>` statt einer einfachen List, um Nachrichtenverluste zu vermeiden
- **Cloud-Provider**: In öffentlichen Clouds `AutoDetect = false` beibehalten, um Probleme mit internen Knotenadressen zu vermeiden
- **Überwachung**: Aktivieren Sie in der Produktion die `Counter`-Statistik; aktivieren Sie `ClientLog` nur beim Debuggen, um Mehraufwand im Hot Path zu vermeiden
- **SSL**: Aktivieren Sie den verschlüsselten TLS-Transport in öffentlichen Clouds und Produktionsumgebungen

</details>

<details>
<summary><b>FAQ</b></summary>

**F: Wird das Sharding von Redis Cluster unterstützt?**
A: Ja. `FullRedis` erkennt den Cluster-Modus mit `AutoDetect=true` automatisch. CRC16-Routing sowie MOVED-/ASK-Umleitungen sind vollständig implementiert. Für den Proxy-Modus öffentlicher Clouds behalten Sie den Standardwert `AutoDetect=false` bei.

**F: Wie schneidet es im Vergleich zu StackExchange.Redis ab?**
A: NewLife.Redis verwendet ein Verbindungspool-Modell (im Gegensatz zum Multiplexing von StackExchange) für ein intuitiveres Verbindungsverhalten. Integrierte Wrapper für Nachrichtenwarteschlangen (zuverlässige Warteschlange / Verzögerungswarteschlange) sind ein wesentliches Unterscheidungsmerkmal. Außerdem wird .NET Framework 4.5 unterstützt. Siehe [Wettbewerbsanalyse (中文)](Doc/竞品分析.md).

**F: Wie geht man mit langsamen Abfragen durch große Werte um?**
A: Datenstrukturen aufteilen (z. B. Hash pro Feld) + Batch-Lesen/Schreiben + binärer Encoder + `MaxMessageSize` begrenzen (Standardlimit 1 MB).

**F: Was tun, wenn der Verbindungspool erschöpft ist?**
A: Das Standardmaximum liegt bei 100.000 Verbindungen; normale Arbeitslasten erreichen das nicht. Der Pool ist über `Redis.PoolConfig` (`RedisPoolConfig`) konfigurierbar: minimale Leerlaufverbindungen / maximale Kapazität / Leerlaufbereinigungszeit / maximale Lebensdauer / Wartezeit beim Ausleihen. Verbindungen, die länger als `IdleTime` im Leerlauf sind, werden bei der Ausleihe proaktiv per PING geprüft; für Master-Slave-Wechsel oder rollierende Proxy-Updates senken Sie `MaxLifetime`, um Verbindungen schneller zu recyceln. Bei Wartezeitüberschreitungen prüfen Sie auf Verbindungslecks (fehlendes `Dispose`) oder Vorgänge, die Verbindungen übermäßig lange halten.

**F: Warum schlagen Verbindungen bei Alibaba Cloud / Tencent Cloud fehl?**
A: Prüfen Sie: ① Ist `AutoDetect` auf `false` gesetzt (korrekter Standard)? ② Ist das Auth-Format korrekt (Tencent Cloud erfordert `username=instanceId`)? ③ Erlauben die Netzwerksicherheitsgruppe / Whitelist die Client-IP? ④ Verbietet die Cluster-Edition die Verwendung von SELECT zum Wechseln der Datenbank?

**F: Unterstützt Garnet alle Funktionen?**
A: Grundoperationen, List/Hash/Set/ZSet/HLL/Geo sowie Warteschlangen werden alle unterstützt. Stream wird noch nicht unterstützt und löst automatisch eine `NotSupportedException` mit dem Hinweis aus, stattdessen `RedisQueue<T>` zu verwenden.

</details>

<details>
<summary><b>Roadmap</b></summary>

- [x] ~~RESP3-Protokollunterstützung~~ (Abgeschlossen)
- [ ] Redis Functions (FCALL/FUNCTION als Ersatz für Lua-Skripte)
- [ ] LMPOP / ZMPOP (Batch-Pop ab Redis 7.0)
- [ ] Verteiltes RedLock-Sperrsystem über mehrere Instanzen
- [ ] Bitmap-/Bitfield-Operationen
- [ ] Vollständige Implementierung des Musterabonnements PSUBSCRIBE
- [ ] UNLINK asynchrones Löschen
- [ ] Adapter für den Prometheus-Metrikexport
- [x] Alibaba Cloud Tair-Erweiterung (TairString / TairHash / TairZSet unterstützt)
- [ ] Adapter für die Knotenadresszuordnung der Huawei Cloud DCS Cluster-Edition
- [x] ~~Pub/Sub~~ (Abgeschlossen)
- [x] ~~RedisStream mit mehreren Consumergruppen~~ (Abgeschlossen)
- [x] ~~Erkennung des Garnet-Servertyps~~ (Abgeschlossen)

Feedback und Funktionswünsche sind über Issue / PR willkommen.

</details>

<details>
<summary><b>Wettbewerbsvergleich</b></summary>

| Fähigkeit | NewLife.Redis | StackExchange.Redis | FreeRedis | Hinweise |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | Entscheidend für Legacy-Systeme in Industrie und Behörden |
| Verbindungsmodell | Verbindungspool | Multiplexing | Verbindungspool | Jedes Modell hat seine Einsatzbereiche |
| Befehlsabdeckung | ~98% | ~95% | ~98% | Alle dokumentierten Befehle außer den ausdrücklich nicht geplanten |
| Integrierte Wrapper für Nachrichtenwarteschlangen | ✅ Vollständig | ❌ | 🔄 Teilweise | Einzigartige zuverlässige / verzögerte Warteschlangen |
| Integrierte Serialisierung | ✅ | ❌ Manuell | ❌ Manuell | Reduziert Boilerplate-Code |
| Spezialisierte Cloud-Provider-Adapter | ✅ (Alibaba/Tencent/Huawei) | Generisch | Generisch | Eigene Dokumentation |
| Expliziter Garnet-Adapter | ✅ | ❌ | ❌ | Typerkennung + elegante Degradierung |
| kvrocks-Adapter | ✅ | ❌ | ❌ | Automatische Erkennung |
| Valkey-Autoerkennung | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| APM-Tracing | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | Native OpenTelemetry-Integration |
| RESP3 | ✅ | ✅ | ✅ | Über die RESP3-Testsuite verifiziert |

Details finden Sie im [Dokument zur Wettbewerbsanalyse (中文)](Doc/竞品分析.md).

</details>

### Versionshinweise

- [ChangeLog.md](ChangeLog.md): Versionshistorie (Chinesisch)
- [ChangeLog.en.md](ChangeLog.en.md): Versionshistorie (Englisch)

## Mitwirken

### Erstellen und Testen

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- Integrationstests benötigen ein laufendes Redis: Legen Sie `REDIS_CONNECTION` fest oder starten Sie eines auf `127.0.0.1:6379`; andernfalls werden Tests mit `[RedisFact]` / `[RedisTopologyFact]` automatisch übersprungen.
- NativeAOT-Smoke: `AotSmoke/`, CI in `.github/workflows/aot.yml`.
- Benchmarks: `Samples/Benchmark`.

### Repository-Struktur

| Verzeichnis | Beschreibung |
| --- | --- |
| `NewLife.Redis/` | Client-Quellen: Verbindungspool, RESP-Codec, Datenstrukturen und Warteschlangen |
| `NewLife.Redis.Extensions/` | ASP.NET Core-Integration: `IDistributedCache`, DI und Datenschutz |
| `NewLife.Extensions.Caching.Redis/` | Kompatibilitätspaket für die ältere Caching-Erweiterung |
| `XUnitTest/` | xUnit-Testsuite (net10.0) |
| `AotSmoke/` | NativeAOT-Smoke-Projekt |
| `Samples/` | Beispiele für Benchmark, WebDemo, QueueDemo, RedisSwitch und Mobile-Smoke |
| `Doc/` | Anforderungen, Architektur, Funktionsliste und Themendokumente |

### Veröffentlichung

Ein Push eines Tags mit dem Präfix `v` (zum Beispiel `v7.0.2026.1024`) löst `.github/workflows/publish.yml` aus. Der Workflow packt `NewLife.Redis` und `NewLife.Redis.Extensions`, veröffentlicht sie auf NuGet.org und GitHub Packages und erstellt ein GitHub Release mit den Notizen aus dem passenden Abschnitt der `ChangeLog.md`; ein manueller Lauf veröffentlicht nur die Pakete.

### Feedback

1. Stellen Sie vor dem Einreichen sicher, dass `dotnet build` und die relevanten Tests erfolgreich laufen, und halten Sie den Codestil und die Commit-Konventionen des Repositorys ein
2. PRs: Halten Sie die Änderungen minimal und ergänzen Sie notwendige Kommentare und Testbeschreibungen
3. Issues: Geben Sie Version, Laufzeitumgebung und ein minimales Reproduktionsszenario an

Community: QQ-Gruppen 1600800 / 1600838; GitHub Discussions / Issues.

## Team

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

NewLife wurde 2002 gegründet und widmet sich seit mehr als zwei Jahrzehnten dem .NET- und IoT-Ökosystem, betreut fast 100 Open-Source-Projekte (34 davon Hauptprojekte) und liefert integrierte Hard- und Softwarelösungen für die Bereiche Energie, Logistik, Gesundheitswesen, Industrieautomation und Internet. Unser Leitspruch lautet „Lernen kennt kein Dienstalter – wer es beherrscht, wird zum Lehrmeister“.

Die Open-Source-Komponenten wurden <!--nuget-total:start-->6.48M<!--nuget-total:end--> Mal von NuGet heruntergeladen (ökosystemweit, Stand 2026-10).

Website: https://newlifex.com · Open-Source-Organisation: https://github.com/NewLifeX

<details>
<summary>WeChat-Offizieller Account: 智能大石头 (zum Folgen scannen)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## Lizenz

MIT-Lizenz. Kostenlose kommerzielle Nutzung / Änderung / Weitergabe (keine zusätzliche Autorisierung erforderlich). Der Urheberrechtshinweis muss lediglich beibehalten werden.

> Wenn dieses Dokument Ihren Anwendungsfall nicht abdeckt, reichen Sie gerne ein Issue ein — verbessern wir die Dokumentation gemeinsam!
