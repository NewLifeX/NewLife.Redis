# NewLife.Redis — 고성능 Redis 클라이언트

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.Redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.Redis?logo=nuget)

🌐 [中文](Readme.MD) | [English](Readme.en.md) | [日本語](README.ja.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Русский](README.ru.md)

---

`NewLife.Redis`는 NewLife 팀이 개발한 **고성능·고처리량·쉬운 통합** .NET Redis 클라이언트입니다. 2017년부터 여러 대규모 프로덕션 환경에서 안정적으로 운영되며, **하루 80억 회 이상**의 명령 호출을 처리하고 있습니다.

## 빠른 시작

```bash
dotnet add package NewLife.Redis
```

```csharp
using NewLife.Caching;

// 연결
var redis = new FullRedis("server=127.0.0.1:6379;password=yourpwd;db=0");

// 기본 작업
redis.Set("key", "value");
var val = redis.Get<String>("key");

// 키 만료 설정
redis.Set("temp", data, 60); // 60초
```

## 주요 기능

- **완전한 프로토콜 지원**: RESP2/RESP3, Redis 2.8~7.x 모든 명령어
- **모든 데이터 구조**: String / List / Hash / Set / Sorted Set / Stream / Geo / HyperLogLog
- **메시지 큐**: 단순·신뢰성(RPOPLPUSH+Ack)·지연·Stream 멀티 컨슈머 그룹
- **클러스터 & HA**: Standalone / Sentinel / Cluster, 자동 장애 조치
- **클라우드 지원**: Alibaba Cloud KVStore / Tencent Cloud Redis / Huawei Cloud DCS
- **광범위한 프레임워크**: .NET Framework 4.5 ~ .NET 10+ 지원
- **Garnet 호환**: Microsoft Garnet / kvrocks 자동 감지, 기능 미지원 시 우아한 대체 제공

## 확장 패키지

| 패키지 | 설명 |
|-----------|------|
| `NewLife.Redis` | 코어 Redis 클라이언트 |
| `NewLife.Redis.Extensions` | ASP.NET Core DI, `IDistributedCache`, `IDataProtection` |

## 문서

> 📖 전체 문서(중국어): [요구사항](Doc/需求文档.md) | [아키텍처](Doc/架构文档.md) | [경쟁사 분석](Doc/竞品分析.md)

## 라이선스

MIT License © 2002-2026 [新生命 개발팀](https://newlifex.com)

[GitHub](https://github.com/NewLifeX/NewLife.Redis) | [공식 사이트](https://newlifex.com)
