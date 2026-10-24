# NewLife.Redis — 고성능 Redis 클라이언트 컴포넌트

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [العربية](README.ar.md)
<!-- lang:end -->

`NewLife.Redis`는 NewLife 팀이 만든 **고성능·고처리량·쉬운 통합**을 지향하는 .NET Redis 클라이언트 컴포넌트입니다. 2017년부터 수십억 건의 레코드와 높은 동시성을 다루는 여러 프로덕션 플랫폼에서 안정적으로 운영되어 왔으며, 현재 **하루 800억 회 이상**의 명령 호출을 처리합니다.

> 📖 전체 문서: [요구사항 (中文)](Doc/需求文档.md) | [아키텍처 (中文)](Doc/架构文档.md) | [기능 목록 (中文)](Doc/功能清单.md) | [경쟁 분석 (中文)](Doc/竞品分析.md) | [Garnet 호환성 (中文)](Doc/Garnet兼容性.md) | [AOT 호환성 (中文)](Doc/AOT兼容性分析.md)

**목차**: [빠른 시작](#빠른-시작) · [주요 기능](#주요-기능) · [생태계 지도](#생태계-지도) · [문서](#문서) · [기여](#기여) · [팀](#팀) · [라이선스](#라이선스)

## 빠른 시작

### 설치

```bash
# Stable release
dotnet add package NewLife.Redis
# ASP.NET Core extensions
dotnet add package NewLife.Redis.Extensions
# Development release (includes prereleases)
dotnet add package NewLife.Redis --prerelease
```

### 기본 사용법

**권장: 싱글턴으로 생성해 스레드 안전하게, 애플리케이션 수명 주기 동안 재사용하세요:**

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

**연결 문자열 방식(구성 센터에 권장):**

```csharp
var rds = FullRedis.Create("server=127.0.0.1:6379;password=pass;db=0;timeout=3000");
```

### 플랫폼 지원

| 항목 | 지원 범위 |
| --- | --- |
| 대상 프레임워크 | 단일 패키지: net45, net461, netstandard2.0, netstandard2.1, net8.0; 확장 패키지는 netcoreapp3.1 → net10 추가 지원 |
| 운영체제 및 아키텍처 | Windows, Linux, macOS, Android, iOS; x64, arm64, loongarch64, riscv64, s390x |
| NativeAOT | `net8.0`은 `IsAotCompatible` 검사와 함께 제공됩니다. `AotSmoke/`와 CI 스모크 매트릭스가 win-x64 / linux-x64 / linux-arm64 / linux-la64(LA64는 QEMU 경유)를 커버하며, win-arm64, macOS 및 musl RID는 네이티브로 컴파일됩니다. linux-x64가 차단 게이트이고 나머지는 관찰 대상입니다 — [AOT 호환성](Doc/AOT兼容性分析.md) 참조 |

## 주요 기능

<details>
<summary><b>핵심 엔진</b> (7)</summary>

| 기능 | 핵심 특징 |
| --- | --- |
| 대규모 실전 검증 | 프로덕션에서 200개 이상의 Redis 인스턴스를 운영하며, 일일 피크 기준 1억 건 이상의 비즈니스 객체 쓰기와 800억 건 이상의 명령 호출을 처리합니다. |
| 낮은 지연 시간 | 단일 Get/Set 왕복은 네트워크를 포함해 200–600µs입니다. |
| 높은 처리량 | 최대 100,000 동시 연결을 지원하는 내장 연결 풀, 동기 RESP 파서, 선택적 자동 파이프라인 배치. |
| 자동 재시도 및 다중 주소 장애 조치 | 콤마로 구분된 노드 목록, 네트워크 오류 시 빠른 장애 조치, 기본 10초의 차단(shield) 시간. |
| 플러그형 인코더 | 기본은 JSON(`RedisJsonEncoder`)이며 `IPacketEncoder`를 통해 이진 직렬화로 확장할 수 있습니다. |
| APM 추적 | `ITracer` 분산 추적과 `PerfCounter` 성능 카운터. |
| 무거운 의존성 없음 | BCL 위에 NewLife 생태계(로깅, 구성, 직렬화, 보안)만 사용합니다. |

</details>

<details>
<summary><b>데이터 구조</b> (10)</summary>

| 구조 | 핵심 특징 |
| --- | --- |
| String KV + 배치 | MGET / MSET / INCR / APPEND / GETRANGE 등. |
| List | LPUSH / RPOP / LRANGE / LMOVE. |
| Hash | HGET / HSET / HMGET / HSCAN. |
| Set | SADD / SMEMBERS / SUNION / SDIFF. |
| Sorted Set | ZADD / ZRANGE / ZPOPMIN / ZSCAN. |
| Stack | List 위에 구현한 LIFO 래퍼. |
| Stream | XADD / XREAD / XGROUP / XACK 전체 지원. |
| Geo | GEOADD / GEODIST / GEOSEARCH. |
| HyperLogLog | PFADD / PFCOUNT / PFMERGE. |
| Bitmap / Bitfield | SETBIT / BITCOUNT / BITFIELD(계획됨). |

</details>

<details>
<summary><b>메시지 큐와 Pub/Sub</b> (7)</summary>

| 유형 | 핵심 특징 |
| --- | --- |
| `RedisQueue<T>` | 최대 1회 전달되는 단순 큐. |
| `RedisReliableQueue<T>` | RPOPLPUSH + Ack 롤백을 통한 최소 1회 전달. |
| `RedisDelayQueue<T>` | Sorted Set 기반의 지연 전달. |
| `RedisStream<T>` | 컨슈머 그룹과 XACK를 지원하는 순서 보장 스트림. |
| `MultipleConsumerGroupsQueue<T>` | 여러 컨슈머 그룹으로 브로드캐스트. |
| `PubSub` + `RedisEventBus` | 실시간 발행/구독. |
| RedLock 분산 잠금 | 다중 인스턴스 Redlock 알고리즘(진행 중). |

</details>

<details>
<summary><b>클러스터링과 고가용성</b> (7)</summary>

| 모드 | 핵심 특징 |
| --- | --- |
| Standalone | 리소스 사용이 가장 적은 단일 인스턴스 직접 연결. |
| Master-Replica 복제 | 읽기/쓰기 분리와 장애 조치. |
| Sentinel | Sentinel 탐색과 자동 마스터 전환. |
| Native Cluster | MOVED / ASK 리다이렉션을 지원하는 CRC16 슬롯 라우팅. |
| kvrocks | kvrocks 스토리지 엔진에 자동 적응. |
| Garnet | Microsoft Garnet을 자동 감지하고 기능이 없을 때 우아하게 성능을 저하합니다. |
| Valkey | 서버 INFO에서 감지. |

</details>

<details>
<summary><b>프로토콜 및 서버 호환성</b> (7)</summary>

| 기능 | 핵심 특징 |
| --- | --- |
| RESP2 | Simple String, Error, Integer, Bulk String, Array를 완전히 구현. |
| RESP3 | Map / Set / Double / Boolean / Null / Blob Error, HELLO 핸드셰이크로 협상(Redis 6.0+). |
| Redis 2.8 – 7.x | 모든 버전의 주류 명령을 지원하며, 6.2의 COPY / GETDEL / GETEX / GEOSEARCH 포함. |
| ACL 인증 | Redis 6.0+ 사용자 이름 + 비밀번호 인증. |
| 트랜잭션 | MULTI / EXEC / WATCH. |
| Lua 스크립트 | EVAL / EVALSHA. |
| Redis Functions | FCALL / FUNCTION(계획됨). |

</details>

<details>
<summary><b>클라우드, 보안 및 관측성</b> (8)</summary>

| 기능 | 핵심 특징 |
| --- | --- |
| Alibaba Cloud KVStore / Tair | 표준 에디션, ACL, SSL 및 프록시 모드 가이드와 Tair 구조(TairString / TairHash / TairZSet). |
| Tencent Cloud Redis | 인스턴스 ID를 사용자 이름으로 사용합니다(`instanceId:password`). |
| Huawei Cloud DCS | 마스터/스탠바이, SSL, 클러스터 에디션 차이(클러스터 모드에서는 SELECT 미지원). |
| TLS/SSL | 프로토콜 버전 설정과 클라이언트 인증서 기반 상호 TLS 지원. |
| 구조화 로깅 | 컴포넌트 로그(`Log`)와 프로토콜 수준 로그(`ClientLog`). |
| 성능 카운터 | `PerfCounter`가 ops/s, 지연 시간, 오류율을 보고합니다. |
| APM 추적 | 곳곳에 `ITracer` 스팬을 두고, OpenTelemetry용 `ActivitySource`(net8.0+)도 제공합니다. |
| 연결 문자열 보호 | `ProtectedKey`가 연결 문자열에 포함된 비밀번호를 복호화합니다. |

</details>

<details>
<summary><b>플랫폼 및 통합</b> (5)</summary>

| 기능 | 핵심 특징 |
| --- | --- |
| 멀티 타기팅 | net45 / net461 / netstandard2.0 / netstandard2.1 / net8.0; 확장 패키지는 netcoreapp3.1 → net10을 커버합니다. |
| 강력한 이름(Strong Name) | `newlife.snk`로 서명되어 어셈블리를 다시 빌드해 교체할 수 있습니다. |
| NativeAOT | `IsAotCompatible` 정적 검사, `AotSmoke/` 프로젝트, 다중 RID CI 매트릭스. |
| ASP.NET Core 패키지 | `NewLife.Redis.Extensions`가 `AddRedisCaching`을 통해 `IDistributedCache`와 `FullRedis`를 등록합니다. |
| DataProtection | `PersistKeysToRedis`가 데이터 보호 키를 Redis에 저장합니다. |

</details>

## 생태계 지도

NewLife는 각자 독립적으로 버전이 관리되는 오픈소스 프로젝트들로 이루어진 계층형 생태계입니다. 아래 지도는 모든 NewLife 저장소가 동일한 내용으로 공유하며, 각 프로젝트는 해당 저장소와 기본 NuGet 패키지로 연결됩니다.

| 프로젝트 | 역할 | 다운로드 |
| --- | --- | --- |
| **기반** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 로깅, 구성, 캐시, 네트워크, 직렬화, 스케줄링, APM 추적. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 데이터 미들웨어: 단일 테이블 수백억 건, 자동 분할 테이블, 읽기/쓰기 분리, 10여 종 데이터베이스 지원. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 고성능 네트워크 라이브러리: 단일 노드 177만 pps, 14.5 Gbps(TCP 1KB, 2026-10 실측), 400만 Tcp 장기 연결. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | CS 애플리케이션과 IoT 장치를 위한 프로토콜 통신 및 RPC 프레임워크. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | .NET 애플리케이션을 운영체제 데몬으로 설치: Windows 서비스와 Linux systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | 콘솔, Web, API, 서비스, 데이터, IoT용 `dotnet new` 프로젝트 템플릿. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **데이터 및 메시징** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 클라이언트: 마이크로초 수준 지연, 백만 수준 처리량, 메시지 큐 내장. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Apache RocketMQ와 알리바바 클라우드 메시지 큐를 지원하는 순수 관리형 RocketMQ 클라이언트. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 클라이언트와 서버, 알리바바 클라우드 IoT 연결 포함. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 하이브리드 데이터베이스: 임베디드/네트워크 두 모드, 관계형, 시계열, 메시지 큐, KV를 하나로. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | NewLife 데이터 스택 위에 구축한 MySql 드라이버와 도우미 계층. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI 및 분산** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 빠른 관리 플랫폼 프레임워크: 권한, SSO, OAuth 서버, CRUD 스캐폴딩. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 분산 서비스 플랫폼: 노드 관리, APM, 구성 센터, 등록 센터, 배포 센터. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 샤딩, 재시도, 백필을 지원하는 분산 작업 및 실시간 데이터 스케줄링 플랫폼. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 46개 LLM 공급자를 통합 연결하는 클라이언트와 바로 사용할 수 있는 ChatAI 웹 채팅 애플리케이션. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **산업 및 IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 프로토콜 계약과 장치 추상을 정의하는 IoT 표준 라이브러리. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | NewLife.IoT 기반으로 구현한 Modbus TCP/RTU/ASCII. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | NewLife.IoT 기반으로 구현한 지멘스 PLC 프로토콜. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | NewLife.IoT 기반으로 구현한 오므론 PLC 프로토콜. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | NewLife.IoT 기반으로 구현한 미쓰비시 Melsec PLC 프로토콜. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | 산업 데이터 수집용 OPC 클라이언트. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 빌딩 자동화 프로토콜 스택. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 게이트웨이와 장치 관리를 갖춘 경량 오픈소스 IoT 플랫폼. | — |
| **차량 텔레매틱스** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 차량 위치 프로토콜: 70개 이상 메시지 본문, 2011/2013/2019 지원. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | 운송 관리 부서를 위한 JT/T 809 플랫폼 간 프로토콜. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 차량용 비디오: RTP 파싱, FLV/fMP4 리먹싱, 라이브 스트리밍. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **도메인 컴포넌트** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | Excel, Word, PPT, PDF 등 15개 이상 문서 형식을 하나의 패키지로. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 크로스 플랫폼 오디오: 코덱, 장치 IO, DSP, 컨테이너, 스트리밍. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 비디오 코덱, 컨테이너 형식, 프레임 처리, 스트리밍. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 바이두/고덕/텐센트/톈디투 지도 통합 인터페이스, 지오코딩과 좌표 변환 지원. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 위치 정보: 오프라인 메모리 매핑 데이터베이스와 온라인 API 이중 소스. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **도구 및 제품** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 개발자 도구 모음: 네트워크, 시리얼 포트, 암호화, 정규식, Modbus, MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 셀프 호스팅 Git 소스 코드 관리 시스템. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 구버전 호스트에서 시작할 때 일치하는 .NET 런타임을 자동으로 설치합니다. | — |

> 배지는 각 프로젝트의 기본 패키지를 나타냅니다. 테마, 확장, 위성 패키지는 해당 NuGet 페이지에서 확인하세요. 여기에 나열하지 않은 30여 개 패키지(FTP, LoRa, Office 확장, 각종 프로토콜 드라이버 등)는 [NewLifeX 조직](https://github.com/NewLifeX)과 [newlifex.com](https://newlifex.com)에서 확인할 수 있습니다.

## 문서

**NewLife.Redis**는 .NET 개발자에게 전 생애 주기에 걸친 Redis 접근 기능을 제공합니다:

- **완전한 프로토콜**: RESP2를 구현하고 Redis 2.8부터 7.x까지 모든 주류 명령을 지원하며, String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog 등 모든 데이터 구조를 다룹니다.
- **메시지 큐**: 단순 큐, **신뢰성 큐(RPOPLPUSH + Ack 시맨틱)**, 지연 큐, Stream 멀티 컨슈머 그룹 큐를 기본 제공합니다. 바로 사용할 수 있으므로 바퀴를 다시 발명할 필요가 없습니다.
- **클러스터링 및 고가용성**: Standalone / Master-Replica / Sentinel / Native Cluster를 지원하며 자동 장애 조치를 제공합니다.
- **클라우드 사업자 어댑터**: **Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS**에 특화된 구성과 적응을 제공하며, 프록시 모드, ACL 인증, SSL 암호화, 특수 인증 형식 등의 차이를 다룹니다.
- **호환 구현체**: Microsoft Garnet, kvrocks 및 기타 Redis 호환 서버를 자동 감지하고, 기능이 지원되지 않을 때는 명확한 대안과 함께 우아하게 성능을 저하합니다.
- **폭넓은 프레임워크 지원**: 단일 패키지로 **.NET Framework 4.5부터 .NET 10+까지** 지원하여 산업 제어, 공공, IoT 등 레거시 시나리오에 적합합니다.

### 가이드

- [요구사항 (中文)](Doc/需求文档.md): 비전, 목표 및 기능 요구사항
- [아키텍처 (中文)](Doc/架构文档.md): 계층, 컴포넌트 및 핵심 설계 결정
- [기능 목록 (中文)](Doc/功能清单.md): 각 기능의 구현, 테스트, 주석 상태
- [경쟁 분석 (中文)](Doc/竞品分析.md): 주요 Redis 클라이언트와의 비교
- [Garnet 호환성 (中文)](Doc/Garnet兼容性.md): 지원 범위와 우아한 성능 저하
- [AOT 호환성 (中文)](Doc/AOT兼容性分析.md): NativeAOT 범위와 측정 결과
- [다국어 현지화 (中文)](Doc/多语言本地化.md): 예외와 로그의 메시지 모델
- [RedisSwitch 통합 (中文)](Doc/RedisSwitch项目说明.md): 무중단 Redis 전환 도구
- [GC 할당 분석 (中文)](Doc/dotMemory分析Redis的GC分配.md): dotMemory 측정
- [공식 사이트](https://newlifex.com/core/redis): 컴포넌트 페이지와 온라인 문서

### 프로토콜 및 버전 지원

#### RESP 프로토콜 지원

| 프로토콜 | 상태 | 설명 |
|----------|--------|-------------|
| RESP2 | ✅ 완전 지원 | Simple String / Error / Integer / Bulk String / Array |
| RESP3 | ✅ 완전 지원 | Map / Set / Double / Boolean / Null / Blob Error 타입, HELLO 핸드셰이크로 협상(Redis 6.0+) |

#### Redis 버전별 기능 지원

| Redis 버전 | 주요 기능 | 상태 |
|---------------|--------------|--------|
| 2.8 | 기본 명령, SCAN, HyperLogLog, Pub/Sub | ✅ 완전 지원 |
| 3.0 | Native Cluster, CRC16 샤드 라우팅 | ✅ 완전 지원 |
| 3.2 | GEO 공간 데이터 | ✅ 완전 지원 |
| 4.0 | UNLINK 비동기 삭제, MODULE | 🔄 부분 지원 |
| 5.0 | Stream 데이터 구조(XADD/XREAD/XGROUP/XACK 전체) | ✅ 완전 지원 |
| 6.0 | ACL 접근 제어, TLS/SSL, LMOVE, RESET | ✅ 완전 지원 |
| 6.2 | COPY, GETDEL, GETEX, GEOSEARCH | ✅ 핵심 명령 지원 |
| 7.0 | Functions(FCALL), LMPOP/ZMPOP, 샤드 Pub/Sub | 🔄 계획됨 |
| 7.2 | SSUBSCRIBE / SPUBLISH | 🔄 계획됨 |

### 아키텍처

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

핵심 클래스 설명:

| 클래스 | 역할 |
|-------|------|
| `Redis` | 핵심 클라이언트: 연결 풀, 다중 노드 장애 조치, 파이프라인, 구성 관리 |
| `FullRedis` | 확장 클라이언트: 클러스터 라우팅, 키 접두사, 데이터 구조 팩토리 메서드 |
| `RedisClient` | RESP 프로토콜 단위: TCP 연결, 명령 인코딩/디코딩(내부용, 풀에서 관리) |
| `RedisList<T>` 등 | 통일된 인코딩 전략을 사용하는 제네릭 데이터 구조 래퍼 |
| `RedisReliableQueue<T>` | 신뢰성 메시지 큐(RPOPLPUSH + Ack 확인) |
| `RedisStream<T>` | Stream 큐(Redis 5.0+, 컨슈머 그룹 모드) |
| `IPacketEncoder` | 플러그형 인코딩/디코딩 전략 |

### 사용 예제

아래 모든 예제는 [빠른 시작](#빠른-시작)에서 생성한 실제 `FullRedis` 인스턴스를 대상으로 실행됩니다.

<details>
<summary><b>기본 작업</b></summary>

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
<summary><b>데이터 구조</b></summary>

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

> 참고: 기본 `Redis` 인스턴스는 KV 작업만 지원합니다. 고급 컬렉션은 `FullRedis`를 사용하세요.

</details>

<details>
<summary><b>파이프라인과 자동 배치</b></summary>

파이프라인은 네트워크 왕복(RTT)을 크게 줄여 대량 작업의 처리량을 높입니다:

```csharp
// Explicit pipeline
rds.StartPipeline();
for (var i = 0; i < 1000; i++) rds.Set($"p:{i}", i);
var results = rds.StopPipeline(true);
```

**자동 모드**(프로덕션 권장):

```csharp
rds.AutoPipeline = 100;    // Auto-commit when 100 write operations accumulate
rds.FullPipeline = true;   // Both reads and writes enter the pipeline (throughput-first scenarios)
```

</details>

<details>
<summary><b>메시지 큐</b></summary>

#### 단순 큐

```csharp
var queue = rds.GetQueue<OrderMsg>("orders");

// Producer
queue.Add(new OrderMsg { OrderId = "12345", Amount = 99.9m });

// Consumer (blocking wait; timeout=0 means block indefinitely)
var msg = queue.TakeOne(timeout: 30);
```

#### 신뢰성 큐(최소 1회 시맨틱)

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

#### 지연 큐

```csharp
var delay = rds.GetDelayQueue<OrderMsg>("orders:delay");

// Available for consumption after 60 seconds
delay.Add(new OrderMsg { OrderId = "12345" }, delay: 60);

// Consume (auto-waits for due messages)
var msg = delay.TakeOne(timeout: 5);
```

#### Stream 큐(Redis 5.0+, 멀티 컨슈머 그룹)

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
<summary><b>발행 / 구독</b></summary>

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
<summary><b>클러스터링과 고가용성</b></summary>

#### 다중 주소 자동 장애 조치

```csharp
// Comma-separated addresses; auto-failover on network errors;
// switches back to the primary node after 60 seconds
var rds = new FullRedis("10.0.0.1:6379,10.0.0.2:6379,10.0.0.3:6379", "pass", 0)
{
    ShieldingTime = 10,   // Shield unavailable nodes for 10 seconds (default)
    Retry = 3,            // Retry count (default)
};
```

#### 클러스터 모드 자동 감지

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
<summary><b>클라우드 사업자 어댑터</b></summary>

#### 알리바바 클라우드 KVStore / Tair

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

#### 텐센트 클라우드 Redis

```csharp
// Tencent Cloud standard/cluster edition: use instance ID as UserName (AUTH instanceId:password format)
var rds = new FullRedis("xxxxxx.redis.tencentcds.com:6379", "instanceId", "YourPassword", 0);

// Or via connection string
var rds = FullRedis.Create("server=xxxxxx.redis.tencentcds.com:6379;username=instanceId;password=YourPassword;db=0");
```

#### 화웨이 클라우드 DCS

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

**클라우드 사업자 공통 유의사항:**
- 공용 클라우드 프록시 모드: 내부 노드 자동 감지를 막기 위해 `AutoDetect = false`(기본값)를 유지하세요
- 연결 할당량: 클라우드 사업자와 인스턴스 사양마다 연결 수 제한이 있으므로 풀의 `Max` 매개변수를 그에 맞게 설정하세요
- TCP keepalive: 화웨이 클라우드의 기본 TCP 유휴 타임아웃은 300초이므로 `Timeout`을 그보다 작게 설정하고 주기적으로 PING을 보내세요

</details>

<details>
<summary><b>직렬화와 인코더</b></summary>

기본 인코더는 `RedisJsonEncoder`(내장 JSON 호스트)이며 별도 설정 없이 바로 사용할 수 있습니다.

```csharp
// Inspect current JSON configuration
var jsonHost = RedisJsonEncoder.GetJsonHost();

// Switch to a custom encoder (e.g., MessagePack)
rds.Encoder = new MyMessagePackEncoder();
```

원시 타입(`String` / `Int32` / `Int64` / `Double` / `Boolean`)은 최고 성능을 위해 JSON을 거치지 않고 직접 직렬화됩니다.

</details>

<details>
<summary><b>보안과 인증</b></summary>

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
<summary><b>관측성</b></summary>

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
<summary><b>확장 패키지</b></summary>

`NewLife.Redis.Extensions`는 ASP.NET Core와의 긴밀한 통합을 제공합니다:

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
<summary><b>Garnet 호환성</b></summary>

NewLife.Redis는 Microsoft의 [Garnet](https://github.com/microsoft/garnet) 고성능 Redis 호환 서버를 완전히 지원하며, 자동 감지와 우아한 성능 저하를 제공합니다.

```csharp
var redis = new FullRedis("127.0.0.1:7000", null, 0);

// Auto-detect server type
if (redis.IsGarnet)
    Console.WriteLine("Connected to Garnet server");

Console.WriteLine($"Server type: {redis.ServerType}");  // Redis / Garnet / Unknown
Console.WriteLine($"Server version: {redis.Version}");
```

| 기능 | Garnet 지원 상태 |
|---------|----------------------|
| String / List / Hash / Set / Sorted Set | ✅ 완전 지원 |
| HyperLogLog / Geo | ✅ 완전 지원 |
| Pub/Sub / 트랜잭션 / Lua 스크립트 | ✅ 완전 지원 |
| Pipeline | ✅ 완전 지원 |
| `RedisQueue<T>` / `RedisDelayQueue<T>` | ✅ 완전 지원 |
| `RedisReliableQueue<T>` | ✅ 완전 지원 |
| `RedisStream<T>` | ❌ Garnet은 아직 Stream을 지원하지 않으며, 자동으로 `NotSupportedException`을 발생시키고 대안을 제시합니다 |

참고: [Garnet 호환성 문서 (中文)](Doc/Garnet兼容性.md)

</details>

### 참고 자료

<details>
<summary><b>기능 완성 상태</b></summary>

> 범례: ✅ 완료 | 🔄 계획됨 | ❌ 명시적으로 계획 없음

#### 데이터 구조

| 구조 | 상태 | 설명 |
|-----------|--------|-------------|
| String KV + 배치 | ✅ | MGET/MSET/INCR/APPEND/GETRANGE 등 |
| List | ✅ | LPUSH/RPOP/LRANGE/LMOVE 등 |
| Hash | ✅ | HGET/HSET/HMGET/HSCAN 등 |
| Set | ✅ | SADD/SMEMBERS/SUNION/SDIFF 등 |
| Sorted Set | ✅ | ZADD/ZRANGE/ZPOPMIN/ZSCAN 등 |
| Stream | ✅ | XADD/XREAD/XGROUP/XACK 전체 |
| Geo | ✅ | GEOADD/GEODIST/GEOSEARCH 등 |
| HyperLogLog | ✅ | PFADD/PFCOUNT/PFMERGE |
| Bitmap / Bitfield | 🔄 | SETBIT/BITCOUNT/BITFIELD |

#### 메시지 큐

| 큐 유형 | 상태 | 전달 시맨틱 |
|------------|--------|-------------------|
| `RedisQueue<T>` | ✅ | 최대 1회 |
| `RedisReliableQueue<T>` | ✅ | 최소 1회(RPOPLPUSH + Ack) |
| `RedisDelayQueue<T>` | ✅ | 지연 전달(Sorted Set) |
| `RedisStream<T>` | ✅ | 순서 보장 + 컨슈머 그룹 + XACK |
| `MultipleConsumerGroupsQueue<T>` | ✅ | 멀티 컨슈머 그룹 브로드캐스트 |
| `PubSub` + `RedisEventBus` | ✅ | 실시간 푸시 |
| RedLock 분산 잠금 | 🔄 | 다중 인스턴스 Redlock 알고리즘 |

#### 클러스터 지원

| 모드 | 상태 |
|------|--------|
| Standalone | ✅ |
| Master-Replica 복제 | ✅ |
| Sentinel | ✅ |
| Native Cluster | ✅ |
| kvrocks 자동 적응 | ✅ |
| Garnet 자동 감지 | ✅ |
| Valkey 자동 감지 | ✅ |

#### 프로토콜 및 고급 기능

| 기능 | 상태 |
|---------|--------|
| RESP2 완전 지원 | ✅ |
| RESP3 | ✅ |
| SSL/TLS | ✅ |
| ACL 인증 | ✅ |
| 트랜잭션 MULTI/EXEC/WATCH | ✅ |
| Lua 스크립트 EVAL/EVALSHA | ✅ |
| Redis Functions | 🔄 계획됨 |
| Pipeline(명시적 + 자동) | ✅ |
| 패턴 구독 PSUBSCRIBE | 🔄 |
| UNLINK 비동기 삭제 | 🔄 |

</details>

<details>
<summary><b>성능 벤치마크</b></summary>

소스에 내장된 벤치마크(`Samples/Benchmark`)의 대표 결과(논리 프로세서 40개, 로컬 Redis 7.0):

```
Write 400,000 items | 4 threads  | ~576,368 ops/s
Read  800,000 items | 8 threads  | ~647,249 ops/s
Delete 800,000 items | 8 threads | ~1,011,378 ops/s
Batch Write (pipeline) | 4 threads | ~1,800,000 ops/s
```

수동 벤치 실행:

```csharp
rds.Bench(rand: true, batch: 100);  // Random keys + batch optimization
```

> 실제 성능은 네트워크 RTT / 값 크기 / 직렬화 복잡도의 영향을 받습니다. 개별 값은 1–1.4KB 범위로 유지하는 것을 권장합니다.

</details>

<details>
<summary><b>모범 사례</b></summary>

- **싱글턴 재사용**: `FullRedis`는 내부에 연결 풀을 두고 있으므로 애플리케이션 수명 주기 전체에 걸쳐 단일 인스턴스를 사용하는 것을 강력히 권장합니다 — 매번 `new`하지 마세요
- **키 설계**: `:`를 계층 구분자로 사용하세요(예: `user:1001:profile`). 프로덕션에서는 `Prefix`로 네임스페이스를 격리하세요
- **배치 우선**: 단일 키 작업을 반복하기보다 `GetAll/SetAll`을 선호하고, `AutoPipeline`으로 쓰기 작업을 자동 병합하세요
- **값 크기**: 값을 1–2KB 이내로 유지하고, 큰 객체는 분할하거나 압축하거나 Hash를 사용해 필드 단위로 저장하세요
- **신뢰성 있는 소비**: 메시지 유실을 피하려면 프로덕션에서는 단순 List 대신 `RedisReliableQueue<T>`를 사용하세요
- **클라우드 사업자**: 공용 클라우드에서는 내부 노드 주소 문제를 피하기 위해 `AutoDetect = false`를 유지하세요
- **모니터링**: 프로덕션에서는 `Counter` 통계를 켜고, `ClientLog`는 디버깅할 때만 켜서 핫 패스 로깅 오버헤드를 피하세요
- **SSL**: 공용 클라우드와 프로덕션 환경에서는 TLS 암호화 전송을 사용하세요

</details>

<details>
<summary><b>자주 묻는 질문(FAQ)</b></summary>

**Q: Redis Cluster 샤딩을 지원하나요?**
A: 지원합니다. `FullRedis`는 `AutoDetect=true`일 때 Cluster 모드를 자동 감지합니다. CRC16 라우팅과 MOVED/ASK 리다이렉션도 모두 구현되어 있습니다. 공용 클라우드 프록시 모드에서는 기본값인 `AutoDetect=false`를 유지하세요.

**Q: StackExchange.Redis와 비교해 어떤가요?**
A: NewLife.Redis는 StackExchange의 멀티플렉싱 방식과 달리 연결 풀 모델을 사용하여 연결 동작이 더 직관적입니다. 내장 메시지 큐 래퍼(신뢰성 큐 / 지연 큐)는 중요한 차별점입니다. .NET Framework 4.5도 지원합니다. [경쟁 분석 (中文)](Doc/竞品分析.md)을 참고하세요.

**Q: 큰 값으로 인한 느린 쿼리는 어떻게 처리하나요?**
A: 데이터 구조를 분할하고(예: 필드 단위 Hash), 배치 읽기/쓰기와 바이너리 인코더를 사용하며, `MaxMessageSize`(기본 1MB 제한)를 조정하세요.

**Q: 연결 풀이 고갈되면 어떻게 되나요?**
A: 기본 최대 연결 수는 100,000개이므로 일반적인 워크로드에서는 고갈되지 않습니다. 풀은 `Redis.PoolConfig`(`RedisPoolConfig`)로 구성할 수 있으며, 최소 유휴 수 / 최대 용량 / 유휴 정리 시간 / 최대 수명 / 대여 대기 타임아웃을 지정할 수 있습니다. 대여 시 `IdleTime`을 초과한 유휴 연결은 능동적으로 PING 검사를 수행합니다. 마스터-슬레이브 전환이나 프록시 롤링 업데이트 시나리오에서는 `MaxLifetime`을 낮춰 연결을 더 빨리 재활용하세요. 대기 타임아웃이 발생하면 연결 누수(`Dispose` 누락)나 연결을 지나치게 오래 점유하는 작업이 있는지 확인하세요.

**Q: 알리바바 클라우드 / 텐센트 클라우드에서 연결이 실패하는 이유는 무엇인가요?**
A: 다음을 확인하세요: ① `AutoDetect`가 `false`(올바른 기본값)인가? ② 인증 형식이 올바른가(텐센트 클라우드는 `username=instanceId` 필요)? ③ 네트워크 보안 그룹 / 화이트리스트가 클라이언트 IP를 허용하는가? ④ 클러스터 에디션이 SELECT로 DB를 전환하는 것을 금지하지 않는가?

**Q: Garnet은 모든 기능을 지원하나요?**
A: 기본 작업, List/Hash/Set/ZSet/HLL/Geo, 큐는 모두 지원됩니다. Stream은 아직 지원되지 않으며 자동으로 `NotSupportedException`을 발생시키고 `RedisQueue<T>`를 대안으로 사용할 것을 제안합니다.

</details>

<details>
<summary><b>로드맵</b></summary>

- [x] ~~RESP3 프로토콜 지원~~ (완료)
- [ ] Redis Functions(FCALL/FUNCTION, Lua 스크립트 대체)
- [ ] LMPOP / ZMPOP(Redis 7.0 배치 팝)
- [ ] RedLock 다중 인스턴스 분산 잠금
- [ ] Bitmap / Bitfield 작업
- [ ] 패턴 구독 PSUBSCRIBE 전체 구현
- [ ] UNLINK 비동기 삭제
- [ ] Prometheus 메트릭 내보내기 어댑터
- [x] 알리바바 클라우드 Tair 확장(TairString / TairHash / TairZSet 지원)
- [ ] 화웨이 클라우드 DCS 클러스터 에디션 노드 주소 매핑 어댑터
- [x] ~~Pub/Sub~~ (완료)
- [x] ~~RedisStream 멀티 컨슈머 그룹~~ (완료)
- [x] ~~Garnet 서버 유형 감지~~ (완료)

Issue / PR로 피드백과 기능 요청을 환영합니다.

</details>

<details>
<summary><b>경쟁 제품 비교</b></summary>

| 기능 | NewLife.Redis | StackExchange.Redis | FreeRedis | 비고 |
|------------|--------------|---------------------|-----------|-------|
| .NET Framework 4.5 | ✅ | ❌ (4.6.1+) | ❌ | 산업/공공 레거시 시스템에 중요 |
| 연결 모델 | 연결 풀 | 멀티플렉싱 | 연결 풀 | 각각 장단점이 있음 |
| 명령 커버리지 | ~98% | ~95% | ~98% | 명시적으로 계획하지 않은 명령을 제외한 모든 문서화된 명령 |
| 내장 메시지 큐 래퍼 | ✅ 완전 | ❌ | 🔄 부분 | 독보적인 신뢰성/지연 큐 |
| 내장 직렬화 | ✅ | ❌ 수동 | ❌ 수동 | 보일러플레이트 코드 감소 |
| 클라우드 사업자 전용 어댑터 | ✅ (알리바바/텐센트/화웨이) | 범용 | 범용 | 전용 문서 제공 |
| Garnet 전용 어댑터 | ✅ | ❌ | ❌ | 유형 감지 + 우아한 성능 저하 |
| kvrocks 어댑터 | ✅ | ❌ | ❌ | 자동 감지 |
| Valkey 자동 감지 | ✅ | ❌ | ❌ | INFO `server=valkey` / `valkey_version` |
| APM 추적 | ITracer + ActivitySource (net8.0+) | ActivitySource | ❌ | 네이티브 OpenTelemetry 통합 |
| RESP3 | ✅ | ✅ | ✅ | RESP3 테스트 스위트로 검증 |

자세한 내용은 [경쟁 분석 문서 (中文)](Doc/竞品分析.md)를 참고하세요.

</details>

### 릴리스 노트

- [ChangeLog.md](ChangeLog.md): 버전 이력(중국어)
- [ChangeLog.en.md](ChangeLog.en.md): 버전 이력(영어)

## 기여

### 빌드 및 테스트

```bash
dotnet build NewLife.Redis/NewLife.Redis.csproj -c Release
dotnet test XUnitTest -c Release
```

- 통합 테스트에는 실제 Redis가 필요합니다: `REDIS_CONNECTION`을 설정하거나 `127.0.0.1:6379`에서 Redis를 실행하세요. 그렇지 않으면 `[RedisFact]` / `[RedisTopologyFact]`로 표시된 테스트는 자동으로 건너뜁니다.
- NativeAOT 스모크: `AotSmoke/`, CI는 `.github/workflows/aot.yml`.
- 벤치마크: `Samples/Benchmark`.

### 저장소 구조

| 디렉터리 | 설명 |
| --- | --- |
| `NewLife.Redis/` | 클라이언트 소스: 연결 풀, RESP 코덱, 데이터 구조 및 큐 |
| `NewLife.Redis.Extensions/` | ASP.NET Core 통합: `IDistributedCache`, DI 및 데이터 보호 |
| `NewLife.Extensions.Caching.Redis/` | 레거시 캐싱 확장 호환 패키지 |
| `XUnitTest/` | xUnit 테스트 스위트(net10.0) |
| `AotSmoke/` | NativeAOT 스모크 프로젝트 |
| `Samples/` | Benchmark, WebDemo, QueueDemo, RedisSwitch 및 모바일 스모크 샘플 |
| `Doc/` | 요구사항, 아키텍처, 기능 목록 및 주제별 문서 |

### 릴리스

`v` 접두사 태그(예: `v7.0.2026.1024`)를 푸시하면 `.github/workflows/publish.yml`이 실행됩니다. 워크플로는 `NewLife.Redis`와 `NewLife.Redis.Extensions`를 패키징하여 NuGet.org와 GitHub Packages에 게시하고, `ChangeLog.md`의 해당 버전 절로 GitHub Release를 생성합니다. 수동 실행은 패키지 게시만 수행합니다.

### 피드백

1. 제출 전에 `dotnet build`와 관련 테스트가 통과하는지 확인하고, 저장소의 코드 스타일과 커밋 규칙을 따르세요
2. PR: 변경을 최소화하고 필요한 주석과 테스트 설명을 추가하세요
3. Issue: 버전, 런타임 환경, 최소 재현 시나리오를 제공하세요

커뮤니티: QQ 그룹 1600800 / 1600838; GitHub Discussions / Issues.

## 뉴라이프 개발팀

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

뉴라이프 팀(NewLife)은 2002년에 설립되어 20여 년간 .NET과 IoT 기술 생태계에 집중하며 100여 개의 오픈소스 프로젝트를 주도하고(이 중 34개가 핵심 프로젝트), 전력, 물류, 의료, 산업 제어, 인터넷 등 산업에 소프트웨어와 하드웨어를 아우르는 통합 솔루션을 제공해 왔습니다. "학문에는 선후가 없으며, 이룬 사람이 스승이다"라는 신조를 따릅니다.

오픈소스 컴포넌트는 NuGet에서 누적 <!--nuget-total:start-->6.48M<!--nuget-total:end-->회 다운로드되었습니다(생태계 전체 기준, 2026-10).

웹사이트: https://newlifex.com · 오픈소스 조직: https://github.com/NewLifeX

<details>
<summary>위챗 공식 계정: 智能大石头(스캔하여 팔로우)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## 라이선스

MIT License. 상업적 사용 / 수정 / 재배포 모두 무료입니다(별도 승인 불필요). 저작권 표시만 유지하면 됩니다.

> 이 문서가 다루지 않는 사용 사례가 있다면 언제든 Issue를 남겨 주세요 — 함께 문서를 개선해 나갑시다!
