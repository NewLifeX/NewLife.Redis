# NewLife.Redis - 高效能 Redis 用戶端元件

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Redis?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Redis?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Redis/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Redis/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Redis?logo=github)](https://github.com/NewLifeX/NewLife.Redis/graphs/contributors)

<!-- lang:start -->
🌐 [中文](Readme.MD) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> 本翻譯可能落後，以簡體中文版為準。

NewLife.Redis 是高吞吐、易整合的 .NET Redis 用戶端，自 2017 年起經受日均 80 億次以上命令呼叫，支援叢集、哨兵、訊息佇列與多種雲端廠商。

## 安裝

```bash
dotnet add package NewLife.Redis
```

## 快速開始

```csharp
var redis = new FullRedis("server=127.0.0.1:6379;db=0");
redis.Set("key", "value");
var value = redis.Get<String>("key");
```

## 核心特性

- 高吞吐、低延遲，已在生產環境大規模驗證
- 連線集區、交易與非同步 API 完整支援
- 適配主流雲端服務與託管中介軟體

## 文件

> 📖 完整文件（中文）見 [儲存庫文件](Doc)

## 授權

MIT License © 2002-2026 新生命開發團隊

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [Website](https://newlifex.com)
