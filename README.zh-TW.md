# NewLife.Redis — 高效能 Redis 用戶端元件

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` 是 NewLife 團隊打造的 **高效能／高吞吐／易整合** .NET Redis 用戶端元件。自 2017 年起，它已在多個生產平台穩定運行，承載數十億筆記錄與高併發，每日處理 **800 億+** 次命令呼叫。

> 📖 完整文件：[需求文件（中文）](Doc/需求文档.md) | [架構文件（中文）](Doc/架构文档.md) | [功能清單（中文）](Doc/功能清单.md) | [競品分析（中文）](Doc/竞品分析.md) | [Garnet 相容性（中文）](Doc/Garnet兼容性.md) | [AOT 相容性（中文）](Doc/AOT兼容性分析.md)

**目錄**：[快速開始](#快速開始) · [主要功能](#主要功能) · [生態版圖](#生態版圖) · [文件](#文件) · [參與貢獻](#參與貢獻) · [團隊](#團隊) · [授權條款](#授權條款)

## 快速開始

### 安裝

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### 基本用法

**建議：單例、執行緒安全，於應用程式整個生命週期重複使用：**

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

**連線字串模式（建議用於設定中心）：**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### 平台支援

| 項目 | 涵蓋範圍 |
| --- | --- |
| 目標框架 | 單一套件：net45、net461、netstandard2.0、netstandard2.1、net8.0；擴充套件額外涵蓋 netcoreapp3.1 → net10 |
| 作業系統與架構 | Windows、Linux、macOS、Android、iOS；x64、arm64、loongarch64、riscv64、s390x |
| NativeAOT | `net8.0` 隨附 `IsAotCompatible` 檢查；`AotSmoke/` 搭配 CI 冒煙矩陣涵蓋 win-x64 / linux-x64 / linux-arm64 / linux-la64（LA64 透過 QEMU），win-arm64、macOS 與 musl RID 則為原生編譯。linux-x64 為阻斷性關卡，其餘持續觀察 — 詳見 [AOT 相容性](Doc/AOT兼容性分析.md) |

## 主要功能

<details>
<summary><b>核心引擎</b> (7)</summary>

| 功能 | 關鍵特性 |
| --- | --- |
| 大規模實戰驗證 | 生產環境 200+ 個 Redis 執行個體，單日尖峰 1 億+ 次業務物件寫入、800 億+ 次命令呼叫。 |
| 低延遲 | 單次 Get/Set 往返含網路約 200–600µs。 |
| 高吞吐 | 內建連線池（最高 10 萬併發），搭配同步 RESP 解析器與可選的自動管線批次。 |
| 自動重試與多地址容錯移轉 | 以逗號分隔的節點清單，網路錯誤時快速容錯移轉，預設 10 秒遮蔽視窗。 |
| 可插拔編碼器 | 預設 JSON（`RedisJsonEncoder`），可透過 `IPacketEncoder` 擴充為二進位序列化。 |
| APM 追蹤 | `ITracer` 分散式追蹤搭配 `PerfCounter` 效能計數器。 |
| 零重磅相依 | 除 BCL 外，僅相依 NewLife 生態（日誌、設定、序列化、安全性）。 |

</details>

<details>
<summary><b>資料結構</b> (10)</summary>

| 結構 | 關鍵特性 |
| --- | --- |
| String KV + 批次 | MGET / MSET / INCR / APPEND / GETRANGE 等。 |
| List | LPUSH / RPOP / LRANGE / LMOVE。 |
| Hash | HGET / HSET / HMGET / HSCAN。 |
| Set | SADD / SMEMBERS / SUNION / SDIFF。 |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN。 |
| Stack | 建構於 List 之上的 LIFO 包裝。 |
| Stream | 完整 XADD / XREAD / XGROUP / XACK。 |
| Geo | GEOADD / GEODIST / GEOSEARCH。 |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE。 |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD（規劃中）。 |

</details>

<details>
<summary><b>訊息佇列與發佈／訂閱</b> (7)</summary>

| 類型 | 關鍵特性 |
| --- | --- |
| `RedisQueue<T>` | 簡單佇列，最多一次（at-most-once）投遞。 |
| `RedisReliableQueue<T>` | 至少一次（at-least-once）投遞，RPOPLPUSH + Ack 回滾。 |
| `RedisDelayQueue<T>` | 以 Sorted Set 實作的延遲投遞。 |
| `RedisStream<T>` | 有序串流，支援消費者群組與 XACK。 |
| `MultipleConsumerGroupsQueue<T>` | 廣播至多個消費者群組。 |
| `PubSub` + `RedisEventBus` | 即時發佈／訂閱。 |
| RedLock 分散式鎖 | 多執行個體 Redlock 演算法（進行中）。 |

</details>

<details>
<summary><b>叢集與高可用性</b> (7)</summary>

| 模式 | 關鍵特性 |
| --- | --- |
| 單機 | 直連單一執行個體，資源佔用最小。 |
| 主從複寫 | 讀寫分離與容錯移轉。 |
| Sentinel | Sentinel 探索並自動切換主節點。 |
| 原生叢集 | CRC16 槽位路由，支援 MOVED / ASK 重新導向。 |
| kvrocks | 自動適配 kvrocks 儲存引擎。 |
| Garnet | 自動偵測 Microsoft Garnet，缺少功能時優雅降級。 |
| Valkey | 由伺服器 INFO 偵測。 |

</details>

<details>
<summary><b>協定與伺服器相容性</b> (7)</summary>

| 能力 | 關鍵特性 |
| --- | --- |
| RESP2 | 簡單字串、錯誤、整數、大量字串與陣列完整實作。 |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error，透過 HELLO 握手協商（Redis 6.0+）。 |
| Redis 2.8 – 7.x | 各版本的主流命令，含 6.2 起的 COPY / GETDEL / GETEX / GEOSEARCH。 |
| ACL 驗證 | Redis 6.0+ 使用者名稱加密碼驗證。 |
| 交易 | MULTI / EXEC / WATCH。 |
| Lua 指令碼 | EVAL / EVALSHA。 |
| Redis Functions | FCALL / FUNCTION（規劃中）。 |

</details>

<details>
<summary><b>雲端、安全性與可觀測性</b> (8)</summary>

| 能力 | 關鍵特性 |
| --- | --- |
| 阿里雲 KVStore / Tair | 標準版、ACL、SSL 與代理模式指引，以及 Tair 結構（TairString / TairHash / TairZSet）。 |
| 騰訊雲 Redis | 以執行個體 ID 作為使用者名稱（`instanceId:password`）。 |
| 華為雲 DCS | 主備、SSL 與叢集版差異（叢集模式不支援 SELECT）。 |
| TLS/SSL | 可設定協定版本，並支援用戶端憑證雙向 TLS。 |
| 結構化日誌 | 元件日誌（`Log`）與協定層日誌（`ClientLog`）。 |
| 效能計數器 | `PerfCounter` 回報每秒操作數、延遲與錯誤率。 |
| APM 追蹤 | 全面 `ITracer` span，並內建 `ActivitySource`（net8.0+）供 OpenTelemetry 使用。 |
| 連線字串保護 | `ProtectedKey` 解密內嵌於連線字串的密碼。 |

</details>

<details>
<summary><b>平台與整合</b> (5)</summary>

| 能力 | 關鍵特性 |
| --- | --- |
| 多目標 | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0；擴充套件涵蓋 netcoreapp3.1 → net10。 |
| 強式命名 | 以 `newlife.snk` 簽章，組件可重建替換。 |
| NativeAOT | `IsAotCompatible` 靜態檢查、`AotSmoke/` 專案與多 RID CI 矩陣。 |
| ASP.NET Core 套件 | `NewLife.Redis.Extensions` 透過 `AddRedisCaching` 註冊 `IDistributedCache` 與 `FullRedis`。 |
| DataProtection | `PersistKeysToRedis` 將資料保護金鑰保存至 Redis。 |

</details>

## 生態版圖

NewLife 是按相依關係分層的生態系，各專案獨立版本、獨立發行。下方版圖在每個 NewLife 儲存庫中逐字一致；每個專案連結到其儲存庫與主 NuGet 套件。

| 專案 | 一句話定位 | 下載量 |
| --- | --- | --- |
| **基礎底座** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 日誌、設定、快取、網路、序列化、排程與 APM 追蹤。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 資料中介層：單表百億級、自動分表、讀寫分離，支援 10 多種資料庫。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高效能網路庫：單機 177 萬 pps、14.5 Gbps（TCP 1KB，2026-10 實測），400 萬 Tcp 長連線。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 協定通訊與 RPC 框架，面向 CS 應用程式與物聯網裝置。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 將 .NET 應用程式安裝為作業系統常駐服務：Windows 服務與 Linux systemd。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` 專案範本：主控台、Web、API、服務、資料與物聯網。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **資料儲存與訊息** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 用戶端：微秒級延遲、百萬級吞吐，內建訊息佇列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 純受控 RocketMQ 用戶端，支援 Apache RocketMQ 與阿里雲訊息佇列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 用戶端與服務端，含阿里雲物聯網接入。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 混合式資料庫：內嵌或網路雙模，關係型、時序、訊息佇列與 KV 於一體。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | 建構於 NewLife 資料堆疊之上的 MySql 驅動與輔助層。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web、AI 與分散式** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 快速後台開發框架：權限、SSO、OAuth 服務端與 CRUD 樣板。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 分散式服務平台：節點管理、APM、設定中心、註冊中心與發佈中心。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 分散式任務與即時資料排程平台，支援分片、重試與回溯補算。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 統一接入 46 家大模型服務商，附帶開箱即用的 ChatAI Web 對話應用。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **工業與物聯網** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 物聯網標準庫，定義通訊協定契約與裝置抽象。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 建構於 NewLife.IoT 之上的 Modbus TCP/RTU/ASCII 實作。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 建構於 NewLife.IoT 之上的西門子 PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | 建構於 NewLife.IoT 之上的歐姆龍 PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | 建構於 NewLife.IoT 之上的三菱 Melsec PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | 工業資料存取的 OPC 用戶端。 | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 樓宇自動化協定堆疊。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 開源輕量級物聯網平台，含閘道與裝置管理。 | — |
| **車聯網** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 車輛定位協定：70+ 訊息體，涵蓋 2011/2013/2019 三個版本。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 部標平台互聯協定，面向運輸管理部門。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 車載視訊：RTP 解析、FLV/fMP4 轉封裝與直播推送。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **領域元件** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | 一個套件涵蓋 Excel、Word、PPT、PDF 等 15+ 文件格式。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 跨平台音訊：編解碼、裝置 IO、DSP、容器與串流處理。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 視訊編解碼、封裝格式、影格處理與串流。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度／高德／騰訊／天地圖統一介面，支援地理編碼與座標轉換。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 歸屬地：離線記憶體映射資料庫 + 線上 API 雙來源。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **工具與產品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 開發者工具箱：網路、序列埠、加解密、正規表示式、Modbus 與 MQTT。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 自架 Git 原始碼管理系統。 | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 從較舊的宿主啟動時，自動安裝相符的 .NET 執行階段。 | — |

