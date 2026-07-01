# NewLife.Redis — Cliente Redis de Alto Rendimiento

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)

🌐 [中文](Readme.MD) | [English](Readme.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Русский](README.ru.md)

---

`NewLife.Redis` es un cliente Redis para .NET **de alto rendimiento, alta productividad y fácil integración**, desarrollado por el equipo NewLife. Desde 2017, funciona de forma estable en múltiples plataformas de producción a gran escala, procesando más de **80 mil millones** de llamadas de comandos al día.

## Inicio Rápido

```bash
dotnet add package NewLife.Redis
```

```csharp
using NewLife.Caching;

// Conexión
var redis = new FullRedis("server=127.0.0.1:6379;password=yourpwd;db=0");

// Operaciones básicas
redis.Set("key", "value");
var val = redis.Get<String>("key");

// Expiración de clave
redis.Set("temp", data, 60); // 60 segundos
```

## Características Principales

- **Protocolo Completo**: RESP2/RESP3, todos los comandos de Redis 2.8~7.x
- **Todas las Estructuras de Datos**: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog
- **Colas de Mensajes**: Simple, confiable (RPOPLPUSH+Ack), diferida, Stream multi-grupo
- **Clúster y Alta Disponibilidad**: Standalone / Sentinel / Cluster, conmutación por error automática
- **Adaptación a Nubes**: Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS
- **Amplio Soporte de Framework**: .NET Framework 4.5 ~ .NET 10+
- **Compatibilidad con Garnet**: Detección automática de Microsoft Garnet / kvrocks, degradación elegante

## Paquetes de Extensión

| Paquete | Descripción |
|-----------|------|
| `NewLife.Redis` | Cliente Redis principal |
| `NewLife.Redis.Extensions` | Inyección de dependencias ASP.NET Core, `IDistributedCache`, `IDataProtection` |

## Documentación

> 📖 Documentación completa (en chino): [Requisitos](Doc/需求文档.md) | [Arquitectura](Doc/架构文档.md) | [Análisis Competitivo](Doc/竞品分析.md)

## Licencia

MIT License © 2002-2026 [Equipo de Desarrollo NewLife](https://newlifex.com)

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [Sitio Oficial](https://newlifex.com)
