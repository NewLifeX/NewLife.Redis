# NewLife.Redis — 高性能 Redis クライアント

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)

🌐 [中文](Readme.MD) | [English](Readme.en.md) | [한국어](README.ko.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Русский](README.ru.md)

---

`NewLife.Redis` は、NewLife チームが開発した **高性能・高スループット・統合容易** な .NET Redis クライアントです。2017年から複数の大規模プロダクション環境で稼働し、**1日80億回以上**のコマンド呼び出しを処理しています。

## クイックスタート

```bash
dotnet add package NewLife.Redis
```

```csharp
using NewLife.Caching;

// 接続
var redis = new FullRedis("server=127.0.0.1:6379;password=yourpwd;db=0");

// 基本的な操作
redis.Set("key", "value");
var val = redis.Get<String>("key");

// キーの有効期限設定
redis.Set("temp", data, 60); // 60秒
```

## 主要機能

- **完全なプロトコル対応**: RESP2/RESP3、Redis 2.8〜7.x の全コマンドをサポート
- **全データ構造**: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog
- **メッセージキュー**: シンプル・信頼性（RPOPLPUSH+Ack）・遅延・Stream マルチコンシューマグループ
- **クラスタ & HA**: Standalone / Sentinel / Cluster、自動フェイルオーバー
- **クラウド対応**: Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS
- **幅広いフレームワーク**: .NET Framework 4.5 〜 .NET 10+ 対応
- **Garnet 互換**: Microsoft Garnet / kvrocks を自動検出、機能低下時に適切な代替案を提供

## 拡張パッケージ

| パッケージ | 説明 |
|-----------|------|
| `NewLife.Redis` | コア Redis クライアント |
| `NewLife.Redis.Extensions` | ASP.NET Core DI、`IDistributedCache`、`IDataProtection` |

## ドキュメント

> 📖 完全なドキュメント（中国語）: [要件](Doc/需求文档.md) | [アーキテクチャ](Doc/架构文档.md) | [競合分析](Doc/竞品分析.md)

## ライセンス

MIT License © 2002-2026 [新生命開発チーム](https://newlifex.com)

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [公式サイト](https://newlifex.com)
