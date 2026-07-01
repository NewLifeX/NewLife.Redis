# NewLife.Redis — Client Redis Haute Performance

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)

🌐 [中文](Readme.MD) | [English](Readme.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [Русский](README.ru.md)

---

`NewLife.Redis` est un client Redis .NET **haute performance, haut débit et facile à intégrer**, développé par l'équipe NewLife. Depuis 2017, il fonctionne de manière stable sur de nombreuses plateformes de production à grande échelle, traitant plus de **80 milliards** d'appels de commandes par jour.

## Démarrage Rapide

```bash
dotnet add package NewLife.Redis
```

```csharp
using NewLife.Caching;

// Connexion
var redis = new FullRedis("server=127.0.0.1:6379;password=yourpwd;db=0");

// Opérations de base
redis.Set("key", "value");
var val = redis.Get<String>("key");

// Expiration de clé
redis.Set("temp", data, 60); // 60 secondes
```

## Fonctionnalités Principales

- **Protocole Complet** : RESP2/RESP3, toutes les commandes Redis 2.8~7.x
- **Toutes les Structures de Données** : String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog
- **Files de Messages** : Simple, fiable (RPOPLPUSH+Ack), différée, Stream multi-groupe
- **Cluster & Haute Disponibilité** : Standalone / Sentinel / Cluster, basculement automatique
- **Adaptation Cloud** : Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS
- **Large Support Framework** : .NET Framework 4.5 ~ .NET 10+
- **Compatibilité Garnet** : Détection automatique de Microsoft Garnet / kvrocks, dégradation élégante

## Packages d'Extension

| Package | Description |
|-----------|------|
| `NewLife.Redis` | Client Redis principal |
| `NewLife.Redis.Extensions` | Injection de dépendances ASP.NET Core, `IDistributedCache`, `IDataProtection` |

## Documentation

> 📖 Documentation complète (en chinois) : [Exigences](Doc/需求文档.md) | [Architecture](Doc/架构文档.md) | [Analyse Concurrentielle](Doc/竞品分析.md)

## Licence

MIT License © 2002-2026 [Équipe de Développement NewLife](https://newlifex.com)

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [Site Officiel](https://newlifex.com)
