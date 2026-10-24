# NewLife.Redis — Cliente Redis de Alto Desempenho

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis` é um componente cliente Redis para .NET **de alto desempenho, alta taxa de transferência e fácil integração**, construído pela equipe NewLife. Desde 2017, opera de forma estável em várias plataformas de produção que lidam com bilhões de registros e alta concorrência, processando **mais de 80 bilhões** de chamadas de comandos por dia.

> 📖 Documentação completa: [Requisitos (中文)](Doc/需求文档.md) | [Arquitetura (中文)](Doc/架构文档.md) | [Lista de Recursos (中文)](Doc/功能清单.md) | [Análise Competitiva (中文)](Doc/竞品分析.md) | [Compatibilidade com Garnet (中文)](Doc/Garnet兼容性.md) | [Compatibilidade com AOT (中文)](Doc/AOT兼容性分析.md)

**Conteúdo**: [Início Rápido](#início-rápido) · [Principais Recursos](#principais-recursos) · [Mapa do Ecossistema](#mapa-do-ecossistema) · [Documentação](#documentação) · [Contribuindo](#contribuindo) · [Equipe](#equipe) · [Licença](#licença)

## Início Rápido

### Instalação

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### Uso básico

**Recomendado: singleton, thread-safe, reutilize durante todo o ciclo de vida da aplicação:**

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

**Modo connection string (recomendado para centros de configuração):**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### Suporte de plataformas

| Item | Cobertura |
| --- | --- |
| Frameworks de destino | Um único pacote: net45, net461, netstandard2.0, netstandard2.1, net8.0; as extensões acrescentam netcoreapp3.1 → net10 |
| Sistemas operacionais e arquiteturas | Windows, Linux, macOS, Android, iOS; x64, arm64, loongarch64, riscv64, s390x |
| NativeAOT | O `net8.0` é distribuído com verificações `IsAotCompatible`; o `AotSmoke/` e a matriz de smoke test da CI cobrem win-x64 / linux-x64 / linux-arm64 / linux-la64 (LA64 via QEMU), com win-arm64, macOS e RIDs musl compilados nativamente. linux-x64 é o gate bloqueante, os demais estão em observação — veja [Compatibilidade com AOT](Doc/AOT兼容性分析.md) |

## Principais Recursos

<details>
<summary><b>Mecanismo Central</b> (7)</summary>

| Recurso | Principais características |
| --- | --- |
| Comprovado em escala | Mais de 200 instâncias Redis em produção, com pico diário de mais de 100 milhões de gravações de objetos de negócio e mais de 80 bilhões de chamadas de comandos. |
| Baixa latência | Uma única ida e volta Get/Set leva de 200 a 600µs, incluindo a rede. |
| Alta taxa de transferência | Pool de conexões integrado (até 100.000 conexões simultâneas), com parser RESP síncrono e batching opcional de auto-pipeline. |
| Nova tentativa automática e failover multi-endereço | Listas de nós separadas por vírgula, failover rápido em erros de rede e janela de blindagem de 10 segundos por padrão. |
| Codificador plugável | JSON por padrão (`RedisJsonEncoder`), extensível para serialização binária via `IPacketEncoder`. |
| Rastreamento APM | Rastreamento distribuído com `ITracer` e contadores de desempenho `PerfCounter`. |
| Zero dependências pesadas | Apenas o ecossistema NewLife (log, configuração, serialização, segurança) sobre a BCL. |

</details>

<details>
<summary><b>Estruturas de Dados</b> (10)</summary>

| Estrutura | Principais características |
| --- | --- |
| String KV + lote | MGET / MSET / INCR / APPEND / GETRANGE e afins. |
| List | LPUSH / RPOP / LRANGE / LMOVE. |
| Hash | HGET / HSET / HMGET / HSCAN. |
| Set | SADD / SMEMBERS / SUNION / SDIFF. |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN. |
| Stack | Wrapper LIFO sobre List. |
| Stream | XADD / XREAD / XGROUP / XACK completos. |
| Geo | GEOADD / GEODIST / GEOSEARCH. |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE. |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD (planejado). |

</details>

<details>
<summary><b>Filas de Mensagens e Pub/Sub</b> (7)</summary>

| Tipo | Principais características |
| --- | --- |
| `RedisQueue<T>` | Fila simples com entrega no máximo uma vez. |
| `RedisReliableQueue<T>` | Entrega pelo menos uma vez com RPOPLPUSH + rollback de Ack. |
| `RedisDelayQueue<T>` | Entrega atrasada suportada por Sorted Set. |
| `RedisStream<T>` | Stream ordenado com grupos de consumidores e XACK. |
| `MultipleConsumerGroupsQueue<T>` | Broadcast para múltiplos grupos de consumidores. |
| `PubSub` + `RedisEventBus` | Publicação/assinatura em tempo real. |
| Bloqueio distribuído RedLock | Algoritmo Redlock multi-instância (em andamento). |

</details>

<details>
<summary><b>Cluster e Alta Disponibilidade</b> (7)</summary>

| Modo | Principais características |
| --- | --- |
| Standalone | Conexão direta com uma única instância, com o menor consumo de recursos. |
| Replicação Master-Replica | Separação de leitura/escrita e failover. |
| Sentinel | Descoberta via Sentinel com troca automática de master. |
| Cluster nativo | Roteamento de slots por CRC16 com redirecionamentos MOVED / ASK. |
| kvrocks | Adapta-se automaticamente ao mecanismo de armazenamento kvrocks. |
| Garnet | Detecta automaticamente o Microsoft Garnet e degrada graciosamente quando um recurso está ausente. |
| Valkey | Detectado a partir do INFO do servidor. |

</details>

<details>
<summary><b>Protocolo e Compatibilidade com Servidores</b> (7)</summary>

| Capacidade | Principais características |
| --- | --- |
| RESP2 | Simple string, error, integer, bulk string e array totalmente implementados. |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error, negociados pelo handshake HELLO (Redis 6.0+). |
| Redis 2.8 – 7.x | Comandos mainstream de todas as versões, incluindo COPY / GETDEL / GETEX / GEOSEARCH a partir da 6.2. |
| Autenticação ACL | Autenticação por nome de usuário e senha no Redis 6.0+. |
| Transações | MULTI / EXEC / WATCH. |
| Scripts Lua | EVAL / EVALSHA. |
| Redis Functions | FCALL / FUNCTION (planejado). |

</details>

<details>
<summary><b>Nuvem, Segurança e Observabilidade</b> (8)</summary>

| Capacidade | Principais características |
| --- | --- |
| Alibaba Cloud KVStore / Tair | Edição padrão, ACL, SSL e orientações para modo proxy, além das estruturas Tair (TairString / TairHash / TairZSet). |
| Tencent Cloud Redis | Usa o ID da instância como nome de usuário (`instanceId:password`). |
| Huawei Cloud DCS | Diferenças entre edições master/standby, SSL e cluster (sem SELECT no modo cluster). |
| TLS/SSL | Versão de protocolo configurável e TLS mútuo com certificados de cliente. |
| Log estruturado | Log do componente (`Log`) e log em nível de protocolo (`ClientLog`). |
| Contadores de desempenho | `PerfCounter` informa ops/s, latência e taxa de erro. |
| Rastreamento APM | Spans `ITracer` em toda a biblioteca, além de `ActivitySource` (net8.0+) para OpenTelemetry. |
| Proteção de connection string | `ProtectedKey` descriptografa senhas embutidas em connection strings. |

</details>

<details>
<summary><b>Plataforma e Integração</b> (5)</summary>

| Capacidade | Principais características |
| --- | --- |
| Múltiplos alvos | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0; os pacotes de extensão cobrem netcoreapp3.1 → net10. |
| Nome forte | Assinado com `newlife.snk` para que os assemblies possam ser recompilados e substituídos. |
| NativeAOT | Verificações estáticas `IsAotCompatible`, o projeto `AotSmoke/` e uma matriz de CI multi-RID. |
| Pacote ASP.NET Core | `NewLife.Redis.Extensions` registra `IDistributedCache` e `FullRedis` via `AddRedisCaching`. |
| DataProtection | `PersistKeysToRedis` persiste as chaves de proteção de dados no Redis. |

</details>

## Mapa do Ecossistema

NewLife é um ecossistema em camadas de projetos open-source versionados de forma independente. O mapa abaixo é compartilhado literalmente por todos os repositórios NewLife; cada projeto aponta para seu repositório e seu pacote NuGet principal.

| Projeto | O que faz | Downloads |
| --- | --- | --- |
| **Base** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Log, configuração, cache, rede, serialização, agendamento e rastreamento APM. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Middleware de dados: tabelas únicas na escala de dezenas de bilhões de linhas, particionamento automático, separação de leitura e escrita, mais de 10 bancos de dados. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | Biblioteca de rede de alto desempenho: 1,77M pps / 14,5 Gbps em uma máquina (TCP 1KB, 2026-10), 4M de conexões TCP persistentes. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Framework de comunicação por protocolo e RPC para aplicações cliente/servidor e dispositivos IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Instala aplicações .NET como daemons do sistema operacional: serviços do Windows e systemd do Linux. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | Modelos de projeto `dotnet new` para console, web, API, serviço, dados e IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Dados e mensagens** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Cliente Redis com latência em microssegundos, milhões de operações por segundo e filas de mensagens integradas. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Cliente RocketMQ totalmente gerenciado para Apache RocketMQ e Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | Cliente e servidor MQTT, incluindo conectividade com o Alibaba Cloud IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Banco de dados híbrido em C#: embarcado ou em rede, relacional, séries temporais, fila de mensagens e KV em um só. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | Driver MySql e camada auxiliar construídos sobre a pilha de dados NewLife. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, IA e distribuído** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Framework de plataforma administrativa rápida: permissões, SSO, servidor OAuth e scaffolding CRUD. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Plataforma de serviços distribuídos: gestão de nós, APM, centro de configuração, registro e centro de publicação. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Plataforma distribuída de tarefas e agendamento de dados em tempo real, com particionamento, repetição e reprocessamento. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Cliente unificado para 46 provedores de LLM, além do ChatAI, uma aplicação web de chat pronta para uso. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrial e IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | Biblioteca padrão de IoT que define contratos de protocolo e abstrações de dispositivos. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Implementação Modbus TCP/RTU/ASCII sobre NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Protocolo Siemens PLC sobre NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Protocolo Omron PLC sobre NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Protocolo Mitsubishi Melsec PLC sobre NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | Cliente OPC para acesso a dados industriais. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | Pilha do protocolo de automação predial BACnet. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Plataforma IoT de código aberto leve, com gestão de gateways e dispositivos. | — |
| **Telemática veicular** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | Protocolo de posicionamento veicular JT/T 808: mais de 70 corpos de mensagem, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | Protocolo plataforma-a-plataforma JT/T 809 para autoridades de transporte. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | Vídeo veicular JT/T 1078: análise RTP, remux para FLV/fMP4 e transmissão ao vivo. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Componentes de domínio** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Um pacote para Excel, Word, PPT, PDF e mais de 15 formatos de documento. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Áudio multiplataforma: codecs, E/S de dispositivos, DSP, contêineres e streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Codecs de vídeo, formatos de contêiner, processamento de frames e streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Interface unificada sobre Baidu, AMap, Tencent e Tianditu, com geocodificação e conversão de coordenadas. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | Geolocalização IPv4 com banco de dados offline mapeado em memória e API online como alternativa. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Ferramentas e produtos** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Caixa de ferramentas do desenvolvedor: rede, porta serial, criptografia, regex, Modbus e MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Sistema auto-hospedado de gerenciamento de código-fonte Git. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Instala automaticamente o runtime .NET correspondente quando iniciado em um host mais antigo. | — |

> Os badges mostram o pacote principal de cada projeto; pacotes de tema, extensão e satélites estão listados nas respectivas páginas do NuGet. Os mais de 30 pacotes não listados aqui (FTP, LoRa, extensões do Office, drivers de protocolo e outros) estão disponíveis na [organização NewLifeX](https://github.com/NewLifeX) e em [newlifex.com](https://newlifex.com).

## Documentação

**NewLife.Redis** oferece aos desenvolvedores .NET acesso completo ao Redis em todo o ciclo de vida:

- **Protocolo completo**: Implementa RESP2, cobre todos os comandos mainstream do Redis 2.8 ao 7.x e suporta todas as estruturas de dados: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog.
- **Filas de mensagens**: Fila simples integrada, **fila confiável (RPOPLPUSH + semântica de Ack)**, fila de atraso e fila Stream com múltiplos grupos de consumidores — prontas para uso, sem reinventar a roda.
- **Cluster e alta disponibilidade**: Suporta Standalone / Master-Replica / Sentinel / Cluster nativo, com failover automático.
- **Adaptadores para provedores de nuvem**: Configuração e adaptação específicas para **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**, cobrindo modo proxy, autenticação ACL, criptografia SSL, formatos de autenticação especiais e outras diferenças.
- **Implementações compatíveis**: Detecta automaticamente Microsoft Garnet, kvrocks e outros servidores compatíveis com Redis, degradando graciosamente com alternativas claras quando um recurso não é suportado.
- **Amplo suporte de frameworks**: Um único pacote cobre **.NET Framework 4.5 até .NET 10+**, adequado para cenários legados de controle industrial, governo, IoT e outros.

### Guias

- [Requisitos (中文)](Doc/需求文档.md): visão, objetivos e requisitos funcionais
- [Arquitetura (中文)](Doc/架构文档.md): camadas, componentes e decisões-chave de design
- [Lista de recursos (中文)](Doc/功能清单.md): status de implementação, testes e comentários de cada recurso
- [Análise competitiva (中文)](Doc/竞品分析.md): comparação com os principais clientes Redis
- [Compatibilidade com Garnet (中文)](Doc/Garnet兼容性.md): escopo suportado e degradação graciosa
- [Compatibilidade com AOT (中文)](Doc/AOT兼容性分析.md): escopo do NativeAOT e conclusões medidas
- [Localização (中文)](Doc/多语言本地化.md): modelo de mensagens para exceções e logs
- [Integração com RedisSwitch (中文)](Doc/RedisSwitch项目说明.md): ferramenta de troca de Redis sem downtime
- [Análise de alocação de GC (中文)](Doc/dotMemory分析Redis的GC分配.md): medições com dotMemory
- [Site oficial](https://newlifex.com/core/redis): página do componente e documentação online

### Suporte a protocolos e versões

#### Suporte ao protocolo RESP

| Protocolo | Status | Descrição |
|----------|--------|-------------|
| RESP2 | ✅ Totalmente suportado | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ Totalmente suportado | Tipos Map / Set / Double / Boolean / Null / Blob Error, negociados pelo handshake HELLO (Redis 6.0+) |

#### Suporte a recursos por versão do Redis

| Versão do Redis | Principais recursos | Status |
|---------------|--------------|--------|
| 2.8 | Comandos básicos, SCAN, HyperLogLog, Pub/Sub | ✅ Totalmente suportado |
| 3.0 | Cluster nativo, roteamento de shards por CRC16 | ✅ Totalmente suportado |
| 3.2 | Dados espaciais GEO | ✅ Totalmente suportado |
| 4.0 | UNLINK (exclusão assíncrona), MODULE | 🔄 Suporte parcial |
| 5.0 | Estrutura de dados Stream (XADD/XREAD/XGROUP/XACK completos) | ✅ Totalmente suportado |
| 6.0 | Controle de acesso ACL, TLS/SSL, LMOVE, RESET | ✅ Totalmente suportado |
| 6.2 | COPY, GETDEL, GETEX, GEOSEARCH | ✅ Comandos principais suportados |
| 7.0 | Functions (FCALL), LMPOP/ZMPOP, Pub/Sub com sharding | 🔄 Planejado |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 Planejado |

### Arquitetura

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

Descrições das principais classes:

| Classe | Papel |
|-------|------|
| `Redis` | Cliente central: pool de conexões, failover multi-nó, pipeline, gerenciamento de configuração |
| `FullRedis` | Cliente aprimorado: roteamento de cluster, prefixo de chave, métodos fábrica de estruturas de dados |
| `RedisClient` | Unidade do protocolo RESP: conexão TCP, codificação/decodificação de comandos (interno, gerenciado pelo pool) |
| `RedisList<T>` etc. | Wrappers genéricos de estruturas de dados com estratégia de codificação unificada |
| `RedisReliableQueue<T>` | Fila de mensagens confiável (RPOPLPUSH + confirmação Ack) |
| `RedisStream<T>` | Fila Stream (Redis 5.0+, modo grupo de consumidores) |
| `IPacketEncoder` | Estratégia de codificação/decodificação plugável |

### Exemplos de uso

Cada exemplo abaixo é executado sobre uma instância `FullRedis` viva criada no [Início Rápido](#início-rápido).

<details>
<summary><b>Operações básicas</b></summary>

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
<summary><b>Estruturas de dados</b></summary>

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

> Observação: a instância base `Redis` suporta apenas operações KV. Use `FullRedis` para coleções avançadas.

</details>

<details>
<summary><b>Pipeline e batching automático</b></summary>

O pipeline reduz significativamente as idas e voltas de rede (RTT) e melhora a taxa de transferência de operações em lote:

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**Modo automático** (recomendado para produção):

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>Filas de mensagens</b></summary>

#### Fila simples

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### Fila confiável (semântica de pelo menos uma vez)

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

#### Fila de atraso

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### Fila Stream (Redis 5.0+, multi-grupo de consumidores)

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
<summary><b>Publicação / Assinatura</b></summary>

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
<summary><b>Cluster e alta disponibilidade</b></summary>

#### Failover automático multi-endereço

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### Detecção automática do modo cluster

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
<summary><b>Adaptadores para provedores de nuvem</b></summary>

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

**Observações gerais para provedores de nuvem:**
- Modo proxy em nuvem pública: mantenha `AutoDetect = false` (padrão) para impedir a detecção automática de nós internos
- Cota de conexões: cada provedor de nuvem e tamanho de instância tem limites de conexão; ajuste o parâmetro `Max` do pool de acordo
- Keepalive TCP: o Huawei Cloud usa por padrão um timeout de inatividade TCP de 300 segundos; defina `Timeout` abaixo desse valor e faça PING periodicamente

</details>

<details>
<summary><b>Serialização e codificadores</b></summary>

Codificador padrão: `RedisJsonEncoder` (host JSON integrado), pronto para uso imediato.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

Tipos primitivos (`String` / `Int32` / `Int64` / `Double` / `Boolean`) são serializados diretamente, sem passar pelo JSON, para máximo desempenho.

</details>

<details>
<summary><b>Segurança e autenticação</b></summary>

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
<summary><b>Observabilidade</b></summary>

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
<summary><b>Pacotes de extensão</b></summary>

`NewLife.Redis.Extensions` oferece integração profunda com ASP.NET Core:

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
<summary><b>Compatibilidade com Garnet</b></summary>

O NewLife.Redis suporta plenamente o servidor compatível com Redis de alto desempenho da Microsoft, o [Garnet](https://github.com/microsoft/garnet), com detecção automática e degradação graciosa.

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| Recurso | Status de suporte no Garnet |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ Totalmente suportado |
| HyperLogLog / Geo | ✅ Totalmente suportado |
| Pub/Sub / Transações / Scripts Lua | ✅ Totalmente suportado |
| Pipeline | ✅ Totalmente suportado |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ Totalmente suportado |
| `RedisReliableQueue<T>` | ✅ Totalmente suportado |
| `RedisStream<T>` | ❌ O Garnet ainda não suporta Stream; lança automaticamente `NotSupportedException` com alternativas sugeridas |

Veja: [Documentação de compatibilidade com Garnet (中文)](Doc/Garnet兼容性.md)

</details>

### Referência

<details>
<summary><b>Status de conclusão dos recursos</b></summary>

> Legenda: ✅ Concluído | 🔄 Planejado | ❌ Explicitamente não planejado

#### Estruturas de dados

| Estrutura | Status | Descrição |
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

#### Filas de mensagens

| Tipo de fila | Status | Semântica de entrega |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | No máximo uma vez |
| `RedisReliableQueue<T>` | ✅ | Pelo menos uma vez (RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | Entrega atrasada (Sorted Set) |
| `RedisStream<T>` | ✅ | Ordenado + Grupo de consumidores + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | Broadcast multi-grupo de consumidores |
| `PubSub` + `RedisEventBus` | ✅ | Push em tempo real |
| Bloqueio distribuído RedLock | 🔄 | Algoritmo Redlock multi-instância |

#### Suporte a cluster

| Modo | Status |
|------|--------|
| Standalone | ✅ |
| Replicação Master-Replica | ✅ |
| Sentinel | ✅ |
| Cluster nativo | ✅ |
| Auto-adaptação kvrocks | ✅ |
| Auto-detecção Garnet | ✅ |
| Auto-detecção Valkey | ✅ |

#### Protocolo e recursos avançados

| Recurso | Status |
|---------|--------|
| Suporte completo a RESP2 | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| Autenticação ACL | ✅ |
| Transações MULTI/EXEC/WATCH | ✅ |
| Scripts Lua EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 Planejado |
| Pipeline (explícito + automático) | ✅ |
| Assinatura por padrão PSUBSCRIBE | 🔄 |
| Exclusão assíncrona UNLINK | 🔄 |

</details>

<details>
<summary><b>Benchmarks de desempenho</b></summary>

Benchmark integrado ao código-fonte (`Samples/Benchmark`), resultados típicos (40 processadores lógicos, Redis 7.0 local):

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

Acionamento manual do benchmark:

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> O desempenho real é afetado pelo RTT da rede / tamanho do valor / complexidade da serialização. Recomenda-se manter valores individuais entre 1–1,4KB.

</details>

<details>
<summary><b>Boas práticas</b></summary>

- **Reutilização como singleton**: `FullRedis` tem um pool de conexões interno; recomenda-se fortemente usar uma única instância durante todo o ciclo de vida da aplicação — não crie `new` a cada uso
- **Design de chaves**: Use `:` como separador hierárquico, por exemplo `user:1001:profile`; use `Prefix` para isolar namespaces em produção
- **Lote primeiro**: Prefira `GetAll/SetAll` a loops de operações em chave única; use `AutoPipeline` para mesclar gravações automaticamente
- **Tamanho do valor**: Mantenha valores entre 1–2KB; divida objetos grandes / comprima / use Hash para armazenamento por campo
- **Consumo confiável**: Use `RedisReliableQueue<T>` em produção em vez de uma List simples, para evitar perda de mensagens
- **Provedores de nuvem**: Mantenha `AutoDetect = false` em nuvens públicas para evitar problemas com endereços de nós internos
- **Monitoramento**: Habilite as estatísticas de `Counter` em produção; habilite `ClientLog` apenas ao depurar, para evitar o custo de log no caminho crítico
- **SSL**: Habilite o transporte criptografado TLS em nuvens públicas e em ambientes de produção

</details>

<details>
<summary><b>Perguntas frequentes</b></summary>

**P: Há suporte a sharding no Redis Cluster?**
R: Sim. O `FullRedis` detecta automaticamente o modo Cluster com `AutoDetect=true`. O roteamento CRC16 e os redirecionamentos MOVED/ASK estão todos implementados. Para o modo proxy de nuvens públicas, mantenha o padrão `AutoDetect=false`.

**P: Como ele se compara ao StackExchange.Redis?**
R: O NewLife.Redis usa um modelo de pool de conexões (em vez do multiplexing do StackExchange) para um comportamento de conexão mais intuitivo. Os wrappers de fila de mensagens integrados (fila confiável / fila de atraso) são um diferencial importante. Ele também suporta .NET Framework 4.5. Veja [Análise Competitiva (中文)](Doc/竞品分析.md).

**P: Como tratar consultas lentas causadas por valores grandes?**
R: Divida as estruturas de dados (por exemplo, Hash por campo) + leituras/gravações em lote + codificador binário + controle de `MaxMessageSize` (limite padrão de 1MB).

**P: E se o pool de conexões esgotar?**
R: O máximo padrão é de 100.000 conexões; cargas normais não o esgotam. O pool é configurável via `Redis.PoolConfig` (`RedisPoolConfig`): mínimo ocioso / capacidade máxima / tempo de limpeza ociosa / tempo de vida máximo / timeout de espera por empréstimo. Conexões ociosas além de `IdleTime` passam por PING proativo ao serem emprestadas; em cenários de troca master-slave ou atualização gradual de proxy, reduza `MaxLifetime` para reciclar conexões mais rápido. Se houver timeouts de espera, verifique vazamentos de conexão (`Dispose` ausente) ou operações que mantêm conexões por tempo excessivo.

**P: Por que as conexões falham no Alibaba Cloud / Tencent Cloud?**
R: Verifique: ① O `AutoDetect` está como `false` (padrão correto)? ② O formato de autenticação está correto (o Tencent Cloud exige `username=instanceId`)? ③ O security group / whitelist libera o IP do cliente? ④ A edição cluster proíbe o uso de SELECT para trocar de banco?

**P: O Garnet suporta todos os recursos?**
R: Operações básicas, List/Hash/Set/ZSet/HLL/Geo e filas são todos suportados. Stream ainda não é suportado e lançará automaticamente `NotSupportedException` com sugestão de usar `RedisQueue<T>` como alternativa.

</details>

<details>
<summary><b>Roteiro</b></summary>

- [x] ~~Suporte ao protocolo RESP3~~ (Concluído)
- [ ] Redis Functions (FCALL/FUNCTION, substituindo scripts Lua)
- [ ] LMPOP / ZMPOP (pop em lote do Redis 7.0)
- [ ] Bloqueio distribuído multi-instância RedLock
- [ ] Operações Bitmap / Bitfield
- [ ] Implementação completa de assinatura por padrão PSUBSCRIBE
- [ ] Exclusão assíncrona UNLINK
- [ ] Adaptador de exportação de métricas Prometheus
- [x] Extensão Alibaba Cloud Tair (TairString / TairHash / TairZSet suportados)
- [ ] Adaptador de mapeamento de endereços de nós para a edição cluster do Huawei Cloud DCS
- [x] ~~Pub/Sub~~ (Concluído)
- [x] ~~RedisStream multi-grupo de consumidores~~ (Concluído)
- [x] ~~Detecção do tipo de servidor Garnet~~ (Concluído)

Feedback e solicitações de recursos são bem-vindos via Issue / PR.

</details>

<details>
<summary><b>Comparação competitiva</b></summary>

| Capacidade | NewLife.Redis | StackExchange.Redis | FreeRedis | Observações |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | Crítico para sistemas legados industriais/governamentais |
| Modelo de conexão | Pool de conexões | Multiplexação | Pool de conexões | Cada um tem seus casos de uso |
| Cobertura de comandos | ~98% | ~95% | ~98% | Todos os comandos documentados, exceto os explicitamente não planejados |
| Wrappers de fila de mensagens integrados | ✅ Completo | ❌ | 🔄 Parcial | Filas confiável/atraso exclusivas |
| Serialização integrada | ✅ | ❌ Manual | ❌ Manual | Reduz código boilerplate |
| Adaptadores especializados para provedores de nuvem | ✅ (Alibaba/Tencent/Huawei) | Genérico | Genérico | Documentação dedicada |
| Adaptador explícito para Garnet | ✅ | ❌ | ❌ | Detecção de tipo + degradação graciosa |
| Adaptador kvrocks | ✅ | ❌ | ❌ | Detecção automática |
| Auto-detecção de Valkey | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| Rastreamento APM | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | Integração nativa com OpenTelemetry |
| RESP3 | ✅ | ✅ | ✅ | Verificado pela suíte de testes RESP3 |

Veja o [Documento de Análise Competitiva (中文)](Doc/竞品分析.md) para detalhes.

</details>

### Notas de versão

- [ChangeLog.md](ChangeLog.md): histórico de versões (chinês)
- [ChangeLog.en.md](ChangeLog.en.md): histórico de versões (inglês)

## Contribuindo

### Compilação e testes

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- Os testes de integração precisam de um Redis ativo: defina `REDIS_CONNECTION` ou inicie um em `127.0.0.1:6379`; caso contrário, os testes marcados com `[RedisFact]` / `[RedisTopologyFact]` são ignorados automaticamente.
- Smoke test do NativeAOT: `AotSmoke/`, CI em `.github/workflows/aot.yml`.
- Benchmarks: `Samples/Benchmark`.

### Estrutura do repositório

| Diretório | Descrição |
| --- | --- |
| `NewLife.Redis/` | Código-fonte do cliente: pool de conexões, codec RESP, estruturas de dados e filas |
| `NewLife.Redis.Extensions/` | Integração com ASP.NET Core: `IDistributedCache`, DI e proteção de dados |
| `NewLife.Extensions.Caching.Redis/` | Pacote de compatibilidade da extensão de cache legada |
| `XUnitTest/` | Suíte de testes xUnit (net10.0) |
| `AotSmoke/` | Projeto de smoke test NativeAOT |
| `Samples/` | Amostras de Benchmark, WebDemo, QueueDemo, RedisSwitch e smoke mobile |
| `Doc/` | Requisitos, arquitetura, lista de recursos e documentos temáticos |

### Lançamento

Envie uma tag com prefixo `v` (por exemplo `v7.0.2026.1024`) para acionar `.github/workflows/publish.yml`. O workflow empacota `NewLife.Redis` e `NewLife.Redis.Extensions`, publica-os no NuGet.org e no GitHub Packages e cria uma GitHub Release com as notas da seção correspondente do `ChangeLog.md`; uma execução manual apenas publica os pacotes.

### Feedback

1. Antes de enviar, garanta que `dotnet build` e os testes relevantes passam, e siga o estilo de código e as convenções de commit do repositório
2. PRs: mantenha as alterações mínimas, adicione os comentários necessários e descrições de teste
3. Issues: informe versão, ambiente de execução e um cenário mínimo de reprodução

Comunidade: grupos QQ 1600800 / 1600838; GitHub Discussions / Issues.

## Equipe

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

Fundada em 2002, a equipe NewLife dedica-se há mais de duas décadas ao ecossistema .NET e de IoT, mantém cerca de 100 projetos de código aberto (34 deles principais) e oferece soluções integradas de software e hardware para os setores de energia, logística, saúde, controle industrial e internet. Seguimos o lema "não há hierarquia no aprendizado — o domínio conquista o respeito".

Os componentes de código aberto já foram baixados <!--nuget-total:start-->6.48M<!--nuget-total:end--> vezes no NuGet (todo o ecossistema, até 2026-10).

Website: https://newlifex.com · Organização de código aberto: https://github.com/NewLifeX

<details>
<summary>Conta oficial no WeChat: 智能大石头 (escaneie para seguir)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## Licença

Licença MIT. Livre para uso comercial / modificação / redistribuição (sem autorização adicional). Basta manter o aviso de copyright.

> Se este documento não cobre o seu caso de uso, fique à vontade para abrir uma Issue — vamos melhorar a documentação juntos!
