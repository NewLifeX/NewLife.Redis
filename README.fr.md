# NewLife.Redis — Composant client Redis haute performance

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` est un composant client Redis .NET **haute performance / haut débit / facile à intégrer**, développé par l'équipe NewLife. Depuis 2017, il fonctionne de manière stable sur de multiples plateformes de production traitant des milliards d'enregistrements et une forte concurrence, avec plus de **80 milliards** d'appels de commandes traités chaque jour.

> 📖 Documentation complète : [Besoins (中文)](Doc/需求文档.md) | [Architecture (中文)](Doc/架构文档.md) | [Liste des fonctionnalités (中文)](Doc/功能清单.md) | [Analyse concurrentielle (中文)](Doc/竞品分析.md) | [Compatibilité Garnet (中文)](Doc/Garnet兼容性.md) | [Compatibilité AOT (中文)](Doc/AOT兼容性分析.md)

**Sommaire** : [Démarrage rapide](#démarrage-rapide) · [Fonctionnalités principales](#fonctionnalités-principales) · [Carte de l'écosystème](#carte-de-lécosystème) · [Documentation](#documentation) · [Contribution](#contribution) · [Équipe](#équipe) · [Licence](#licence)

## Démarrage rapide

### Installation

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### Utilisation de base

**Recommandé : instance unique, thread-safe, réutilisée pendant toute la durée de vie de l'application :**

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

**Mode chaîne de connexion (recommandé pour les centres de configuration) :**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### Prise en charge des plateformes

| Élément | Couverture |
| --- | --- |
| Frameworks cibles | Un seul paquet : net45, net461, netstandard2.0, netstandard2.1, net8.0 ; les extensions ajoutent netcoreapp3.1 → net10 |
| Systèmes d'exploitation et architectures | Windows, Linux, macOS, Android, iOS ; x64, arm64, loongarch64, riscv64, s390x |
| NativeAOT | `net8.0` est livré avec les contrôles `IsAotCompatible` ; `AotSmoke/` ainsi que la matrice de smoke CI couvrent win-x64 / linux-x64 / linux-arm64 / linux-la64 (LA64 via QEMU), avec win-arm64, macOS et les RID musl compilés nativement. linux-x64 est le seuil bloquant, les autres sont sous observation — voir [Compatibilité AOT](Doc/AOT兼容性分析.md) |

## Fonctionnalités principales

<details>
<summary><b>Moteur principal</b> (7)</summary>

| Fonctionnalité | Principales caractéristiques |
| --- | --- |
| Éprouvé à grande échelle | Plus de 200 instances Redis en production, avec un pic quotidien de plus de 100 M d'écritures d'objets métier et de 80 G+ d'appels de commandes. |
| Faible latence | Un aller-retour Get/Set unique prend 200–600 µs, réseau inclus. |
| Haut débit | Pool de connexions intégré (jusqu'à 100 000 connexions simultanées), analyseur RESP synchrone et batching auto-pipeline optionnel. |
| Nouvelle tentative automatique et bascule multi-adresses | Listes de nœuds séparées par des virgules, bascule rapide en cas d'erreur réseau et fenêtre de masquage de 10 secondes par défaut. |
| Encodeur enfichable | JSON par défaut (`RedisJsonEncoder`), extensible à la sérialisation binaire via `IPacketEncoder`. |
| Traçage APM | Traçage distribué `ITracer` et compteurs de performance `PerfCounter`. |
| Aucune dépendance lourde | Uniquement l'écosystème NewLife (journalisation, configuration, sérialisation, sécurité) au-dessus du BCL. |

</details>

<details>
<summary><b>Structures de données</b> (10)</summary>

| Structure | Principales caractéristiques |
| --- | --- |
| String KV + traitement par lots | MGET / MSET / INCR / APPEND / GETRANGE et assimilés. |
| List | LPUSH / RPOP / LRANGE / LMOVE. |
| Hash | HGET / HSET / HMGET / HSCAN. |
| Set | SADD / SMEMBERS / SUNION / SDIFF. |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN. |
| Pile | Encapsulation LIFO au-dessus de List. |
| Stream | XADD / XREAD / XGROUP / XACK complets. |
| Geo | GEOADD / GEODIST / GEOSEARCH. |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE. |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD (prévu). |

</details>

<details>
<summary><b>Files de messages et Pub/Sub</b> (7)</summary>

| Type | Principales caractéristiques |
| --- | --- |
| `RedisQueue<T>` | File simple avec livraison au plus une fois. |
| `RedisReliableQueue<T>` | Livraison au moins une fois avec RPOPLPUSH + rollback Ack. |
| `RedisDelayQueue<T>` | Livraison différée reposant sur un Sorted Set. |
| `RedisStream<T>` | Flux ordonné avec groupes de consommateurs et XACK. |
| `MultipleConsumerGroupsQueue<T>` | Diffusion vers plusieurs groupes de consommateurs. |
| `PubSub` + `RedisEventBus` | Publication/abonnement en temps réel. |
| Verrou distribué RedLock | Algorithme Redlock multi-instances (en cours). |

</details>

<details>
<summary><b>Clustering et haute disponibilité</b> (7)</summary>

| Mode | Principales caractéristiques |
| --- | --- |
| Standalone | Connexion directe à une instance unique avec l'empreinte minimale. |
| Réplication maître-réplica | Séparation lecture/écriture et bascule. |
| Sentinel | Découverte Sentinel avec bascule automatique du maître. |
| Cluster natif | Routage par slot CRC16 avec redirection MOVED / ASK. |
| kvrocks | S'adapte automatiquement au moteur de stockage kvrocks. |
| Garnet | Détecte automatiquement Microsoft Garnet et se dégrade gracieusement lorsqu'une fonctionnalité manque. |
| Valkey | Détecté à partir du serveur INFO. |

</details>

<details>
<summary><b>Protocole et compatibilité serveur</b> (7)</summary>

| Capacité | Principales caractéristiques |
| --- | --- |
| RESP2 | Chaîne simple, erreur, entier, chaîne bulk et tableau entièrement implémentés. |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error, négociés via la poignée de main HELLO (Redis 6.0+). |
| Redis 2.8 – 7.x | Commandes principales de chaque version, y compris COPY / GETDEL / GETEX / GEOSEARCH depuis 6.2. |
| Authentification ACL | Authentification par nom d'utilisateur et mot de passe Redis 6.0+. |
| Transactions | MULTI / EXEC / WATCH. |
| Scripts Lua | EVAL / EVALSHA. |
| Redis Functions | FCALL / FUNCTION (prévu). |

</details>

<details>
<summary><b>Cloud, sécurité et observabilité</b> (8)</summary>

| Capacité | Principales caractéristiques |
| --- | --- |
| Alibaba Cloud KVStore / Tair | Édition standard, ACL, SSL et conseils pour le mode proxy, plus les structures Tair (TairString / TairHash / TairZSet). |
| Tencent Cloud Redis | Utilise l'ID d'instance comme nom d'utilisateur (`instanceId:password`). |
| Huawei Cloud DCS | Différences entre éditions maître/standby, SSL et cluster (pas de SELECT en mode cluster). |
| TLS/SSL | Version de protocole configurable et TLS mutuel avec certificats clients. |
| Journalisation structurée | Journal du composant (`Log`) et journal au niveau protocole (`ClientLog`). |
| Compteurs de performance | `PerfCounter` rapporte les ops/s, la latence et le taux d'erreur. |
| Traçage APM | Des spans `ITracer` partout, plus une `ActivitySource` (net8.0+) pour OpenTelemetry. |
| Protection de la chaîne de connexion | `ProtectedKey` déchiffre les mots de passe intégrés aux chaînes de connexion. |

</details>

<details>
<summary><b>Plateforme et intégration</b> (5)</summary>

| Capacité | Principales caractéristiques |
| --- | --- |
| Multi-cible | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0 ; les paquets d'extension couvrent netcoreapp3.1 → net10. |
| Nommage fort | Signé avec `newlife.snk` afin que les assemblys puissent être reconstruits et remplacés. |
| NativeAOT | Contrôles statiques `IsAotCompatible`, projet `AotSmoke/` et matrice CI multi-RID. |
| Paquet ASP.NET Core | `NewLife.Redis.Extensions` enregistre `IDistributedCache` et `FullRedis` via `AddRedisCaching`. |
| DataProtection | `PersistKeysToRedis` persiste les clés de protection des données dans Redis. |

</details>

## Carte de l'écosystème

NewLife est un écosystème en couches de projets open source versionnés indépendamment. La carte ci-dessous est partagée à l'identique par chaque dépôt NewLife ; chaque projet renvoie à son dépôt et à son paquet NuGet principal.

| Projet | Rôle | Téléchargements |
| --- | --- | --- |
| **Fondation** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Journalisation, configuration, cache, réseau, sérialisation, ordonnancement et traçage APM. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Intergiciel de données : tables uniques à l'échelle de dizaines de milliards, partitionnement automatique, séparation lecture/écriture et plus de 10 bases de données. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Bibliothèque réseau hautes performances : 1,77 M pps / 14,5 Gbps sur une machine (TCP 1KB, 2026-10), 4 M connexions TCP persistantes. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Communication protocolaire et framework RPC pour applications client/serveur et appareils IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installe des applications .NET comme démons système : services Windows et systemd Linux. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Modèles de projet `dotnet new` pour console, web, API, service, données et IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Données et messagerie** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Client Redis avec une latence en microsecondes, des millions d'opérations/s et des files de messages intégrées. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Client RocketMQ entièrement managé pour Apache RocketMQ et Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Client et serveur MQTT, y compris la connectivité Alibaba Cloud IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Base de données hybride en C# : embarquée ou en réseau, relationnelle, séries temporelles, file de messages et KV réunis. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Pilote et couche d'assistance MySql construits sur la pile de données NewLife. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, IA et distribué** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Framework rapide de plateforme d'administration : permissions, SSO, serveur OAuth et échafaudage CRUD. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Plateforme de services distribués : gestion des nœuds, APM, centre de configuration, registre et centre de publication. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plateforme de tâches distribuées et d'ordonnancement de données en temps réel avec partitionnement, reprises et backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Client unifié pour 46 fournisseurs de LLM, plus ChatAI, une application web de chat prête à l'emploi. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industriel et IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Bibliothèque standard IoT définissant les contrats de protocole et les abstractions d'appareils. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Implémentation Modbus TCP/RTU/ASCII au-dessus de NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Protocole pour automate Siemens au-dessus de NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Protocole pour automate Omron au-dessus de NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Protocole pour automate Mitsubishi Melsec au-dessus de NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Client OPC pour l'accès aux données industrielles. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Pile de protocoles BACnet pour l'automatisation des bâtiments. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Plateforme IoT open source légère avec gestion des passerelles et des appareils. | — |
| **Télématique embarquée** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | Protocole de positionnement de véhicules JT/T 808 : plus de 70 corps de message, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | Protocole plateforme à plateforme JT/T 809 pour les autorités de transport. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | Vidéo embarquée JT/T 1078 : analyse RTP, remuxage FLV/fMP4 et diffusion en direct. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Composants métier** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Un seul paquet pour Excel, Word, PPT, PDF et plus de 15 formats de documents. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Audio multiplateforme : codecs, E/S d'appareils, DSP, conteneurs et streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Codecs vidéo, formats de conteneurs, traitement des images et streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Interface unifiée pour Baidu, AMap, Tencent et Tianditu avec géocodage et conversion de coordonnées. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Géolocalisation IPv4 avec une base de données hors ligne mappée en mémoire et une API en ligne en secours. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Outils et produits** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Boîte à outils du développeur : réseau, port série, cryptographie, regex, Modbus et MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Système de gestion de code source Git auto-hébergé. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installe automatiquement le runtime .NET correspondant lorsqu'il est lancé depuis un hôte plus ancien. | — |

> Les badges indiquent le paquet principal de chaque projet ; les paquets de thèmes, d'extensions et satellites sont répertoriés sur leurs pages NuGet. Les plus de 30 paquets non listés ici (FTP, LoRa, extensions Office, pilotes de protocoles, etc.) sont disponibles dans l'[organisation NewLifeX](https://github.com/NewLifeX) et sur [newlifex.com](https://newlifex.com).

## Documentation

**NewLife.Redis** offre aux développeurs .NET un accès Redis sur tout son cycle de vie :

- **Protocole complet** : implémente RESP2, couvre toutes les commandes principales de Redis 2.8 à 7.x, prend en charge toutes les structures de données : String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog.
- **Files de messages** : file simple intégrée, **file fiable (RPOPLPUSH + sémantique Ack)**, file différée et file Stream à groupes de consommateurs multiples — prêtes à l'emploi, sans réinventer la roue.
- **Clustering et haute disponibilité** : prend en charge Standalone / Master-Replica / Sentinel / Native Cluster, avec bascule automatique.
- **Adaptateurs pour fournisseurs cloud** : configuration et adaptation dédiées pour **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**, couvrant le mode proxy, l'authentification ACL, le chiffrement SSL, les formats d'authentification particuliers et autres différences.
- **Implémentations compatibles** : détecte automatiquement Microsoft Garnet, kvrocks et autres serveurs compatibles Redis, et se dégrade gracieusement avec des alternatives claires lorsque des fonctionnalités ne sont pas prises en charge.
- **Large prise en charge des frameworks** : un seul paquet couvre **.NET Framework 4.5 à .NET 10+**, adapté au contrôle industriel, aux administrations, à l'IoT et à d'autres scénarios hérités.

### Guides

- [Besoins (中文)](Doc/需求文档.md) : vision, objectifs et exigences fonctionnelles
- [Architecture (中文)](Doc/架构文档.md) : couches, composants et décisions de conception clés
- [Liste des fonctionnalités (中文)](Doc/功能清单.md) : état d'implémentation, de test et de commentaires de chaque fonctionnalité
- [Analyse concurrentielle (中文)](Doc/竞品分析.md) : comparaison avec les clients Redis grand public
- [Compatibilité Garnet (中文)](Doc/Garnet兼容性.md) : périmètre pris en charge et dégradation gracieuse
- [Compatibilité AOT (中文)](Doc/AOT兼容性分析.md) : périmètre NativeAOT et conclusions mesurées
- [Localisation (中文)](Doc/多语言本地化.md) : modèle de messages pour les exceptions et les journaux
- [Intégration RedisSwitch (中文)](Doc/RedisSwitch项目说明.md) : outil de bascule Redis sans interruption
- [Analyse des allocations GC (中文)](Doc/dotMemory分析Redis的GC分配.md) : mesures dotMemory
- [Site officiel](https://newlifex.com/core/redis) : page du composant et documentation en ligne

### Prise en charge des protocoles et des versions

#### Prise en charge du protocole RESP

| Protocole | État | Description |
|----------|--------|-------------|
| RESP2 | ✅ Entièrement pris en charge | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ Entièrement pris en charge | Types Map / Set / Double / Boolean / Null / Blob Error, négociés via la poignée de main HELLO (Redis 6.0+) |

#### Prise en charge des fonctionnalités par version de Redis

| Version de Redis | Fonctionnalités clés | État |
|---------------|--------------|--------|
| 2.8 | Commandes de base, SCAN, HyperLogLog, Pub/Sub | ✅ Entièrement pris en charge |
| 3.0 | Native Cluster, routage de shards CRC16 | ✅ Entièrement pris en charge |
| 3.2 | Données spatiales GEO | ✅ Entièrement pris en charge |
| 4.0 | UNLINK suppression asynchrone, MODULE | 🔄 Prise en charge partielle |
| 5.0 | Structure de données Stream (XADD/XREAD/XGROUP/XACK complets) | ✅ Entièrement pris en charge |
| 6.0 | Contrôle d'accès ACL, TLS/SSL, LMOVE, RESET | ✅ Entièrement pris en charge |
| 6.2 | COPY, GETDEL, GETEX, GEOSEARCH | ✅ Commandes principales prises en charge |
| 7.0 | Functions (FCALL), LMPOP/ZMPOP, Pub/Sub avec sharding | 🔄 Prévu |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 Prévu |

### Architecture

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

Description des classes clés :

| Classe | Rôle |
|-------|------|
| `Redis` | Client principal : pool de connexions, bascule multi-nœuds, pipeline, gestion de la configuration |
| `FullRedis` | Client enrichi : routage de cluster, préfixe de clé, méthodes de fabrique pour les structures de données |
| `RedisClient` | Unité de protocole RESP : connexion TCP, encodage/décodage des commandes (interne, géré par le pool) |
| `RedisList<T>` et autres | Encapsulations génériques de structures de données avec stratégie d'encodage unifiée |
| `RedisReliableQueue<T>` | File de messages fiable (RPOPLPUSH + confirmation Ack) |
| `RedisStream<T>` | File Stream (Redis 5.0+, mode groupes de consommateurs) |
| `IPacketEncoder` | Stratégie d'encodage/décodage enfichable |

### Exemples d'utilisation

Tous les exemples ci-dessous s'exécutent sur une instance `FullRedis` active créée dans [Démarrage rapide](#démarrage-rapide).

<details>
<summary><b>Opérations de base</b></summary>

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
<summary><b>Structures de données</b></summary>

#### Liste

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

#### Pile

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

> Remarque : l'instance de base `Redis` ne prend en charge que les opérations KV. Utilisez `FullRedis` pour les collections avancées.

</details>

<details>
<summary><b>Pipeline et batching automatique</b></summary>

Le pipeline réduit considérablement les allers-retours réseau (RTT) et améliore le débit des opérations groupées :

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**Mode automatique** (recommandé en production) :

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>Files de messages</b></summary>

#### File simple

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### File fiable (sémantique au moins une fois)

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

#### File différée

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### File Stream (Redis 5.0+, groupes de consommateurs multiples)

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
<summary><b>Publication / Abonnement</b></summary>

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
<summary><b>Clustering et haute disponibilité</b></summary>

#### Bascule automatique multi-adresses

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### Détection automatique du mode cluster

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
<summary><b>Adaptateurs pour fournisseurs cloud</b></summary>

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

**Remarques générales sur les fournisseurs cloud :**
- Mode proxy cloud public : conservez `AutoDetect = false` (valeur par défaut) pour empêcher la détection automatique des nœuds internes
- Quota de connexions : chaque fournisseur cloud et taille d'instance impose des limites de connexions ; ajustez le paramètre `Max` du pool en conséquence
- TCP keepalive : Huawei Cloud applique par défaut un délai d'inactivité TCP de 300 secondes ; réglez `Timeout` en dessous de cette valeur et envoyez périodiquement des PING

</details>

<details>
<summary><b>Sérialisation et encodeurs</b></summary>

Encodeur par défaut : `RedisJsonEncoder` (hôte JSON intégré), prêt à l'emploi immédiatement.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

Les types primitifs (`String` / `Int32` / `Int64` / `Double` / `Boolean`) sont sérialisés directement sans passer par JSON pour une performance maximale.

</details>

<details>
<summary><b>Sécurité et authentification</b></summary>

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
<summary><b>Observabilité</b></summary>

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
<summary><b>Paquets d'extension</b></summary>

`NewLife.Redis.Extensions` offre une intégration approfondie avec ASP.NET Core :

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
<summary><b>Compatibilité Garnet</b></summary>

NewLife.Redis prend entièrement en charge [Garnet](https://github.com/microsoft/garnet) de Microsoft, un serveur compatible Redis haute performance, avec détection automatique et dégradation gracieuse.

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| Fonctionnalité | État de prise en charge par Garnet |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ Entièrement pris en charge |
| HyperLogLog / Geo | ✅ Entièrement pris en charge |
| Pub/Sub / Transactions / Scripts Lua | ✅ Entièrement pris en charge |
| Pipeline | ✅ Entièrement pris en charge |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ Entièrement pris en charge |
| `RedisReliableQueue<T>` | ✅ Entièrement pris en charge |
| `RedisStream<T>` | ❌ Garnet ne prend pas encore en charge Stream ; lève automatiquement une `NotSupportedException` avec des alternatives suggérées |

Voir : [Documentation de compatibilité Garnet (中文)](Doc/Garnet兼容性.md)

</details>

### Référence

<details>
<summary><b>État d'avancement des fonctionnalités</b></summary>

> Légende : ✅ Terminé | 🔄 Prévu | ❌ Explicitement non prévu

#### Structures de données

| Structure | État | Description |
|-----------|--------|-------------|
| String KV + traitement par lots | ✅ | MGET/MSET/INCR/APPEND/GETRANGE, etc. |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE, etc. |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN, etc. |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF, etc. |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN, etc. |
| Stream | ✅ | XADD/XREAD/XGROUP/XACK complets |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH, etc. |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

#### Files de messages

| Type de file | État | Sémantique de livraison |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | Au plus une fois |
| `RedisReliableQueue<T>` | ✅ | Au moins une fois (RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | Livraison différée (Sorted Set) |
| `RedisStream<T>` | ✅ | Ordonné + groupe de consommateurs + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | Diffusion multi-groupes de consommateurs |
| `PubSub` + `RedisEventBus` | ✅ | Poussée en temps réel |
| Verrou distribué RedLock | 🔄 | Algorithme Redlock multi-instances |

#### Prise en charge du clustering

| Mode | État |
|------|--------|
| Standalone | ✅ |
| Réplication maître-réplica | ✅ |
| Sentinel | ✅ |
| Cluster natif | ✅ |
| Adaptation automatique kvrocks | ✅ |
| Détection automatique Garnet | ✅ |
| Détection automatique Valkey | ✅ |

#### Protocole et fonctionnalités avancées

| Fonctionnalité | État |
|---------|--------|
| Prise en charge complète de RESP2 | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| Authentification ACL | ✅ |
| Transactions MULTI/EXEC/WATCH | ✅ |
| Scripts Lua EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 Prévu |
| Pipeline (explicite + automatique) | ✅ |
| Abonnement par motif PSUBSCRIBE | 🔄 |
| Suppression asynchrone UNLINK | 🔄 |

</details>

<details>
<summary><b>Benchmarks de performance</b></summary>

Benchmark intégré au code source (`Samples/Benchmark`), résultats typiques (40 processeurs logiques, Redis 7.0 local) :

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

Déclenchement manuel du bench :

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> Les performances réelles dépendent du RTT réseau / de la taille des valeurs / de la complexité de sérialisation. Il est recommandé de maintenir chaque valeur entre 1 et 1,4 Ko.

</details>

<details>
<summary><b>Bonnes pratiques</b></summary>

- **Réutilisation en singleton** : `FullRedis` possède un pool de connexions interne ; il est fortement recommandé d'utiliser une seule instance pendant toute la durée de vie de l'application — ne faites pas de `new` à chaque fois
- **Conception des clés** : utilisez `:` comme séparateur hiérarchique, par exemple `user:1001:profile` ; utilisez `Prefix` pour isoler les espaces de noms en production
- **Privilégier le traitement par lots** : préférez `GetAll/SetAll` aux opérations mono-clé en boucle ; utilisez `AutoPipeline` pour fusionner automatiquement les écritures
- **Taille des valeurs** : gardez les valeurs entre 1 et 2 Ko ; découpez les gros objets / compressez / utilisez Hash pour un stockage par champ
- **Consommation fiable** : utilisez `RedisReliableQueue<T>` en production plutôt qu'une simple List pour éviter la perte de messages
- **Fournisseurs cloud** : conservez `AutoDetect = false` sur les clouds publics pour éviter les problèmes d'adresses de nœuds internes
- **Surveillance** : activez les statistiques `Counter` en production ; n'activez `ClientLog` que pour le débogage afin d'éviter la surcharge de journalisation sur le chemin critique
- **SSL** : activez le transport chiffré TLS sur les clouds publics et dans les environnements de production

</details>

<details>
<summary><b>FAQ</b></summary>

**Q : Prend-il en charge le sharding de Redis Cluster ?**
A : Oui. `FullRedis` détecte automatiquement le mode Cluster avec `AutoDetect=true`. Le routage CRC16 et les redirections MOVED/ASK sont tous implémentés. Pour le mode proxy des clouds publics, conservez la valeur par défaut `AutoDetect=false`.

**Q : Comment se compare-t-il à StackExchange.Redis ?**
A : NewLife.Redis utilise un modèle de pool de connexions (contrairement au multiplexage de StackExchange) pour un comportement de connexion plus intuitif. Les encapsulations de files de messages intégrées (file fiable / file différée) constituent un différenciateur clé. Il prend également en charge .NET Framework 4.5. Voir [Analyse concurrentielle (中文)](Doc/竞品分析.md).

**Q : Comment gérer les requêtes lentes causées par de grandes valeurs ?**
A : Découpez les structures de données (par exemple Hash par champ) + lectures/écritures par lots + encodeur binaire + contrôle de `MaxMessageSize` (limite par défaut de 1 Mo).

**Q : Que faire si le pool de connexions est épuisé ?**
A : Le maximum par défaut est de 100 000 connexions ; les charges normales ne l'épuiseront pas. Le pool est configurable via `Redis.PoolConfig` (`RedisPoolConfig`) : inactives minimales / capacité maximale / délai de nettoyage des inactives / durée de vie maximale / délai d'attente d'emprunt. Les connexions inactives au-delà de `IdleTime` font l'objet d'un PING proactif lors de l'emprunt ; pour les scénarios de bascule maître-esclave ou de mise à jour glissante de proxy, réduisez `MaxLifetime` pour recycler les connexions plus vite. Si vous observez des délais d'attente, vérifiez les fuites de connexions (`Dispose` manquant) ou les opérations qui conservent les connexions trop longtemps.

**Q : Pourquoi les connexions échouent-elles sur Alibaba Cloud / Tencent Cloud ?**
A : Vérifiez : ① `AutoDetect` est-il défini sur `false` (valeur par défaut correcte) ? ② Le format d'authentification est-il correct (Tencent Cloud exige `username=instanceId`) ? ③ Le groupe de sécurité réseau / la liste blanche autorisent-ils l'IP cliente ? ④ L'édition cluster interdit-elle l'utilisation de SELECT pour changer de base ?

**Q : Garnet prend-il en charge toutes les fonctionnalités ?**
A : Les opérations de base, List/Hash/Set/ZSet/HLL/Geo et les files sont toutes prises en charge. Stream n'est pas encore pris en charge et lève automatiquement une `NotSupportedException` avec une suggestion d'utiliser `RedisQueue<T>` comme alternative.

</details>

<details>
<summary><b>Feuille de route</b></summary>

- [x] ~~Prise en charge du protocole RESP3~~ (Terminé)
- [ ] Redis Functions (FCALL/FUNCTION, remplaçant les scripts Lua)
- [ ] LMPOP / ZMPOP (pop par lots Redis 7.0)
- [ ] Verrou distribué multi-instances RedLock
- [ ] Opérations Bitmap / Bitfield
- [ ] Implémentation complète de l'abonnement par motif PSUBSCRIBE
- [ ] Suppression asynchrone UNLINK
- [ ] Adaptateur d'export de métriques Prometheus
- [x] Extension Alibaba Cloud Tair (TairString / TairHash / TairZSet pris en charge)
- [ ] Adaptateur de mappage d'adresses de nœuds pour l'édition cluster Huawei Cloud DCS
- [x] ~~Pub/Sub~~ (Terminé)
- [x] ~~Groupes de consommateurs multiples RedisStream~~ (Terminé)
- [x] ~~Détection du type de serveur Garnet~~ (Terminé)

Vos retours et demandes de fonctionnalités sont bienvenus via Issue / PR.

</details>

<details>
<summary><b>Comparaison concurrentielle</b></summary>

| Capacité | NewLife.Redis | StackExchange.Redis | FreeRedis | Remarques |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | Critique pour les systèmes industriels/administratifs hérités |
| Modèle de connexion | Pool de connexions | Multiplexage | Pool de connexions | Chacun a ses cas d'usage |
| Couverture des commandes | ~98% | ~95% | ~98% | Toutes les commandes documentées, sauf celles explicitement non prévues |
| Encapsulations de files de messages intégrées | ✅ Complètes | ❌ | 🔄 Partielles | Files fiables/différées uniques |
| Sérialisation intégrée | ✅ | ❌ Manuelle | ❌ Manuelle | Réduit le code répétitif |
| Adaptateurs dédiés aux fournisseurs cloud | ✅ (Alibaba/Tencent/Huawei) | Génériques | Génériques | Documentation dédiée |
| Adaptateur explicite Garnet | ✅ | ❌ | ❌ | Détection de type + dégradation gracieuse |
| Adaptateur kvrocks | ✅ | ❌ | ❌ | Détection automatique |
| Détection automatique Valkey | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| Traçage APM | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | Intégration OpenTelemetry native |
| RESP3 | ✅ | ✅ | ✅ | Vérifié via la suite de tests RESP3 |

Voir le [document d'analyse concurrentielle (中文)](Doc/竞品分析.md) pour plus de détails.

</details>

### Notes de version

- [ChangeLog.md](ChangeLog.md) : historique des versions (chinois)
- [ChangeLog.en.md](ChangeLog.en.md) : historique des versions (anglais)

## Contribution

### Compilation et tests

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- Les tests d'intégration nécessitent un Redis actif : définissez `REDIS_CONNECTION` ou démarrez-en un sur `127.0.0.1:6379` ; sinon les tests marqués `[RedisFact]` / `[RedisTopologyFact]` sont ignorés automatiquement.
- Smoke NativeAOT : `AotSmoke/`, CI dans `.github/workflows/aot.yml`.
- Benchmarks : `Samples/Benchmark`.

### Organisation du dépôt

| Répertoire | Description |
| --- | --- |
| `NewLife.Redis/` | Sources du client : pool de connexions, codec RESP, structures de données et files |
| `NewLife.Redis.Extensions/` | Intégration ASP.NET Core : `IDistributedCache`, DI et protection des données |
| `NewLife.Extensions.Caching.Redis/` | Paquet de compatibilité hérité pour l'extension de mise en cache |
| `XUnitTest/` | Suite de tests xUnit (net10.0) |
| `AotSmoke/` | Projet de smoke NativeAOT |
| `Samples/` | Échantillons Benchmark, WebDemo, QueueDemo, RedisSwitch et smoke mobile |
| `Doc/` | Documents de besoins, architecture, liste des fonctionnalités et documents thématiques |

### Publication

Poussez un tag préfixé par `v` (par exemple `v7.0.2026.1024`) pour déclencher `.github/workflows/publish.yml`. Le workflow empaquette `NewLife.Redis` et `NewLife.Redis.Extensions`, les publie sur NuGet.org et GitHub Packages et crée une GitHub Release dont les notes proviennent de la section correspondante de `ChangeLog.md` ; une exécution manuelle ne publie que les paquets.

### Retours

1. Avant de soumettre, assurez-vous que `dotnet build` et les tests concernés passent, et respectez le style de code et les conventions de commit du dépôt
2. PR : gardez les modifications minimales, ajoutez les commentaires et descriptions de tests nécessaires
3. Issues : fournissez la version, l'environnement d'exécution et un scénario de reproduction minimal

Communauté : groupes QQ 1600800 / 1600838 ; GitHub Discussions / Issues.

## Équipe

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

NewLife a été fondée en 2002 et œuvre depuis plus de deux décennies dans l'écosystème .NET et IoT, maintient près de 100 projets open source (dont 34 principaux) et fournit des solutions matérielles et logicielles intégrées aux secteurs de l'énergie, de la logistique, de la santé, du contrôle industriel et d'Internet. Notre devise : « l'apprentissage ne connaît pas l'ancienneté — celui qui maîtrise devient maître ».

Les composants open source ont été téléchargés <!--nuget-total:start-->6.48M<!--nuget-total:end--> fois depuis NuGet (à l'échelle de l'écosystème, en date de 2026-10).

Site web : https://newlifex.com · Organisation open source : https://github.com/NewLifeX

<details>
<summary>Compte officiel WeChat : 智能大石头 (scannez pour suivre)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## Licence

Licence MIT. Utilisation commerciale / modification / redistribution libres (aucune autorisation supplémentaire requise). Conservez simplement la mention de copyright.

> Si ce document ne couvre pas votre cas d'usage, n'hésitez pas à soumettre une Issue — améliorons la documentation ensemble !
