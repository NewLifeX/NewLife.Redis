# NewLife.Redis — 高性能 Redis クライアントコンポーネント

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` は、NewLife チームが開発した **高性能・高スループット・導入しやすい** .NET Redis クライアントコンポーネントです。2017 年以来、複数の大規模本番環境で数十億件のレコードと高い同時実行性を安定して処理し、**1 日 800 億回以上**のコマンド呼び出しを処理しています。

> 📖 完全なドキュメント: [要件定義（中国語）](Doc/需求文档.md) | [アーキテクチャ（中国語）](Doc/架构文档.md) | [機能一覧（中国語）](Doc/功能清单.md) | [競合分析（中国語）](Doc/竞品分析.md) | [Garnet 互換性（中国語）](Doc/Garnet兼容性.md) | [AOT 互換性（中国語）](Doc/AOT兼容性分析.md)

**目次**: [クイックスタート](#クイックスタート) · [主な機能](#主な機能) · [エコシステムマップ](#エコシステムマップ) · [ドキュメント](#ドキュメント) · [コントリビューション](#コントリビューション) · [チーム](#チーム) · [ライセンス](#ライセンス)

## クイックスタート

### インストール

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### 基本的な使い方

**推奨: シングルトンとして生成し、スレッドセーフなのでアプリケーションのライフサイクル全体で再利用します:**

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

**接続文字列モード（構成センターでの利用に推奨）:**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### プラットフォーム対応

| 項目 | 対応範囲 |
| --- | --- |
| ターゲットフレームワーク | 1 つのパッケージで net45、net461、netstandard2.0、netstandard2.1、net8.0 に対応。拡張パッケージは netcoreapp3.1 → net10 を追加 |
| OS とアーキテクチャ | Windows、Linux、macOS、Android、iOS。x64、arm64、loongarch64、riscv64、s390x |
| NativeAOT | `net8.0` は `IsAotCompatible` チェック付きで提供。`AotSmoke/` と CI スモークマトリクスが win-x64 / linux-x64 / linux-arm64 / linux-la64（LA64 は QEMU 経由）をカバーし、win-arm64、macOS、musl RID はネイティブでコンパイル。linux-x64 がブロッキングゲートで、その他は監視対象 — 詳細は [AOT 互換性](Doc/AOT兼容性分析.md) |

## 主な機能

<details>
<summary><b>コアエンジン</b> (7)</summary>

| 機能 | 主な特長 |
| --- | --- |
| 大規模運用での実績 | 本番環境で 200 以上の Redis インスタンスを運用。1 日のピークで 1 億件以上の業務オブジェクト書き込みと 800 億回以上のコマンド呼び出しを処理。 |
| 低レイテンシ | Get/Set の 1 往復にかかる時間はネットワーク込みで 200〜600µs。 |
| 高スループット | 組み込み接続プール（最大 10 万同時接続）、同期 RESP パーサー、任意の自動パイプラインバッチ処理。 |
| 自動リトライとマルチアドレスフェイルオーバー | カンマ区切りのノード一覧、ネットワークエラー時の高速フェイルオーバー、デフォルト 10 秒の遮蔽ウィンドウ。 |
| 差し替え可能なエンコーダー | デフォルトは JSON（`RedisJsonEncoder`）。`IPacketEncoder` によりバイナリシリアライズへ拡張可能。 |
| APM トレース | `ITracer` による分散トレースと `PerfCounter` パフォーマンスカウンター。 |
| 重い依存関係ゼロ | BCL の上に NewLife エコシステム（ログ、構成、シリアライズ、セキュリティ）のみ。 |

</details>

<details>
<summary><b>データ構造</b> (10)</summary>

| 構造 | 主な特長 |
| --- | --- |
| String KV + バッチ | MGET / MSET / INCR / APPEND / GETRANGE など。 |
| List | LPUSH / RPOP / LRANGE / LMOVE。 |
| Hash | HGET / HSET / HMGET / HSCAN。 |
| Set | SADD / SMEMBERS / SUNION / SDIFF。 |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN。 |
| Stack | List 上の LIFO ラッパー。 |
| Stream | XADD / XREAD / XGROUP / XACK を完全サポート。 |
| Geo | GEOADD / GEODIST / GEOSEARCH。 |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE。 |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD（予定）。 |

</details>

<details>
<summary><b>メッセージキューと Pub/Sub</b> (7)</summary>

| 種類 | 主な特長 |
| --- | --- |
| `RedisQueue<T>` | 最大 1 回配信のシンプルなキュー。 |
| `RedisReliableQueue<T>` | RPOPLPUSH + Ack ロールバックによる少なくとも 1 回配信。 |
| `RedisDelayQueue<T>` | Sorted Set を利用した遅延配信。 |
| `RedisStream<T>` | コンシューマーグループと XACK を備えた順序付きストリーム。 |
| `MultipleConsumerGroupsQueue<T>` | 複数のコンシューマーグループへのブロードキャスト。 |
| `PubSub` + `RedisEventBus` | リアルタイムのパブリッシュ/サブスクライブ。 |
| RedLock 分散ロック | マルチインスタンスの Redlock アルゴリズム（進行中）。 |

</details>

<details>
<summary><b>クラスタリングと高可用性</b> (7)</summary>

| モード | 主な特長 |
| --- | --- |
| Standalone | 単一インスタンスへの直接接続で、フットプリント最小。 |
| Master-Replica レプリケーション | 読み書き分離とフェイルオーバー。 |
| Sentinel | Sentinel ディスカバリと自動マスター切り替え。 |
| Native Cluster | CRC16 スロットルーティングと MOVED / ASK リダイレクト。 |
| kvrocks | kvrocks ストレージエンジンへ自動適応。 |
| Garnet | Microsoft Garnet を自動検出し、機能が欠ける場合はグレースフルデグラデーション。 |
| Valkey | サーバーの INFO から検出。 |

</details>

<details>
<summary><b>プロトコルとサーバー互換性</b> (7)</summary>

| 機能 | 主な特長 |
| --- | --- |
| RESP2 | Simple String、Error、Integer、Bulk String、Array を完全実装。 |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error に対応し、HELLO ハンドシェイクでネゴシエーション（Redis 6.0+）。 |
| Redis 2.8 – 7.x | 各バージョンの主要コマンドに対応。6.2 以降の COPY / GETDEL / GETEX / GEOSEARCH を含む。 |
| ACL 認証 | Redis 6.0+ のユーザー名 + パスワード認証。 |
| トランザクション | MULTI / EXEC / WATCH。 |
| Lua スクリプト | EVAL / EVALSHA。 |
| Redis Functions | FCALL / FUNCTION（予定）。 |

</details>

<details>
<summary><b>クラウド・セキュリティ・可観測性</b> (8)</summary>

| 機能 | 主な特長 |
| --- | --- |
| Alibaba Cloud KVStore / Tair | スタンダード版、ACL、SSL、プロキシモードの指針に加え、Tair 構造（TairString / TairHash / TairZSet）に対応。 |
| Tencent Cloud Redis | インスタンス ID をユーザー名として使用（`instanceId:password`）。 |
| Huawei Cloud DCS | 主備、SSL、クラスタ版の差異（クラスタモードでは SELECT 不可）。 |
| TLS/SSL | プロトコルバージョンの設定とクライアント証明書による相互 TLS。 |
| 構造化ログ | コンポーネントログ（`Log`）とプロトコルレベルログ（`ClientLog`）。 |
| パフォーマンスカウンター | `PerfCounter` が ops/s、レイテンシ、エラー率を報告。 |
| APM トレース | 全体に `ITracer` スパンを張り、`ActivitySource`（net8.0+）で OpenTelemetry に対応。 |
| 接続文字列の保護 | `ProtectedKey` が接続文字列に埋め込まれたパスワードを復号。 |

</details>

<details>
<summary><b>プラットフォームと統合</b> (5)</summary>

| 機能 | 主な特長 |
| --- | --- |
| マルチターゲット | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0。拡張パッケージは netcoreapp3.1 → net10 をカバー。 |
| 厳密名 | `newlife.snk` で署名され、アセンブリの再ビルドと差し替えが可能。 |
| NativeAOT | `IsAotCompatible` による静的チェック、`AotSmoke/` プロジェクト、マルチ RID の CI マトリクス。 |
| ASP.NET Core パッケージ | `NewLife.Redis.Extensions` が `AddRedisCaching` を通じて `IDistributedCache` と `FullRedis` を登録。 |
| DataProtection | `PersistKeysToRedis` がデータ保護キーを Redis に永続化。 |

</details>

## エコシステムマップ

NewLife は、それぞれ独立してバージョン管理されるオープンソースプロジェクトによる階層型エコシステムです。以下のマップはすべての NewLife リポジトリでそのまま共通掲載されており、各プロジェクトは自身のリポジトリと主要な NuGet パッケージにリンクしています。

| プロジェクト | 役割 | ダウンロード数 |
| --- | --- | --- |
| **基盤** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | ログ、構成、キャッシュ、ネットワーク、シリアライズ、スケジューリング、APM トレーシング。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | データミドルウェア：単一テーブルで数百億件級、自動シャーディング、読み書き分離、10 種類以上のデータベースに対応。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高性能ネットワークライブラリ：単一マシンで 177 万 pps、14.5 Gbps（TCP 1KB、2026-10 実測）、400 万 Tcp 長接続。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | CS アプリケーションと IoT デバイス向けのプロトコル通信と RPC フレームワーク。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | .NET アプリケーションを OS デーモンとしてインストール：Windows サービスと Linux systemd。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | コンソール、Web、API、サービス、データ、IoT 向けの `dotnet new` プロジェクトテンプレート。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **データとメッセージング** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | マイクロ秒級の遅延、数百万 ops/s のスループット、メッセージキュー内蔵の Redis クライアント。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Apache RocketMQ と Alibaba Cloud Message Queue に対応した純マネージド RocketMQ クライアント。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT クライアントとサーバー。Alibaba Cloud IoT への接続を含みます。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 製ハイブリッドデータベース：組み込み/ネットワークの両モード、リレーショナル、時系列、メッセージキュー、KV を 1 つに。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | NewLife データスタック上に構築された MySql ドライバーとヘルパー層。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web、AI、分散** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 高速管理プラットフォームフレームワーク：権限、SSO、OAuth サーバー、CRUD スキャフォールディング。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 分散サービスプラットフォーム：ノード管理、APM、構成センター、登録センター、リリースセンター。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | シャーディング、リトライ、バックフィルに対応した分散タスクとリアルタイムデータのスケジューリングプラットフォーム。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 46 社の LLM プロバイダーに統一接続するクライアントと、すぐに使える Web チャットアプリ ChatAI。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **産業と IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | プロトコル契約とデバイス抽象を定義する IoT 標準ライブラリ。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | NewLife.IoT 上に実装された Modbus TCP/RTU/ASCII。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | NewLife.IoT 上に実装されたシーメンス PLC プロトコル。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | NewLife.IoT 上に実装されたオムロン PLC プロトコル。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | NewLife.IoT 上に実装された三菱 Melsec PLC プロトコル。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | 産業データアクセス向けの OPC クライアント。 | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet ビル管理プロトコルスタック。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | ゲートウェイとデバイス管理を備えた軽量なオープンソース IoT プラットフォーム。 | — |
| **車載テレマティクス** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 車両位置情報プロトコル：70 以上のメッセージボディ、2011/2013/2019 に対応。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | 運輸管理部門向けの JT/T 809 プラットフォーム間プロトコル。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 車載ビデオ：RTP 解析、FLV/fMP4 リマックス、ライブ配信。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **ドメインコンポーネント** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Excel、Word、PPT、PDF など 15 以上のドキュメント形式を 1 パッケージで。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | クロスプラットフォーム音声：コーデック、デバイス IO、DSP、コンテナ、ストリーミング。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | ビデオコーデック、コンテナ形式、フレーム処理、ストリーミング。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度地図、高徳地図、テンセント地図、天地図を統一インターフェイス化。ジオコーディングと座標変換に対応。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 位置情報：オフラインのメモリマップデータベースとオンライン API のフォールバック。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **ツールと製品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 開発者向けツールボックス：ネットワーク、シリアルポート、暗号化、正規表現、Modbus、MQTT。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | セルフホスト型 Git ソースコード管理システム。 | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 古いホストから起動した際に対応する .NET ランタイムを自動インストールします。 | — |

> バッジは各プロジェクトの主要パッケージを示します。テーマ、拡張、サテライトパッケージは各 NuGet ページを参照してください。ここに記載していない 30 以上のパッケージ（FTP、LoRa、Office 拡張、各種プロトコルドライバーなど）は [NewLifeX 組織](https://github.com/NewLifeX) と [newlifex.com](https://newlifex.com) で公開しています。

## ドキュメント

**NewLife.Redis** は .NET 開発者に Redis アクセスの全ライフサイクルを提供します:

- **完全なプロトコル**: RESP2 を実装し、Redis 2.8 から 7.x までの主要コマンドを網羅。String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog のすべてのデータ構造に対応。
- **メッセージキュー**: シンプルキュー、**信頼性キュー（RPOPLPUSH + Ack セマンティクス）**、遅延キュー、Stream マルチコンシューマーグループキューを内蔵。すぐに使え、車輪の再発明は不要。
- **クラスタリングと高可用性**: Standalone / Master-Replica / Sentinel / Native Cluster に対応し、自動フェイルオーバーを備える。
- **クラウドプロバイダーアダプター**: **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS** 向けの専用構成と適応。プロキシモード、ACL 認証、SSL 暗号化、特殊な認証形式などの差異に対応。
- **互換実装**: Microsoft Garnet、kvrocks などの Redis 互換サーバーを自動検出し、未サポートの機能では明確な代替案を示してグレースフルデグラデーション。
- **幅広いフレームワーク対応**: 1 つのパッケージで **.NET Framework 4.5 から .NET 10+** をカバー。産業制御、政府、IoT などのレガシーシナリオに適する。

### ガイド

- [要件定義（中国語）](Doc/需求文档.md): ビジョン、目標、機能要件
- [アーキテクチャ（中国語）](Doc/架构文档.md): レイヤリング、コンポーネント、主要な設計判断
- [機能一覧（中国語）](Doc/功能清单.md): 各機能の実装・テスト・コメント状況
- [競合分析（中国語）](Doc/竞品分析.md): 主要な Redis クライアントとの比較
- [Garnet 互換性（中国語）](Doc/Garnet兼容性.md): 対応範囲とグレースフルデグラデーション
- [AOT 互換性（中国語）](Doc/AOT兼容性分析.md): NativeAOT の範囲と実測結果
- [ローカライズ（中国語）](Doc/多语言本地化.md): 例外とログのメッセージモデル
- [RedisSwitch 連携（中国語）](Doc/RedisSwitch项目说明.md): ゼロダウンタイムの Redis 切り替えツール
- [GC 割り当て分析（中国語）](Doc/dotMemory分析Redis的GC分配.md): dotMemory による計測
- [公式サイト](https://newlifex.com/core/redis): コンポーネントページとオンラインドキュメント

### プロトコルとバージョンの対応

#### RESP プロトコル対応

| プロトコル | 状態 | 説明 |
|----------|--------|-------------|
| RESP2 | ✅ 完全対応 | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ 完全対応 | Map / Set / Double / Boolean / Null / Blob Error 型。HELLO ハンドシェイクでネゴシエーション（Redis 6.0+） |

#### Redis バージョン別機能対応

| Redis バージョン | 主な機能 | 状態 |
|---------------|--------------|--------|
| 2.8 | 基本コマンド、SCAN、HyperLogLog、Pub/Sub | ✅ 完全対応 |
| 3.0 | Native Cluster、CRC16 シャードルーティング | ✅ 完全対応 |
| 3.2 | GEO 空間データ | ✅ 完全対応 |
| 4.0 | UNLINK 非同期削除、MODULE | 🔄 部分対応 |
| 5.0 | Stream データ構造（XADD/XREAD/XGROUP/XACK 完全対応） | ✅ 完全対応 |
| 6.0 | ACL アクセス制御、TLS/SSL、LMOVE、RESET | ✅ 完全対応 |
| 6.2 | COPY、GETDEL、GETEX、GEOSEARCH | ✅ 主要コマンド対応 |
| 7.0 | Functions（FCALL）、LMPOP/ZMPOP、シャード Pub/Sub | 🔄 計画中 |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 計画中 |

### アーキテクチャ

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

主要クラスの説明:

| クラス | 役割 |
|-------|------|
| `Redis` | コアクライアント: 接続プール、マルチノードフェイルオーバー、パイプライン、構成管理 |
| `FullRedis` | 拡張クライアント: クラスタルーティング、キープレフィックス、データ構造のファクトリメソッド |
| `RedisClient` | RESP プロトコル単位: TCP 接続、コマンドのエンコード/デコード（内部用、プール管理） |
| `RedisList<T>` など | 統一されたエンコード戦略を持つ汎用データ構造ラッパー |
| `RedisReliableQueue<T>` | 信頼性メッセージキュー（RPOPLPUSH + Ack 確認） |
| `RedisStream<T>` | Stream キュー（Redis 5.0+、コンシューマーグループモード） |
| `IPacketEncoder` | 差し替え可能なエンコード/デコード戦略 |

### 使用例

以下のすべての例は、[クイックスタート](#クイックスタート) で作成した実際の `FullRedis` インスタンスに対して実行します。

<details>
<summary><b>基本的な操作</b></summary>

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
<summary><b>データ構造</b></summary>

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

> 注意: 基底の `Redis` インスタンスは KV 操作のみに対応しています。高度なコレクションには `FullRedis` を使用してください。

</details>

<details>
<summary><b>パイプラインと自動バッチ処理</b></summary>

パイプラインはネットワークの往復（RTT）を大幅に削減し、一括操作のスループットを向上させます:

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**自動モード**（本番環境に推奨）:

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>メッセージキュー</b></summary>

#### シンプルキュー

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### 信頼性キュー（少なくとも 1 回のセマンティクス）

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

#### 遅延キュー

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### Stream キュー（Redis 5.0+、マルチコンシューマーグループ）

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
<summary><b>パブリッシュ / サブスクライブ</b></summary>

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
<summary><b>クラスタリングと高可用性</b></summary>

#### マルチアドレス自動フェイルオーバー

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### クラスタモードの自動検出

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
<summary><b>クラウドプロバイダーアダプター</b></summary>

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

**クラウドプロバイダー全般の注意点:**
- パブリッククラウドのプロキシモード: 内部ノードの自動検出を防ぐため、`AutoDetect = false`（デフォルト）を維持してください
- 接続数の上限: クラウドプロバイダーとインスタンスサイズごとに接続上限があります。プールの `Max` パラメータをそれに合わせて設定してください
- TCP keepalive: Huawei Cloud はデフォルトで 300 秒の TCP アイドルタイムアウトがあります。`Timeout` をその値より小さく設定し、定期的に PING を送ってください

</details>

<details>
<summary><b>シリアライズとエンコーダー</b></summary>

デフォルトのエンコーダーは `RedisJsonEncoder`（組み込みの JSON ホスト）で、すぐにそのまま使えます。

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

プリミティブ型（`String` / `Int32` / `Int64` / `Double` / `Boolean`）は最高のパフォーマンスを得るため、JSON を経由せず直接シリアライズされます。

</details>

<details>
<summary><b>セキュリティと認証</b></summary>

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
<summary><b>可観測性</b></summary>

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
<summary><b>拡張パッケージ</b></summary>

`NewLife.Redis.Extensions` は ASP.NET Core との深い統合を提供します:

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
<summary><b>Garnet 互換性</b></summary>

NewLife.Redis は Microsoft の高性能 Redis 互換サーバー [Garnet](https://github.com/microsoft/garnet) を完全にサポートし、自動検出とグレースフルデグラデーションを備えています。

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| 機能 | Garnet 対応状況 |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ 完全対応 |
| HyperLogLog / Geo | ✅ 完全対応 |
| Pub/Sub / トランザクション / Lua スクリプト | ✅ 完全対応 |
| Pipeline | ✅ 完全対応 |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ 完全対応 |
| `RedisReliableQueue<T>` | ✅ 完全対応 |
| `RedisStream<T>` | ❌ Garnet はまだ Stream に対応していません。自動的に `NotSupportedException` をスローし、代替案を提示します |

参照: [Garnet 互換性ドキュメント（中国語）](Doc/Garnet兼容性.md)

</details>

### リファレンス

<details>
<summary><b>機能実装状況</b></summary>

> 凡例: ✅ 完了 | 🔄 計画中 | ❌ 明示的に対象外

#### データ構造

| 構造 | 状態 | 説明 |
|-----------|--------|-------------|
| String KV + バッチ | ✅ | MGET/MSET/INCR/APPEND/GETRANGE など |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE など |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN など |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF など |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN など |
| Stream | ✅ | XADD/XREAD/XGROUP/XACK を完全サポート |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH など |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

#### メッセージキュー

| キューの種類 | 状態 | 配信セマンティクス |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | 最大 1 回 |
| `RedisReliableQueue<T>` | ✅ | 少なくとも 1 回（RPOPLPUSH + Ack） |
| `RedisDelayQueue<T>` | ✅ | 遅延配信（Sorted Set） |
| `RedisStream<T>` | ✅ | 順序付き + コンシューマーグループ + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | マルチコンシューマーグループブロードキャスト |
| `PubSub` + `RedisEventBus` | ✅ | リアルタイムプッシュ |
| RedLock 分散ロック | 🔄 | マルチインスタンスの Redlock アルゴリズム |

#### クラスタ対応

| モード | 状態 |
|------|--------|
| Standalone | ✅ |
| Master-Replica レプリケーション | ✅ |
| Sentinel | ✅ |
| Native Cluster | ✅ |
| kvrocks 自動適応 | ✅ |
| Garnet 自動検出 | ✅ |
| Valkey 自動検出 | ✅ |

#### プロトコルと高度な機能

| 機能 | 状態 |
|---------|--------|
| RESP2 完全対応 | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| ACL 認証 | ✅ |
| トランザクション MULTI/EXEC/WATCH | ✅ |
| Lua スクリプト EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 計画中 |
| パイプライン（明示 + 自動） | ✅ |
| パターン購読 PSUBSCRIBE | 🔄 |
| UNLINK 非同期削除 | 🔄 |

</details>

<details>
<summary><b>パフォーマンスベンチマーク</b></summary>

ソースにベンチマークを内蔵しています（`Samples/Benchmark`）。典型的な結果（40 論理プロセッサ、ローカル Redis 7.0）:

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

ベンチマークの手動実行:

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> 実際のパフォーマンスはネットワーク RTT / 値のサイズ / シリアライズの複雑さに左右されます。個々の値は 1〜1.4KB に保つことを推奨します。

</details>

<details>
<summary><b>ベストプラクティス</b></summary>

- **シングルトンでの再利用**: `FullRedis` は内部に接続プールを持つため、アプリケーションのライフサイクル全体で単一インスタンスを使うことを強く推奨します。毎回 `new` しないでください
- **キー設計**: 階層の区切りに `:` を使います（例: `user:1001:profile`）。本番環境では `Prefix` で名前空間を分離してください
- **まずバッチ化**: 単一キー操作のループより `GetAll/SetAll` を優先し、`AutoPipeline` で書き込み操作を自動マージしてください
- **値のサイズ**: 値は 1〜2KB 以内に保ち、大きなオブジェクトは分割 / 圧縮 / Hash によるフィールド単位の格納を行ってください
- **信頼性のある消費**: メッセージ消失を避けるため、本番環境では単純な List ではなく `RedisReliableQueue<T>` を使用してください
- **クラウドプロバイダー**: パブリッククラウドでは `AutoDetect = false` を維持し、内部ノードアドレスの問題を避けてください
- **監視**: 本番環境では `Counter` 統計を有効にし、`ClientLog` はデバッグ時のみ有効にしてホットパスのログ負荷を避けてください
- **SSL**: パブリッククラウドおよび本番環境では TLS 暗号化通信を有効にしてください

</details>

<details>
<summary><b>よくある質問（FAQ）</b></summary>

**Q: Redis Cluster のシャーディングに対応していますか？**
A: はい。`FullRedis` は `AutoDetect=true` で Cluster モードを自動検出します。CRC16 ルーティング、MOVED/ASK リダイレクトはすべて実装済みです。パブリッククラウドのプロキシモードでは、デフォルトの `AutoDetect=false` を維持してください。

**Q: StackExchange.Redis と比べてどうですか？**
A: NewLife.Redis は接続プールモデル（StackExchange の多重化に対して）を採用しており、接続の挙動がより直感的です。メッセージキューのラッパー（信頼性キュー / 遅延キュー）を内蔵している点が大きな差別化要素です。また .NET Framework 4.5 にも対応しています。詳細は [競合分析（中国語）](Doc/竞品分析.md) を参照してください。

**Q: 大きな値によるスロークエリにはどう対処しますか？**
A: データ構造の分割（例: Hash のフィールド単位）、読み書きのバッチ化、バイナリエンコーダー、`MaxMessageSize` の制御（デフォルト 1MB 制限）を組み合わせてください。

**Q: 接続プールが枯渇したらどうなりますか？**
A: デフォルトの上限は 10 万接続で、通常のワークロードでは枯渇しません。プールは `Redis.PoolConfig`（`RedisPoolConfig`）で設定できます: 最小アイドル数 / 最大容量 / アイドルクリーンアップ時間 / 最大ライフタイム / 借り出し待機タイムアウト。`IdleTime` を超えてアイドル状態の接続は、貸出時に事前に PING チェックされます。マスター/スレーブ切り替えやプロキシのローリング更新のシナリオでは、`MaxLifetime` を短くして接続のリサイクルを早めてください。待機タイムアウトが発生する場合は、接続リーク（`Dispose` 漏れ）や接続を過度に長時間保持する操作がないか確認してください。

**Q: Alibaba Cloud / Tencent Cloud で接続に失敗するのはなぜですか？**
A: 次を確認してください: ① `AutoDetect` が `false`（正しいデフォルト）になっているか ② 認証形式が正しいか（Tencent Cloud は `username=instanceId` が必要）③ ネットワークセキュリティグループ / ホワイトリストがクライアント IP を許可しているか ④ クラスタ版で SELECT によるデータベース切り替えが禁止されていないか

**Q: Garnet はすべての機能に対応していますか？**
A: 基本操作、List/Hash/Set/ZSet/HLL/Geo、キューはすべて対応しています。Stream はまだ未対応で、代替として `RedisQueue<T>` の使用を提案する `NotSupportedException` を自動的にスローします。

</details>

<details>
<summary><b>ロードマップ</b></summary>

- [x] ~~RESP3 プロトコル対応~~（完了）
- [ ] Redis Functions（FCALL/FUNCTION、Lua スクリプトの置き換え）
- [ ] LMPOP / ZMPOP（Redis 7.0 のバッチ pop）
- [ ] RedLock マルチインスタンス分散ロック
- [ ] Bitmap / Bitfield 操作
- [ ] パターン購読 PSUBSCRIBE の完全実装
- [ ] UNLINK 非同期削除
- [ ] Prometheus メトリクスエクスポートアダプター
- [x] Alibaba Cloud Tair 拡張（TairString / TairHash / TairZSet 対応）
- [ ] Huawei Cloud DCS クラスタ版ノードアドレスマッピングアダプター
- [x] ~~Pub/Sub~~（完了）
- [x] ~~RedisStream マルチコンシューマーグループ~~（完了）
- [x] ~~Garnet サーバータイプ検出~~（完了）

フィードバックや機能要望は Issue / PR でお寄せください。

</details>

<details>
<summary><b>競合比較</b></summary>

| 機能 | NewLife.Redis | StackExchange.Redis | FreeRedis | 備考 |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌（4.6.1+） | ❌ | 産業/政府系のレガシーシステムで重要 |
| 接続モデル | 接続プール | 多重化 | 接続プール | それぞれに用途がある |
| コマンド網羅率 | 約 98% | 約 95% | 約 98% | 明示的に対象外としたものを除き、すべてのドキュメント化されたコマンド |
| メッセージキューラッパー内蔵 | ✅ 充実 | ❌ | 🔄 部分対応 | 独自の信頼性/遅延キュー |
| シリアライズ内蔵 | ✅ | ❌ 手動 | ❌ 手動 | 定型的なコードを削減 |
| クラウドプロバイダー専用アダプター | ✅（Alibaba/Tencent/Huawei） | 汎用 | 汎用 | 専用ドキュメントあり |
| Garnet 専用アダプター | ✅ | ❌ | ❌ | タイプ検出 + グレースフルデグラデーション |
| kvrocks アダプター | ✅ | ❌ | ❌ | 自動検出 |
| Valkey 自動検出 | ✅ | ❌ | ❌ | INFO の `server=valkey` / `valkey_version` |
| APM トレース | ITracer + ActivitySource（net8.0+） | ActivitySource | ❌ | OpenTelemetry とのネイティブ統合 |
| RESP3 | ✅ | ✅ | ✅ | RESP3 テストスイートで検証済み |

詳細は [競合分析ドキュメント（中国語）](Doc/竞品分析.md) を参照してください。

</details>

### リリースノート

- [ChangeLog.md](ChangeLog.md): バージョン履歴（中国語）
- [ChangeLog.en.md](ChangeLog.en.md): バージョン履歴（英語）

## コントリビューション

### ビルドとテスト

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- 統合テストには稼働中の Redis が必要です: `REDIS_CONNECTION` を設定するか、`127.0.0.1:6379` で起動してください。それ以外の場合、`[RedisFact]` / `[RedisTopologyFact]` が付いたテストは自動的にスキップされます。
- NativeAOT スモーク: `AotSmoke/`、CI は `.github/workflows/aot.yml`。
- ベンチマーク: `Samples/Benchmark`。

### リポジトリ構成

| ディレクトリ | 説明 |
| --- | --- |
| `NewLife.Redis/` | クライアントのソース: 接続プール、RESP コーデック、データ構造、キュー |
| `NewLife.Redis.Extensions/` | ASP.NET Core 統合: `IDistributedCache`、DI、データ保護 |
| `NewLife.Extensions.Caching.Redis/` | 旧キャッシュ拡張の互換パッケージ |
| `XUnitTest/` | xUnit テストスイート（net10.0） |
| `AotSmoke/` | NativeAOT スモークプロジェクト |
| `Samples/` | Benchmark、WebDemo、QueueDemo、RedisSwitch、モバイルスモークのサンプル |
| `Doc/` | 要件、アーキテクチャ、機能一覧、各種トピックのドキュメント |

### リリース

`v` で始まるタグ（例: `v6.7.2026.1024`）をプッシュすると `.github/workflows/publish.yml` が起動します。ワークフローは `NewLife.Redis` と `NewLife.Redis.Extensions` をパックして NuGet.org と GitHub Packages に公開し、`ChangeLog.md` の該当バージョン節から GitHub Release を作成します。手動実行の場合はパッケージの公開のみを行います。

### フィードバック

1. 提出前に `dotnet build` と関連テストが通ることを確認し、リポジトリのコードスタイルとコミット規約に従ってください
2. PR: 変更は最小限に抑え、必要なコメントとテストの説明を追加してください
3. Issue: バージョン、実行環境、最小の再現手順を記載してください

コミュニティ: QQ グループ 1600800 / 1600838、GitHub Discussions / Issues。

## 新生命開発チーム

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

新生命チームは 2002 年に設立され、20 年以上にわたり .NET と IoT の技術エコシステムに注力し、100 近いオープンソースプロジェクトを主導し（うち 34 が主要プロジェクト）、電力、物流、医療、産業制御、インターネットなどの業界にソフトウェアとハードウェアを一体化したソリューションを提供しています。「学問に先後なし、達する者が師である」を信条としています。

オープンソースコンポーネントは NuGet で累計 <!--nuget-total:start-->6.48M<!--nuget-total:end--> 回ダウンロードされています（エコシステム全体、2026-10 時点）。

ウェブサイト：https://newlifex.com · オープンソース組織：https://github.com/NewLifeX

<details>
<summary>WeChat 公式アカウント：智能大石头（スキャンしてフォロー）</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## ライセンス

MIT ライセンス。商用利用 / 改変 / 再配布は自由です（追加の許諾は不要）。著作権表示を保持してください。

> このドキュメントがあなたのユースケースをカバーしていない場合は、お気軽に Issue を送ってください。一緒にドキュメントを改善しましょう！
