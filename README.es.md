# NewLife.Redis — Cliente Redis de Alto Rendimiento

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` es un componente cliente Redis para .NET **de alto rendimiento, alto caudal y fácil integración**, creado por el equipo NewLife. Desde 2017 se ejecuta de forma estable en múltiples plataformas de producción que manejan miles de millones de registros y alta concurrencia, procesando **más de 80 mil millones** de llamadas de comandos al día.

> 📖 Documentación completa: [Requisitos (中文)](Doc/需求文档.md) | [Arquitectura (中文)](Doc/架构文档.md) | [Lista de funcionalidades (中文)](Doc/功能清单.md) | [Análisis competitivo (中文)](Doc/竞品分析.md) | [Compatibilidad con Garnet (中文)](Doc/Garnet兼容性.md) | [Compatibilidad con AOT (中文)](Doc/AOT兼容性分析.md)

**Contenido**: [Inicio rápido](#inicio-rápido) · [Características principales](#características-principales) · [Mapa del ecosistema](#mapa-del-ecosistema) · [Documentación](#documentación) · [Contribución](#contribución) · [Equipo](#equipo) · [Licencia](#licencia)

## Inicio rápido

### Instalación

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### Uso básico

**Recomendado: singleton, thread-safe, reutilizar durante toda la vida de la aplicación:**

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

**Modo cadena de conexión (recomendado para centros de configuración):**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### Compatibilidad de plataformas

| Elemento | Cobertura |
| --- | --- |
| Frameworks de destino | Un solo paquete: net45, net461, netstandard2.0, netstandard2.1, net8.0; las extensiones añaden netcoreapp3.1 → net10 |
| Sistema operativo y arquitectura | Windows, Linux, macOS, Android, iOS; x64, arm64, loongarch64, riscv64, s390x |
| NativeAOT | `net8.0` incluye comprobaciones `IsAotCompatible`; `AotSmoke/` y la matriz de pruebas de humo de CI cubren win-x64 / linux-x64 / linux-arm64 / linux-la64 (LA64 mediante QEMU), con los RID win-arm64, macOS y musl compilados de forma nativa. linux-x64 es la puerta bloqueante, el resto está en observación — véase [Compatibilidad con AOT](Doc/AOT兼容性分析.md) |

## Características principales

<details>
<summary><b>Motor principal</b> (7)</summary>

| Característica | Características clave |
| --- | --- |
| Probado a gran escala | Más de 200 instancias Redis en producción, con un pico diario de más de 100 M de escrituras de objetos de negocio y más de 80 mil millones de llamadas de comandos. |
| Baja latencia | Un único viaje de ida y vuelta Get/Set tarda 200–600 µs incluida la red. |
| Alto caudal | Grupo de conexiones integrado (hasta 100.000 concurrentes) con analizador RESP síncrono y auto-pipeline opcional por lotes. |
| Reintentos automáticos y conmutación por error multidirección | Listas de nodos separadas por comas, conmutación por error rápida ante errores de red y una ventana de protección de 10 segundos por defecto. |
| Codificador conectable | JSON por defecto (`RedisJsonEncoder`), extensible a serialización binaria mediante `IPacketEncoder`. |
| Trazabilidad APM | Trazabilidad distribuida con `ITracer` más contadores de rendimiento `PerfCounter`. |
| Sin dependencias pesadas | Solo el ecosistema NewLife (registro, configuración, serialización, seguridad) sobre la BCL. |

</details>

<details>
<summary><b>Estructuras de datos</b> (10)</summary>

| Estructura | Características clave |
| --- | --- |
| String KV + lote | MGET / MSET / INCR / APPEND / GETRANGE y similares. |
| List | LPUSH / RPOP / LRANGE / LMOVE. |
| Hash | HGET / HSET / HMGET / HSCAN. |
| Set | SADD / SMEMBERS / SUNION / SDIFF. |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN. |
| Stack | Envoltorio LIFO sobre List. |
| Stream | XADD / XREAD / XGROUP / XACK completos. |
| Geo | GEOADD / GEODIST / GEOSEARCH. |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE. |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD (planificado). |

</details>

<details>
<summary><b>Colas de mensajes y Pub/Sub</b> (7)</summary>

| Tipo | Características clave |
| --- | --- |
| `RedisQueue<T>` | Cola simple con entrega como máximo una vez. |
| `RedisReliableQueue<T>` | Entrega al menos una vez con RPOPLPUSH + reversión mediante Ack. |
| `RedisDelayQueue<T>` | Entrega diferida respaldada por un Sorted Set. |
| `RedisStream<T>` | Stream ordenado con grupos de consumidores y XACK. |
| `MultipleConsumerGroupsQueue<T>` | Difusión a múltiples grupos de consumidores. |
| `PubSub` + `RedisEventBus` | Publicación/suscripción en tiempo real. |
| Bloqueo distribuido RedLock | Algoritmo Redlock multiinstancia (en curso). |

</details>

<details>
<summary><b>Clustering y alta disponibilidad</b> (7)</summary>

| Modo | Características clave |
| --- | --- |
| Autónomo | Conexión directa a una sola instancia con la huella más pequeña. |
| Replicación maestro-réplica | Separación de lectura/escritura y conmutación por error. |
| Sentinel | Descubrimiento de Sentinel con cambio automático de maestro. |
| Cluster nativo | Enrutamiento de slots CRC16 con redirección MOVED / ASK. |
| kvrocks | Se adapta automáticamente al motor de almacenamiento kvrocks. |
| Garnet | Detecta automáticamente Microsoft Garnet y degrada de forma controlada cuando falta una funcionalidad. |
| Valkey | Detectado a partir del INFO del servidor. |

</details>

<details>
<summary><b>Protocolos y compatibilidad con servidores</b> (7)</summary>

| Capacidad | Características clave |
| --- | --- |
| RESP2 | Simple string, error, integer, bulk string y array totalmente implementados. |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error, negociados mediante el handshake HELLO (Redis 6.0+). |
| Redis 2.8 – 7.x | Comandos principales de todas las versiones, incluidos COPY / GETDEL / GETEX / GEOSEARCH desde 6.2. |
| Autenticación ACL | Autenticación con nombre de usuario y contraseña en Redis 6.0+. |
| Transacciones | MULTI / EXEC / WATCH. |
| Scripts Lua | EVAL / EVALSHA. |
| Redis Functions | FCALL / FUNCTION (planificado). |

</details>

<details>
<summary><b>Nube, seguridad y observabilidad</b> (8)</summary>

| Capacidad | Características clave |
| --- | --- |
| Alibaba Cloud KVStore / Tair | Guía para edición estándar, ACL, SSL y modo proxy, además de las estructuras Tair (TairString / TairHash / TairZSet). |
| Tencent Cloud Redis | Usa el ID de instancia como nombre de usuario (`instanceId:password`). |
| Huawei Cloud DCS | Diferencias de las ediciones maestro/respaldo, SSL y clúster (sin SELECT en modo clúster). |
| TLS/SSL | Versión de protocolo configurable y TLS mutuo con certificados de cliente. |
| Registro estructurado | Registro del componente (`Log`) y registro a nivel de protocolo (`ClientLog`). |
| Contadores de rendimiento | `PerfCounter` informa de ops/s, latencia y tasa de errores. |
| Trazabilidad APM | Spans de `ITracer` en todas partes más un `ActivitySource` (net8.0+) para OpenTelemetry. |
| Protección de cadenas de conexión | `ProtectedKey` descifra las contraseñas incrustadas en las cadenas de conexión. |

</details>

<details>
<summary><b>Plataforma e integración</b> (5)</summary>

| Capacidad | Características clave |
| --- | --- |
| Multiobjetivo | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0; los paquetes de extensión cubren netcoreapp3.1 → net10. |
| Nombre seguro | Firmado con `newlife.snk` para que los ensamblados puedan recompilarse y sustituirse. |
| NativeAOT | Comprobaciones estáticas `IsAotCompatible`, el proyecto `AotSmoke/` y una matriz de CI multi-RID. |
| Paquete ASP.NET Core | `NewLife.Redis.Extensions` registra `IDistributedCache` y `FullRedis` mediante `AddRedisCaching`. |
| DataProtection | `PersistKeysToRedis` persiste las claves de protección de datos en Redis. |

</details>

## Mapa del ecosistema

NewLife es un ecosistema por capas de proyectos de código abierto con versionado independiente. El mapa siguiente se comparte textualmente en todos los repositorios de NewLife; cada proyecto enlaza a su repositorio y a su paquete NuGet principal.

| Proyecto | Qué hace | Descargas |
| --- | --- | --- |
| **Fundación** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Registro de logs, configuración, caché, red, serialización, programación de tareas y seguimiento APM. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Middleware de datos: tablas únicas a escala de decenas de miles de millones, particionado automático, separación lectura/escritura y más de 10 bases de datos. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Biblioteca de red de alto rendimiento: 1,77 M pps / 14,5 Gbps en una máquina (TCP 1KB, 2026-10), 4 M conexiones TCP persistentes. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Framework de comunicación por protocolo y RPC para aplicaciones cliente/servidor y dispositivos IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Instala aplicaciones .NET como demonios del sistema operativo: servicios de Windows y systemd de Linux. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Plantillas de proyecto `dotnet new` para consola, web, API, servicio, datos e IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Datos y mensajería** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Cliente Redis con latencia de microsegundos, millones de operaciones por segundo y colas de mensajes integradas. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Cliente RocketMQ totalmente administrado para Apache RocketMQ y Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Cliente y servidor MQTT, incluida la conectividad con Alibaba Cloud IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Base de datos híbrida en C#: embebida o en red, relacional, series temporales, cola de mensajes y KV en una sola. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Controlador y capa de ayuda de MySql construidos sobre la pila de datos de NewLife. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, IA y distribuido** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Framework rápido para plataformas de administración: permisos, SSO, servidor OAuth y andamiaje CRUD. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Plataforma distribuida de servicios: gestión de nodos, APM, centro de configuración, registro y centro de despliegues. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plataforma de tareas distribuidas y programación de datos en tiempo real con particionado, reintentos y relleno histórico. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Cliente unificado para 46 proveedores de LLM más ChatAI, una aplicación web de chat lista para usar. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrial e IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Biblioteca estándar de IoT que define contratos de protocolo y abstracciones de dispositivos. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Implementación de Modbus TCP/RTU/ASCII sobre NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Protocolo para PLC Siemens sobre NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Protocolo para PLC Omron sobre NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Protocolo para PLC Mitsubishi Melsec sobre NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Cliente OPC para el acceso a datos industriales. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Pila de protocolo BACnet para la automatización de edificios. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Plataforma IoT de código abierto y ligera con gestión de pasarelas y dispositivos. | — |
| **Telemática vehicular** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | Protocolo de posicionamiento vehicular JT/T 808: más de 70 cuerpos de mensaje, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | Protocolo plataforma a plataforma JT/T 809 para autoridades de transporte. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | Vídeo a bordo JT/T 1078: análisis RTP, remultiplexado FLV/fMP4 y transmisión en directo. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Componentes de dominio** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Un solo paquete para Excel, Word, PPT, PDF y más de 15 formatos de documento. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Audio multiplataforma: códecs, E/S de dispositivos, DSP, contenedores y streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Códecs de vídeo, formatos de contenedor, procesamiento de fotogramas y streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Interfaz unificada sobre Baidu, AMap, Tencent y Tianditu con geocodificación y conversión de coordenadas. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Geolocalización IPv4 con una base de datos offline mapeada en memoria y una API en línea de respaldo. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Herramientas y productos** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Caja de herramientas del desarrollador: red, puerto serie, criptografía, expresiones regulares, Modbus y MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Sistema de gestión de código fuente Git autoalojado. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Instala automáticamente el runtime de .NET correspondiente cuando se ejecuta desde un host más antiguo. | — |

> Las insignias muestran el paquete principal de cada proyecto; los paquetes de temas, extensiones y satélites figuran en sus páginas de NuGet. Los más de 30 paquetes no listados aquí (FTP, LoRa, extensiones de Office, controladores de protocolo, etc.) están disponibles en la [organización NewLifeX](https://github.com/NewLifeX) y en [newlifex.com](https://newlifex.com).

## Documentación

**NewLife.Redis** ofrece a los desarrolladores .NET acceso a Redis durante todo el ciclo de vida:

- **Protocolo completo**: implementa RESP2, cubre todos los comandos principales de Redis 2.8 a 7.x y admite todas las estructuras de datos: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog.
- **Colas de mensajes**: cola simple integrada, **cola confiable (RPOPLPUSH + semántica Ack)**, cola de retardo y cola Stream con múltiples grupos de consumidores, listas para usar sin reinventar la rueda.
- **Clustering y alta disponibilidad**: admite Standalone / Maestro-Réplica / Sentinel / Cluster nativo, con conmutación por error automática.
- **Adaptadores para proveedores de nube**: configuración y adaptación específicas para **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**, que cubren el modo proxy, la autenticación ACL, el cifrado SSL, los formatos de autenticación especiales y otras diferencias.
- **Implementaciones compatibles**: detecta automáticamente Microsoft Garnet, kvrocks y otros servidores compatibles con Redis, y degrada de forma controlada con alternativas claras cuando una funcionalidad no es compatible.
- **Amplio soporte de frameworks**: un solo paquete cubre **.NET Framework 4.5 hasta .NET 10+**, adecuado para control industrial, gobierno, IoT y otros escenarios heredados.

### Guías

- [Requisitos (中文)](Doc/需求文档.md): visión, objetivos y requisitos funcionales
- [Arquitectura (中文)](Doc/架构文档.md): capas, componentes y decisiones clave de diseño
- [Lista de funcionalidades (中文)](Doc/功能清单.md): estado de implementación, pruebas y comentarios de cada funcionalidad
- [Análisis competitivo (中文)](Doc/竞品分析.md): comparación con los clientes Redis más populares
- [Compatibilidad con Garnet (中文)](Doc/Garnet兼容性.md): alcance soportado y degradación controlada
- [Compatibilidad con AOT (中文)](Doc/AOT兼容性分析.md): alcance de NativeAOT y conclusiones medidas
- [Localización (中文)](Doc/多语言本地化.md): modelo de mensajes para excepciones y registros
- [Integración con RedisSwitch (中文)](Doc/RedisSwitch项目说明.md): herramienta de conmutación de Redis sin tiempo de inactividad
- [Análisis de asignaciones del GC (中文)](Doc/dotMemory分析Redis的GC分配.md): mediciones con dotMemory
- [Sitio oficial](https://newlifex.com/core/redis): página del componente y documentación en línea

### Soporte de protocolos y versiones

#### Soporte del protocolo RESP

| Protocolo | Estado | Descripción |
|----------|--------|-------------|
| RESP2 | ✅ Totalmente compatible | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ Totalmente compatible | Tipos Map / Set / Double / Boolean / Null / Blob Error, negociados mediante el handshake HELLO (Redis 6.0+) |

#### Soporte de funcionalidades por versión de Redis

| Versión de Redis | Funcionalidades clave | Estado |
|---------------|--------------|--------|
| 2.8 | Comandos básicos, SCAN, HyperLogLog, Pub/Sub | ✅ Totalmente compatible |
| 3.0 | Cluster nativo, enrutamiento de shards CRC16 | ✅ Totalmente compatible |
| 3.2 | Datos espaciales GEO | ✅ Totalmente compatible |
| 4.0 | Eliminación asíncrona UNLINK, MODULE | 🔄 Soporte parcial |
| 5.0 | Estructura de datos Stream (XADD/XREAD/XGROUP/XACK completos) | ✅ Totalmente compatible |
| 6.0 | Control de acceso ACL, TLS/SSL, LMOVE, RESET | ✅ Totalmente compatible |
| 6.2 | COPY, GETDEL, GETEX, GEOSEARCH | ✅ Comandos principales soportados |
| 7.0 | Functions (FCALL), LMPOP/ZMPOP, Pub/Sub con sharding | 🔄 Planificado |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 Planificado |

### Arquitectura

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

Descripción de las clases principales:

| Clase | Función |
|-------|------|
| `Redis` | Cliente principal: grupo de conexiones, conmutación por error multinodo, pipeline, gestión de configuración |
| `FullRedis` | Cliente mejorado: enrutamiento de clúster, prefijo de claves, métodos de fábrica de estructuras de datos |
| `RedisClient` | Unidad del protocolo RESP: conexión TCP, codificación/decodificación de comandos (interno, gestionado por el pool) |
| `RedisList<T>` etc. | Envoltorios genéricos de estructuras de datos con estrategia de codificación unificada |
| `RedisReliableQueue<T>` | Cola de mensajes confiable (RPOPLPUSH + confirmación Ack) |
| `RedisStream<T>` | Cola Stream (Redis 5.0+, modo de grupos de consumidores) |
| `IPacketEncoder` | Estrategia de codificación/decodificación conectable |

### Ejemplos de uso

Todos los ejemplos siguientes se ejecutan sobre una instancia `FullRedis` real creada en [Inicio rápido](#inicio-rápido).

<details>
<summary><b>Operaciones básicas</b></summary>

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
<summary><b>Estructuras de datos</b></summary>

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

> Nota: la instancia base `Redis` solo admite operaciones KV. Utilice `FullRedis` para colecciones avanzadas.

</details>

<details>
<summary><b>Pipeline y batching automático</b></summary>

El pipeline reduce significativamente los viajes de ida y vuelta por red (RTT) y mejora el rendimiento de las operaciones masivas:

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**Modo automático** (recomendado para producción):

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>Colas de mensajes</b></summary>

#### Cola simple

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### Cola confiable (semántica de al menos una entrega)

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

#### Cola de retardo

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### Cola Stream (Redis 5.0+, múltiples grupos de consumidores)

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
<summary><b>Publicación / Suscripción</b></summary>

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
<summary><b>Clustering y alta disponibilidad</b></summary>

#### Conmutación por error automática multidirección

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### Detección automática del modo de clúster

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
<summary><b>Adaptadores para proveedores de nube</b></summary>

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

**Notas generales sobre proveedores de nube:**
- Modo proxy de nube pública: mantenga `AutoDetect = false` (valor predeterminado) para evitar la detección automática de nodos internos
- Cuota de conexiones: cada proveedor de nube y tamaño de instancia tiene límites de conexiones; ajuste el parámetro `Max` del grupo en consecuencia
- Keepalive TCP: Huawei Cloud aplica por defecto un tiempo de inactividad TCP de 300 segundos; configure `Timeout` por debajo de ese valor y realice PING periódicamente

</details>

<details>
<summary><b>Serialización y codificadores</b></summary>

Codificador predeterminado: `RedisJsonEncoder` (host JSON integrado), listo para usar sin configuración adicional.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

Los tipos primitivos (`String` / `Int32` / `Int64` / `Double` / `Boolean`) se serializan directamente sin pasar por JSON para obtener el máximo rendimiento.

</details>

<details>
<summary><b>Seguridad y autenticación</b></summary>

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
<summary><b>Observabilidad</b></summary>

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
<summary><b>Paquetes de extensión</b></summary>

`NewLife.Redis.Extensions` proporciona una integración profunda con ASP.NET Core:

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
<summary><b>Compatibilidad con Garnet</b></summary>

NewLife.Redis es totalmente compatible con el servidor de alto rendimiento compatible con Redis [Garnet](https://github.com/microsoft/garnet) de Microsoft, con detección automática y degradación controlada.

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| Funcionalidad | Estado de soporte en Garnet |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ Totalmente compatible |
| HyperLogLog / Geo | ✅ Totalmente compatible |
| Pub/Sub / Transacciones / Scripts Lua | ✅ Totalmente compatible |
| Pipeline | ✅ Totalmente compatible |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ Totalmente compatible |
| `RedisReliableQueue<T>` | ✅ Totalmente compatible |
| `RedisStream<T>` | ❌ Garnet aún no admite Stream; se lanza automáticamente `NotSupportedException` con alternativas sugeridas |

Consulte: [Documentación de compatibilidad con Garnet (中文)](Doc/Garnet兼容性.md)

</details>

### Referencia

<details>
<summary><b>Estado de finalización de funcionalidades</b></summary>

> Leyenda: ✅ Completado | 🔄 Planificado | ❌ Explícitamente no planificado

#### Estructuras de datos

| Estructura | Estado | Descripción |
|-----------|--------|-------------|
| String KV + lote | ✅ | MGET/MSET/INCR/APPEND/GETRANGE, etc. |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE, etc. |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN, etc. |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF, etc. |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN, etc. |
| Stream | ✅ | XADD/XREAD/XGROUP/XACK completos |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH, etc. |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

#### Colas de mensajes

| Tipo de cola | Estado | Semántica de entrega |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | Como máximo una vez |
| `RedisReliableQueue<T>` | ✅ | Al menos una vez (RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | Entrega diferida (Sorted Set) |
| `RedisStream<T>` | ✅ | Ordenada + grupo de consumidores + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | Difusión a múltiples grupos de consumidores |
| `PubSub` + `RedisEventBus` | ✅ | Push en tiempo real |
| Bloqueo distribuido RedLock | 🔄 | Algoritmo Redlock multiinstancia |

#### Soporte de clúster

| Modo | Estado |
|------|--------|
| Autónomo | ✅ |
| Replicación maestro-réplica | ✅ |
| Sentinel | ✅ |
| Cluster nativo | ✅ |
| Auto-adaptación a kvrocks | ✅ |
| Detección automática de Garnet | ✅ |
| Detección automática de Valkey | ✅ |

#### Protocolo y funcionalidades avanzadas

| Funcionalidad | Estado |
|---------|--------|
| Soporte completo de RESP2 | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| Autenticación ACL | ✅ |
| Transacciones MULTI/EXEC/WATCH | ✅ |
| Scripts Lua EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 Planificado |
| Pipeline (explícito + automático) | ✅ |
| Suscripción por patrón PSUBSCRIBE | 🔄 |
| Eliminación asíncrona UNLINK | 🔄 |

</details>

<details>
<summary><b>Benchmarks de rendimiento</b></summary>

Benchmark integrado en el código fuente (`Samples/Benchmark`), resultados típicos (40 procesadores lógicos, Redis 7.0 local):

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

Ejecución manual del benchmark:

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> El rendimiento real depende del RTT de red, el tamaño de los valores y la complejidad de la serialización. Se recomienda mantener los valores individuales entre 1 y 1,4 KB.

</details>

<details>
<summary><b>Buenas prácticas</b></summary>

- **Reutilización como singleton**: `FullRedis` tiene un grupo de conexiones interno; se recomienda encarecidamente usar una sola instancia durante toda la vida de la aplicación, no haga `new` cada vez
- **Diseño de claves**: use `:` como separador jerárquico, por ejemplo `user:1001:profile`; use `Prefix` para aislar espacios de nombres en producción
- **Priorice el lote**: prefiera `GetAll/SetAll` a las operaciones de clave única en bucle; use `AutoPipeline` para fusionar automáticamente las operaciones de escritura
- **Tamaño de los valores**: mantenga los valores entre 1 y 2 KB; divida los objetos grandes, comprímalos o use Hash para almacenamiento por campos
- **Consumo confiable**: en producción use `RedisReliableQueue<T>` en lugar de una List simple para evitar la pérdida de mensajes
- **Proveedores de nube**: mantenga `AutoDetect = false` en nubes públicas para evitar problemas con las direcciones internas de los nodos
- **Monitorización**: habilite las estadísticas de `Counter` en producción; active `ClientLog` solo al depurar para evitar el coste de registro en la ruta caliente
- **SSL**: habilite el transporte cifrado TLS en nubes públicas y en entornos de producción

</details>

<details>
<summary><b>Preguntas frecuentes</b></summary>

**P: ¿Admite el sharding de Redis Cluster?**
R: Sí. `FullRedis` detecta automáticamente el modo Cluster con `AutoDetect=true`. El enrutamiento CRC16 y las redirecciones MOVED/ASK están implementados. Para el modo proxy de nube pública, mantenga el valor predeterminado `AutoDetect=false`.

**P: ¿Cómo se compara con StackExchange.Redis?**
R: NewLife.Redis usa un modelo de grupo de conexiones (frente a la multiplexación de StackExchange) que hace que el comportamiento de las conexiones sea más intuitivo. Los envoltorios de colas de mensajes integrados (cola confiable / cola de retardo) son un diferenciador clave. También es compatible con .NET Framework 4.5. Consulte el [Análisis competitivo (中文)](Doc/竞品分析.md).

**P: ¿Cómo tratar las consultas lentas provocadas por valores grandes?**
R: Divida las estructuras de datos (por ejemplo, Hash por campo) + lecturas/escrituras por lotes + codificador binario + controle `MaxMessageSize` (límite predeterminado de 1 MB).

**P: ¿Qué ocurre si se agota el grupo de conexiones?**
R: El máximo predeterminado es de 100.000 conexiones; las cargas de trabajo normales no lo agotarán. El grupo se configura mediante `Redis.PoolConfig` (`RedisPoolConfig`): mínimo de conexiones inactivas / capacidad máxima / tiempo de limpieza de inactivas / vida máxima / tiempo de espera de préstamo. Las conexiones inactivas que superan `IdleTime` se verifican proactivamente con PING al ser prestadas; para escenarios de conmutación maestro-esclavo o actualización progresiva de proxies, reduzca `MaxLifetime` para reciclar conexiones más rápido. Si observa agotamientos del tiempo de espera, compruebe si hay fugas de conexiones (falta de `Dispose`) o operaciones que retienen conexiones durante períodos excesivamente largos.

**P: ¿Por qué fallan las conexiones en Alibaba Cloud / Tencent Cloud?**
R: Compruebe: ① ¿Está `AutoDetect` en `false` (el valor predeterminado correcto)? ② ¿Es correcto el formato de autenticación (Tencent Cloud requiere `username=instanceId`)? ③ ¿El grupo de seguridad de red / la lista blanca permiten la IP del cliente? ④ ¿La edición clúster prohíbe cambiar de base de datos con SELECT?

**P: ¿Garnet admite todas las funcionalidades?**
R: Las operaciones básicas, List/Hash/Set/ZSet/HLL/Geo y las colas son totalmente compatibles. Stream aún no es compatible y lanzará automáticamente `NotSupportedException` con la sugerencia de usar `RedisQueue<T>` como alternativa.

</details>

<details>
<summary><b>Hoja de ruta</b></summary>

- [x] ~~Compatibilidad con el protocolo RESP3~~ (Completado)
- [ ] Redis Functions (FCALL/FUNCTION, en sustitución de los scripts Lua)
- [ ] LMPOP / ZMPOP (extracción por lotes de Redis 7.0)
- [ ] Bloqueo distribuido RedLock multiinstancia
- [ ] Operaciones Bitmap / Bitfield
- [ ] Implementación completa de la suscripción por patrón PSUBSCRIBE
- [ ] Eliminación asíncrona UNLINK
- [ ] Adaptador de exportación de métricas Prometheus
- [x] Extensión Tair de Alibaba Cloud (compatible con TairString / TairHash / TairZSet)
- [ ] Adaptador de mapeo de direcciones de nodos para la edición clúster de Huawei Cloud DCS
- [x] ~~Pub/Sub~~ (Completado)
- [x] ~~Grupos de consumidores múltiples de RedisStream~~ (Completado)
- [x] ~~Detección del tipo de servidor Garnet~~ (Completado)

Agradecemos los comentarios y solicitudes de funcionalidades mediante Issue / PR.

</details>

<details>
<summary><b>Comparación competitiva</b></summary>

| Capacidad | NewLife.Redis | StackExchange.Redis | FreeRedis | Notas |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | Crítico para sistemas heredados industriales y gubernamentales |
| Modelo de conexión | Grupo de conexiones | Multiplexación | Grupo de conexiones | Cada uno tiene sus casos de uso |
| Cobertura de comandos | ~98% | ~95% | ~98% | Todos los comandos documentados excepto los explícitamente no planificados |
| Envoltorios de colas de mensajes integrados | ✅ Completos | ❌ | 🔄 Parciales | Colas confiables/de retardo únicas |
| Serialización integrada | ✅ | ❌ Manual | ❌ Manual | Reduce el código repetitivo |
| Adaptadores especializados para proveedores de nube | ✅ (Alibaba/Tencent/Huawei) | Genéricos | Genéricos | Documentación dedicada |
| Adaptador explícito para Garnet | ✅ | ❌ | ❌ | Detección de tipo + degradación controlada |
| Adaptador para kvrocks | ✅ | ❌ | ❌ | Detección automática |
| Detección automática de Valkey | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| Trazabilidad APM | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | Integración nativa con OpenTelemetry |
| RESP3 | ✅ | ✅ | ✅ | Verificado con la suite de pruebas RESP3 |

Consulte el [documento de análisis competitivo (中文)](Doc/竞品分析.md) para más detalles.

</details>

### Notas de versión

- [ChangeLog.md](ChangeLog.md): historial de versiones (chino)
- [ChangeLog.en.md](ChangeLog.en.md): historial de versiones (inglés)

## Contribución

### Compilación y pruebas

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- Las pruebas de integración necesitan un Redis en ejecución: configure `REDIS_CONNECTION` o inicie uno en `127.0.0.1:6379`; de lo contrario, las pruebas marcadas con `[RedisFact]` / `[RedisTopologyFact]` se omiten automáticamente.
- Prueba de humo de NativeAOT: `AotSmoke/`, CI en `.github/workflows/aot.yml`.
- Benchmarks: `Samples/Benchmark`.

### Estructura del repositorio

| Directorio | Descripción |
| --- | --- |
| `NewLife.Redis/` | Código fuente del cliente: grupo de conexiones, códec RESP, estructuras de datos y colas |
| `NewLife.Redis.Extensions/` | Integración con ASP.NET Core: `IDistributedCache`, DI y protección de datos |
| `NewLife.Extensions.Caching.Redis/` | Paquete de compatibilidad de la extensión de caché heredada |
| `XUnitTest/` | Suite de pruebas xUnit (net10.0) |
| `AotSmoke/` | Proyecto de prueba de humo de NativeAOT |
| `Samples/` | Ejemplos de Benchmark, WebDemo, QueueDemo, RedisSwitch y pruebas de humo móviles |
| `Doc/` | Documentos de requisitos, arquitectura, lista de funcionalidades y temas específicos |

### Publicación

Empuje una etiqueta con prefijo `v` (por ejemplo `v6.7.2026.1024`) para activar `.github/workflows/publish.yml`. El flujo de trabajo empaqueta `NewLife.Redis` y `NewLife.Redis.Extensions`, los publica en NuGet.org y GitHub Packages y crea una GitHub Release con las notas de la sección correspondiente de `ChangeLog.md`; una ejecución manual solo publica los paquetes.

### Comentarios

1. Antes de enviar, asegúrese de que `dotnet build` y las pruebas relevantes pasen, y siga el estilo de código y las convenciones de commit del repositorio
2. PR: mantenga los cambios mínimos, añada los comentarios necesarios y descripciones de pruebas
3. Issues: proporcione la versión, el entorno de ejecución y un escenario de reproducción mínimo

Comunidad: grupos de QQ 1600800 / 1600838; GitHub Discussions / Issues.

## Equipo

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

NewLife se fundó en 2002 y lleva más de dos décadas dedicado al ecosistema .NET e IoT, mantiene cerca de 100 proyectos de código abierto (34 de ellos principales) y ofrece soluciones integradas de software y hardware para los sectores de energía, logística, salud, control industrial e Internet. Nos guía el lema «en el aprendizaje no hay antigüedad: quien domina es maestro».

Los componentes de código abierto acumulan <!--nuget-total:start-->6.48M<!--nuget-total:end--> descargas en NuGet (todo el ecosistema, a fecha de 2026-10).

Sitio web: https://newlifex.com · Organización de código abierto: https://github.com/NewLifeX

<details>
<summary>Cuenta oficial de WeChat: 智能大石头 (escanea para seguir)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## Licencia

Licencia MIT. Uso comercial, modificación y redistribución libres (no se requiere autorización adicional). Solo debe conservar el aviso de copyright.

> Si este documento no cubre su caso de uso, no dude en abrir un Issue; mejoraremos la documentación juntos.
