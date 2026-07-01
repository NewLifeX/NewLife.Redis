# NewLife.Redis — Cliente Redis de Alto Desempenho

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)

🌐 [中文](Readme.MD) | [English](Readme.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Русский](README.ru.md)

---

`NewLife.Redis` é um cliente Redis para .NET de **alto desempenho, alta taxa de transferência e fácil integração**, desenvolvido pela equipe NewLife. Desde 2017, funciona de forma estável em várias plataformas de produção de grande escala, processando mais de **80 bilhões** de chamadas de comandos por dia.

## Início Rápido

```bash
dotnet add package NewLife.Redis
```

```csharp
using NewLife.Caching;

// Conexão
var redis = new FullRedis("server=127.0.0.1:6379;password=yourpwd;db=0");

// Operações básicas
redis.Set("key", "value");
var val = redis.Get<String>("key");

// Expiração de chave
redis.Set("temp", data, 60); // 60 segundos
```

## Principais Funcionalidades

- **Protocolo Completo**: RESP2/RESP3, todos os comandos Redis 2.8~7.x
- **Todas as Estruturas de Dados**: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog
- **Filas de Mensagens**: Simples, confiável (RPOPLPUSH+Ack), com atraso, Stream multi-grupo
- **Cluster & Alta Disponibilidade**: Standalone / Sentinel / Cluster, failover automático
- **Adaptação a Nuvens**: Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS
- **Amplo Suporte de Framework**: .NET Framework 4.5 ~ .NET 10+
- **Compatibilidade Garnet**: Detecção automática de Microsoft Garnet / kvrocks, degradação elegante

## Pacotes de Extensão

| Pacote | Descrição |
|-----------|------|
| `NewLife.Redis` | Cliente Redis principal |
| `NewLife.Redis.Extensions` | Injeção de dependência ASP.NET Core, `IDistributedCache`, `IDataProtection` |

## Documentação

> 📖 Documentação completa (em chinês): [Requisitos](Doc/需求文档.md) | [Arquitetura](Doc/架构文档.md) | [Análise Competitiva](Doc/竞品分析.md)

## Licença

MIT License © 2002-2026 [Equipe de Desenvolvimento NewLife](https://newlifex.com)

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [Site Oficial](https://newlifex.com)