> 徽章取各專案主套件；主題套件、擴充套件與衛星套件見對應 NuGet 頁面。未在表中列出的 30 多個套件（FTP、LoRa、Office 擴充、各類協定驅動等）見 [NewLifeX 組織](https://github.com/NewLifeX) 與 [newlifex.com](https://newlifex.com)。

## 文件

**NewLife.Redis** 為 .NET 開發人員提供全生命週期的 Redis 存取能力：

- **完整協定**：實作 RESP2，涵蓋 Redis 2.8 至 7.x 的所有主流命令，支援全部資料結構：String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog。
- **訊息佇列**：內建簡單佇列、**可靠佇列（RPOPLPUSH + Ack 語意）**、延遲佇列與 Stream 多消費者群組佇列——開箱即用，無需自行造輪子。
- **叢集與高可用性**：支援 Standalone / Master-Replica / Sentinel / Native Cluster，具備自動容錯移轉。
- **雲端廠商介面卡**：針對 **阿里雲 KVStore / 騰訊雲 Redis / 華為雲 DCS** 提供專屬設定與適配，涵蓋代理模式、ACL 驗證、SSL 加密、特殊驗證格式等差異。
- **相容實作**：自動偵測 Microsoft Garnet、kvrocks 等 Redis 相容伺服器，功能不支援時優雅降級並提供明確替代方案。
- **廣泛框架支援**：單一套件涵蓋 **.NET Framework 4.5 至 .NET 10+**，適合工控、政府、物聯網等既有系統場景。

### 指南

- [需求文件（中文）](Doc/需求文档.md)：願景、目標與功能需求
- [架構文件（中文）](Doc/架构文档.md)：分層、元件與關鍵設計決策
- [功能清單（中文）](Doc/功能清单.md)：各功能的實作、測試與註解狀態
- [競品分析（中文）](Doc/竞品分析.md)：與主流 Redis 用戶端的比較
- [Garnet 相容性（中文）](Doc/Garnet兼容性.md)：支援範圍與優雅降級
- [AOT 相容性（中文）](Doc/AOT兼容性分析.md)：NativeAOT 範圍與實測結論
- [多語言本地化（中文）](Doc/多语言本地化.md)：例外與日誌的訊息模型
- [RedisSwitch 整合（中文）](Doc/RedisSwitch项目说明.md)：零停機 Redis 切換工具
- [GC 配置分析（中文）](Doc/dotMemory分析Redis的GC分配.md)：dotMemory 實測
- [官方網站](https://newlifex.com/core/redis)：元件頁面與線上文件

### 協定與版本支援

#### RESP 協定支援

| 協定 | 狀態 | 說明 |
|----------|--------|-------------|
| RESP2 | ✅ 完整支援 | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ 完整支援 | Map / Set / Double / Boolean / Null / Blob Error 型別，透過 HELLO 握手協商（Redis 6.0+） |

#### Redis 版本功能支援

| Redis 版本 | 主要功能 | 狀態 |
|---------------|--------------|--------|
| 2.8 | 基本命令、SCAN、HyperLogLog、Pub/Sub | ✅ 完整支援 |
| 3.0 | 原生叢集、CRC16 分片路由 | ✅ 完整支援 |
| 3.2 | GEO 空間資料 | ✅ 完整支援 |
| 4.0 | UNLINK 非同步刪除、MODULE | 🔄 部分支援 |
| 5.0 | Stream 資料結構（完整 XADD/XREAD/XGROUP/XACK） | ✅ 完整支援 |
| 6.0 | ACL 存取控制、TLS/SSL、LMOVE、RESET | ✅ 完整支援 |
| 6.2 | COPY、GETDEL、GETEX、GEOSEARCH | ✅ 核心命令支援 |
| 7.0 | Functions（FCALL）、LMPOP/ZMPOP、分片 Pub/Sub | 🔄 規劃中 |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 規劃中 |

### 架構

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

關鍵類別說明：

| 類別 | 角色 |
|-------|------|
| `Redis` | 核心用戶端：連線池、多節點容錯移轉、管線、組態管理 |
| `FullRedis` | 增強用戶端：叢集路由、鍵前綴、資料結構工廠方法 |
| `RedisClient` | RESP 協定單元：TCP 連線、命令編碼／解碼（內部使用，由集區管理） |
| `RedisList<T>` 等 | 泛型資料結構包裝，搭配統一編碼策略 |
| `RedisReliableQueue<T>` | 可靠訊息佇列（RPOPLPUSH + Ack 確認） |
| `RedisStream<T>` | Stream 佇列（Redis 5.0+，消費者群組模式） |
| `IPacketEncoder` | 可插拔的編碼／解碼策略 |

### 使用範例

以下每個範例都執行於 [快速開始](#快速開始) 中建立的實際 `FullRedis` 執行個體。

<details>
<summary><b>基本操作</b></summary>

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
<summary><b>資料結構</b></summary>

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

> 注意：基礎 `Redis` 執行個體僅支援 KV 操作。進階集合請使用 `FullRedis`。

</details>

<details>
<summary><b>管線與自動批次</b></summary>

管線可顯著減少網路往返（RTT）並提升批次操作吞吐：

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**自動模式**（生產環境建議）：

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>訊息佇列</b></summary>

#### 簡單佇列

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### 可靠佇列（至少一次語意）

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

#### 延遲佇列

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### Stream 佇列（Redis 5.0+，多消費者群組）

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
<summary><b>發佈／訂閱</b></summary>

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
<summary><b>叢集與高可用性</b></summary>

#### 多地址自動容錯移轉

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### 自動偵測叢集模式

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
<summary><b>雲端廠商介面卡</b></summary>

#### 阿里雲 KVStore / Tair

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

#### 騰訊雲 Redis

```csharp
// Tencent Cloud standard/cluster edition: use instance ID as UserName (AUTH instanceId:password format)
var rds = new FullRedis("xxxxxx.redis.tencentcds.com:6379", "instanceId", "YourPassword", 0);

// Or via connection string
var rds = FullRedis.Create("server=xxxxxx.redis.tencentcds.com:6379;username=instanceId;password=YourPassword;db=0");
```

#### 華為雲 DCS

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

**雲端廠商通用注意事項：**
- 公有雲代理模式：保持 `AutoDetect = false`（預設），避免自動偵測到內部節點
- 連線配額：各雲端廠商與執行個體規格皆有連線上限，請據此設定連線池的 `Max` 參數
- TCP keepalive：華為雲預設 300 秒 TCP 閒置逾時；請將 `Timeout` 設在該值以下，並定期 PING

</details>

<details>
<summary><b>序列化與編碼器</b></summary>

預設編碼器：`RedisJsonEncoder`（內建 JSON host），開箱即用。

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

基本型別（`String` / `Int32` / `Int64` / `Double` / `Boolean`）不經過 JSON，直接序列化以獲得最佳效能。

</details>

<details>
<summary><b>安全性與驗證</b></summary>

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
<summary><b>可觀測性</b></summary>

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
<summary><b>擴充套件</b></summary>

`NewLife.Redis.Extensions` 提供深入的 ASP.NET Core 整合：

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
<summary><b>Garnet 相容性</b></summary>

NewLife.Redis 完整支援 Microsoft [Garnet](https://github.com/microsoft/garnet) 高效能 Redis 相容伺服器，具備自動偵測與優雅降級能力。

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| 功能 | Garnet 支援狀態 |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ 完整支援 |
| HyperLogLog / Geo | ✅ 完整支援 |
| Pub/Sub / 交易 / Lua 指令碼 | ✅ 完整支援 |
| 管線 | ✅ 完整支援 |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ 完整支援 |
| `RedisReliableQueue<T>` | ✅ 完整支援 |
| `RedisStream<T>` | ❌ Garnet 尚不支援 Stream；會自動拋出 `NotSupportedException` 並建議替代方案 |

參見：[Garnet 相容性文件（中文）](Doc/Garnet兼容性.md)

</details>

### 參考資料

<details>
<summary><b>功能完成狀態</b></summary>

> 圖例：✅ 已完成 | 🔄 規劃中 | ❌ 明確不規劃

#### 資料結構

| 結構 | 狀態 | 說明 |
|-----------|--------|-------------|
| String KV + 批次 | ✅ | MGET/MSET/INCR/APPEND/GETRANGE 等 |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE 等 |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN 等 |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF 等 |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN 等 |
| Stream | ✅ | 完整 XADD/XREAD/XGROUP/XACK |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH 等 |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

#### 訊息佇列

| 佇列類型 | 狀態 | 投遞語意 |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | 最多一次 |
| `RedisReliableQueue<T>` | ✅ | 至少一次（RPOPLPUSH + Ack） |
| `RedisDelayQueue<T>` | ✅ | 延遲投遞（Sorted Set） |
| `RedisStream<T>` | ✅ | 有序 + 消費者群組 + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | 多消費者群組廣播 |
| `PubSub` + `RedisEventBus` | ✅ | 即時推送 |
| RedLock 分散式鎖 | 🔄 | 多執行個體 Redlock 演算法 |

#### 叢集支援

| 模式 | 狀態 |
|------|--------|
| 單機 | ✅ |
| 主從複寫 | ✅ |
| Sentinel | ✅ |
| 原生叢集 | ✅ |
| kvrocks 自動適配 | ✅ |
| Garnet 自動偵測 | ✅ |
| Valkey 自動偵測 | ✅ |

#### 協定與進階功能

| 功能 | 狀態 |
|---------|--------|
| RESP2 完整支援 | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| ACL 驗證 | ✅ |
| 交易 MULTI/EXEC/WATCH | ✅ |
| Lua 指令碼 EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 規劃中 |
| 管線（顯式 + 自動） | ✅ |
| 模式訂閱 PSUBSCRIBE | 🔄 |
| UNLINK 非同步刪除 | 🔄 |

</details>

<details>
<summary><b>效能基準測試</b></summary>

原始碼內建基準測試（`Samples/Benchmark`），典型結果（40 個邏輯處理器、本機 Redis 7.0）：

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

手動觸發基準測試：

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> 實際效能受網路 RTT／Value 大小／序列化複雜度影響。建議將單一 Value 控制在 1–1.4KB 之間。

</details>

<details>
<summary><b>最佳實務</b></summary>

- **單例重用**：`FullRedis` 內建連線池，強烈建議整個應用程式生命週期共用單一執行個體，不要每次都 `new`
- **鍵設計**：以 `:` 作為層級分隔，例如 `user:1001:profile`；生產環境用 `Prefix` 做命名空間隔離
- **批次優先**：優先使用 `GetAll/SetAll`，避免迴圈逐鍵操作；以 `AutoPipeline` 自動合併寫入
- **Value 大小**：控制在 1–2KB 以內；大物件拆分／壓縮／改用 Hash 做欄位級儲存
- **可靠消費**：生產環境請用 `RedisReliableQueue<T>` 而非簡單 List，避免訊息遺失
- **雲端廠商**：公有雲保持 `AutoDetect = false`，避免內部節點位址問題
- **監控**：生產環境啟用 `Counter` 統計；`ClientLog` 僅在除錯時開啟，避免熱路徑日誌開銷
- **SSL**：公有雲與生產環境請啟用 TLS 加密傳輸

</details>

<details>
<summary><b>常見問題</b></summary>

**Q：是否支援 Redis Cluster 分片？**
A：支援。`FullRedis` 在 `AutoDetect=true` 時會自動偵測叢集模式，CRC16 路由與 MOVED/ASK 重新導向皆已實作。公有雲代理模式請維持預設 `AutoDetect=false`。

**Q：與 StackExchange.Redis 相比如何？**
A：NewLife.Redis 採用連線池模型（StackExchange 為多路復用），連線行為更直觀；內建訊息佇列包裝（可靠佇列／延遲佇列）是關鍵差異；同時支援 .NET Framework 4.5。詳見 [競品分析（中文）](Doc/竞品分析.md)。

**Q：如何處理大 Value 造成的慢查詢？**
A：拆分資料結構（例如依欄位拆成 Hash）＋批次讀寫＋二進位編碼器＋控制 `MaxMessageSize`（預設 1MB 上限）。

**Q：連線池耗盡怎麼辦？**
A：預設上限 10 萬連線，正常業務不會耗盡。連線池可透過 `Redis.PoolConfig`（`RedisPoolConfig`）設定：最小閒置／最大容量／閒置清理時間／最大生命週期／借出等待逾時。閒置超過 `IdleTime` 的連線在借出時會主動 PING 檢查；主從切換或代理滾動更新場景可調低 `MaxLifetime` 以加快連線回收。若出現等待逾時，請檢查連線洩漏（未 `Dispose`）或長時間佔用連線的操作。

**Q：阿里雲／騰訊雲連線失敗怎麼排查？**
A：依序檢查：① `AutoDetect` 是否設為 `false`（正確預設）？② 驗證格式是否正確（騰訊雲需 `username=instanceId`）？③ 網路安全群組／白名單是否放行用戶端 IP？④ 叢集版是否禁止以 SELECT 切換資料庫？

**Q：Garnet 支援全部功能嗎？**
A：基本操作、List/Hash/Set/ZSet/HLL/Geo 與佇列皆支援。Stream 暫不支援，會自動拋出 `NotSupportedException`，並建議改用 `RedisQueue<T>` 作為替代。

</details>

<details>
<summary><b>發展藍圖</b></summary>

- [x] ~~RESP3 協定支援~~（已完成）
- [ ] Redis Functions（FCALL/FUNCTION，取代 Lua 指令碼）
- [ ] LMPOP / ZMPOP（Redis 7.0 批次彈出）
- [ ] RedLock 多執行個體分散式鎖
- [ ] Bitmap / Bitfield 操作
- [ ] 模式訂閱 PSUBSCRIBE 完整實作
- [ ] UNLINK 非同步刪除
- [ ] Prometheus 指標匯出介面卡
- [x] 阿里雲 Tair 擴充（支援 TairString / TairHash / TairZSet）
- [ ] 華為雲 DCS 叢集版節點位址映射介面卡
- [x] ~~Pub/Sub~~（已完成）
- [x] ~~RedisStream 多消費者群組~~（已完成）
- [x] ~~Garnet 伺服器類型偵測~~（已完成）

歡迎透過 Issue / PR 提供意見反應與功能需求。

</details>

<details>
<summary><b>競品比較</b></summary>

| 能力 | NewLife.Redis | StackExchange.Redis | FreeRedis | 備註 |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | 工控／政府既有系統的關鍵需求 |
| 連線模型 | 連線池 | 多路復用 | 連線池 | 各有適用場景 |
| 命令涵蓋率 | ~98% | ~95% | ~98% | 除明確不規劃者外，所有已記載命令 |
| 內建訊息佇列包裝 | ✅ 完整 | ❌ | 🔄 部分 | 獨有的可靠佇列／延遲佇列 |
| 內建序列化 | ✅ | ❌ 手動 | ❌ 手動 | 減少樣板程式碼 |
| 雲端廠商專屬介面卡 | ✅（阿里／騰訊／華為） | 通用 | 通用 | 專屬文件 |
| Garnet 專屬介面卡 | ✅ | ❌ | ❌ | 類型偵測 + 優雅降級 |
| kvrocks 介面卡 | ✅ | ❌ | ❌ | 自動偵測 |
| Valkey 自動偵測 | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| APM 追蹤 | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | 原生 OpenTelemetry 整合 |
| RESP3 | ✅ | ✅ | ✅ | 已透過 RESP3 測試套件驗證 |

詳見 [競品分析文件（中文）](Doc/竞品分析.md)。

</details>

### 發行說明

- [ChangeLog.md](ChangeLog.md)：版本歷程（中文）
- [ChangeLog.en.md](ChangeLog.en.md)：版本歷程（英文）

## 參與貢獻

### 建置與測試

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- 整合測試需要可用的 Redis：設定 `REDIS_CONNECTION` 或在 `127.0.0.1:6379` 啟動一個；否則標記 `[RedisFact]` / `[RedisTopologyFact]` 的測試會自動略過。
- NativeAOT 冒煙測試：`AotSmoke/`，CI 見 `.github/workflows/aot.yml`。
- 效能基準測試：`Samples/Benchmark`。

### 儲存庫結構

| 目錄 | 說明 |
| --- | --- |
| `NewLife.Redis/` | 用戶端原始碼：連線池、RESP 編解碼器、資料結構與佇列 |
| `NewLife.Redis.Extensions/` | ASP.NET Core 整合：`IDistributedCache`、DI 與資料保護 |
| `NewLife.Extensions.Caching.Redis/` | 舊版快取擴充相容套件 |
| `XUnitTest/` | xUnit 測試套件（net10.0） |
| `AotSmoke/` | NativeAOT 冒煙測試專案 |
| `Samples/` | Benchmark、WebDemo、QueueDemo、RedisSwitch 與行動裝置冒煙範例 |
| `Doc/` | 需求、架構、功能清單與專題文件 |

### 發行

推送以 `v` 開頭的標籤（例如 `v6.7.2026.1024`）即可觸發 `.github/workflows/publish.yml`。此工作流程會封裝 `NewLife.Redis` 與 `NewLife.Redis.Extensions`，發佈至 NuGet.org 與 GitHub Packages，並依 `ChangeLog.md` 對應版本段落建立 GitHub Release；手動執行僅發佈套件。

### 意見反應

1. 提交前請確認 `dotnet build` 與相關測試通過，並遵循儲存庫的程式碼風格與提交慣例
2. PR：保持變更最小，補充必要註解與測試說明
3. Issue：提供版本、執行環境與最小重現案例

社群：QQ 群 1600800 / 1600838；GitHub Discussions / Issues。

## 新生命開發團隊

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

新生命團隊成立於 2002 年，二十餘年專注 .NET 與物聯網技術生態，主導開源專案近百個（生態版圖收錄 34 個主幹專案），為電力、物流、醫療、工控、網際網路等行業提供軟硬體一體化解決方案；以「學無先後達者為師」為信條。

開源元件在 NuGet 累計下載 <!--nuget-total:start-->6.48M<!--nuget-total:end--> 次（全生態口徑，截至 2026-10）。

網站：https://newlifex.com · 開源組織：https://github.com/NewLifeX

<details>
<summary>微信公眾號：智能大石头（掃碼關注）</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## 授權條款

MIT 授權條款。可自由用於商業用途、修改與再發佈（無需額外授權），僅需保留著作權聲明。

> 若本文件未涵蓋您的使用情境，歡迎提交 Issue，讓我們一起完善文件！
