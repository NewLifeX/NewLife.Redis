# NewLife.Redis ChangeLog

## v7.0.2026.1026 (2026-10-26)

### Bug Fixes
- **[fix]** Fixed pool-slot leaks that threw `PoolFullException` after a broken client was destroyed and `Pool.Return` was skipped (for example AllPool reporting 100,000 busy items at the maximum)
- Removed `disposed`. Setting it after disposal made `finally` skip `Return` via `if (!disposed)`. The slot is released only by `Return`, so that flag leaked the slot. `ObjectPool.Return` sees `Disposed` and only releases the slot, without placing the client back in the free list, so `finally` always calls `Return`
- Moved `TryDispose` ahead of `if (++i >= Retry) throw`. The previous order threw on the last attempt before disposal, and `finally` returned the broken client to the free list. A `RedisException`, and any non-network exception, still returns a live client
- `Redis.Execute` / `ExecuteAsync`, `FullRedis` node execution and `ExecuteAsync`, a failed PubSub unsubscribe, and a failed transaction `DISCARD` all follow the same rule: dispose an unusable client, then `Return`
- **Tests**: `PoolFullExceptionReproTests` uses a pool named All and fails more times than `PoolMax`. Before the fix this throws a pool-full exception; after the fix the busy count stays 0

---

## v7.0.2026.1024 (2026-10-24)

### Breaking Changes (Upgrade Notes)
- **NewLife.Core 12.0.2026.1024**: the first release crossing the Core major version, adopting the v12 packet ownership model (`OwnerPacket` reference counting, `Slice` semantics) plus localization and NativeAOT. Callers compiled against Core v11 must recompile; mixing with components still pinned to Core 11 causes dependency conflicts
- **`Message.GetBody<T>()` signature change**: now `GetBody<T>(String? primitiveKey = null)`; source-compatible but not binary-compatible, so callers that are not recompiled fail at runtime
- **Assembly version**: `AssemblyVersion` moves from `6.x.yyyy.MMdd` to `7.0.yyyy.MMdd`; strong-name binding and declarative authorization must be updated accordingly
- **Target framework resolution**: a new `net8.0` asset is included, so net8.0+ hosts load it instead of `netstandard2.1` (with AOT support); Core 12 dropped the netcoreapp3.1/net5.0/net7.0 assets, so `net5.0` and `netcoreapp3.1` hosts fall back to `netstandard2.1` and `net7.0` hosts fall back to `net6.0`

### New Features
- **Redis transactions**: added `RedisTransaction` with `MULTI`/`EXEC`/`DISCARD`/`WATCH`/`UNWATCH`, running transactional commands on a dedicated connection
- **Broader command coverage**: added persistence commands `SAVE`/`BGSAVE`/`BGREWRITEAOF`/`LASTSAVE`, Stream `XAUTOCLAIM`, sorted-set reverse and lexicographic families, `LPUSHX`/`RPUSHX`, `GETDEL`/`PSETEX`/`MSETNX`/`SUBSTR`, and the `MOVE`/`RANDOMKEY`/`SORT`/`PTTL`/`EXPIREAT` family
- **Operations and scripting commands**: completed the `ECHO`/`RESET`/`CLIENT`/`CONFIG`/`SCRIPT`/`EVALSHA`/`ACL` groups for day-to-day operations and staged rollouts
- **Observability**: built-in OpenTelemetry `ActivitySource` so command execution can join OTel traces
- **Cloud and compatible implementations**: Valkey server-type detection, Alibaba Cloud TairZSet (`EXZ*`), and Redis 8.x core commands (`MSETEX`/`DELEX`/`DIGEST`/`XDELEX`/`XACKDEL`/`SUNIONCARD`/`SDIFFCARD`)
- **AOT and net8.0**: added the `net8.0` target with NativeAOT/trimming support, and `IsAotCompatible` keeps the analyzer gate always on
- **Runtime localization**: runtime exception and log message tables extended to 31 languages, with log prefixes kept out of translation; the extension package carries its own table

### Bug Fixes
- **[fix]** Fixed `HGETEX`/`HGETDEL` argument assembly and array response decoding
- **[fix]** Fixed out-of-order consumption caused by the delay-queue transfer loop starting twice
- **[fix]** Fixed cluster MOVED redirect endpoint parsing and sentinel primary failover
- **[fix]** Fixed pooled connections being polluted when PubSub cancellation skipped unsubscribe
- **[fix]** Fixed `RedisStream` decoding failures for primitive payloads with extra fields by injecting metadata at the tail consistently
- **[fix]** Fixed command arguments missing from exception tracing

### Architecture & Improvements
- **Solution migrated to `.slnx`**: switched to the new solution format
- **Removed AOT annotation shims**: dropped local shims duplicated with NewLife.Core public stubs to avoid type conflicts
- **Cluster logs carry instance prefix**: slot-operation errors now point at the specific node
- **Package metadata in English**: assembly titles and descriptions standardized to English, package descriptions kept bilingual

### Testing & Quality
- Unit tests parallelized with full-database operations isolated, and shared-Redis leftovers cleaned up in statistics tests
- Added 23 AotSmoke scenarios (including cluster and sentinel topologies); CI now also smokes Android/iOS, riscv64/s390x and LoongArch LA64
- Added an English runtime-message gate and locale table checks that block hard-coded Chinese exceptions and orphan keys

### Documentation & Localization
- **README rebuilt**: complete coverage of 11 languages, with the ecosystem map and team section matching NewLife.Core verbatim
- Added requirements, feature list, architecture, competitor analysis and AOT compatibility documents as a traceable baseline

### Dependencies
- `NewLife.Core` upgraded to `12.0.2026.1024`, with `NewLife.UnitTest`/`NewLife.Stardust`/`BenchmarkDotNet` raised in sync

---

## v6.6.2026.0901 (2026-09-01)

### Bug Fixes
- **[fix]** `FullRedis` `Eval`/`FCall`/`FCallRO` now assemble arguments as arrays, avoiding argument-count errors and improving primary-key routing for distributed scripts; added boundary tests for read-only functions, Lua argument expansion and empty arguments

### Security & Dependencies
- **Dependency upgrades**: `Microsoft.Extensions` packages raised to the latest patch of each target framework, `net10.0` DataProtection from 10.0.9 to 10.0.11, removing high-severity `System.Security.Cryptography.Xml` vulnerabilities; `NewLife.Core`, SourceLink and test tooling upgraded in sync

---
## v6.6.2026.0802 (2026-08-02)

### New Features
- **Connection Pool Configuration & Health Checks**: Added `RedisPoolConfig` class to configure min idle / max capacity / idle cleanup / max lifetime / borrow wait timeout; proactively PING-verify connections idle beyond the threshold when leased, improving long-connection reliability
- **Command Packet Pre-Encoding**: `GetCommandSize` pre-encodes complex objects and caches them for reuse; batch commands pre-encoded as a whole, avoiding repeated serialization and improving command throughput

### Bug Fixes
- **[fix]** Fixed `Search` returning empty when `count=-1` (default all), wildcard delete `Remove("key*")` restored
- **[fix]** Fixed RedLock millisecond locks never expiring: integer division yields 0 when `msExpire < 1000`, switched to `SET PX NX`
- **[fix]** Fixed `GetCommandSize` under-estimating value-type parameters causing buffer overflow
- **[fix]** Fixed cluster `Execute(String[])` mutating caller's array in place, result ordering, and `List.Contains` O(n²) dedup
- **[fix]** Fixed cluster `Search` sharing SCAN cursor across nodes causing duplicate/missing keys
- **[fix]** Fixed extension package fake async: `Task.Run` wrapping sync implementation replaced with true async `ExecuteAsync`, supporting cancellation without tying up thread pool
- **[fix]** Fixed `ExecuteAsync` retry blocking thread pool threads with `Thread.Sleep`, switched to `Task.Delay`
- **[fix]** Fixed thread-unsafe mutation of global `Redis.Timeout` during queue blocking consumption, switched to client-level timeout/linked token
- **[fix]** Fixed silent truncation of overlong `ReadLine` responses (256 chars), falls back to StringBuilder
- **[fix]** Fixed `ExecuteCommand` memory-pool buffer not returned on exception and PubSub connection not returned to pool on error
- **[fix]** Fixed returning already-disposed client to pool in connection pool catch

### Architecture
- **RedLock Unified Implementation**: `FullRedis.AcquireRedLock` delegates to `RedisRedLock`, eliminating logic drift between two implementations
- **Removed Deprecated PrefixedRedis**: Superseded by `FullRedis.Prefix`, breaking change
- **Removed `IncrementWithTtl`/`DecrementWithTtl`**: Removed increment/decrement-with-TTL APIs to align with the base class (breaking change)
- **Marked `AddPrefixedRedis` Obsolete**: `[Obsolete]`, use `AddRedis` (FullRedis supports Prefix)

### Improvements
- **RedisHash/RedisSet Full Traversal**: Support `count<=0` full scan, batch fetch raised to 1000, improving large-data traversal efficiency

### Testing & Quality
- Added regression tests for Search/wildcard delete/immutable batch input/RedLock millisecond lock/value-type size estimation
- Fixed `RedisCacheTests`/`DependencyInjectionTest` hardcoded `127.0.0.1` preventing use of configured Redis

---

## v6.5.2026.0702 (2026-07-02)

### New Features
- **NuGet Security Audit**: Enabled NuGet audit to automatically scan dependencies for known vulnerabilities, improving supply chain security
- **Redis 6.2~7.4+ Commands**: Added support for more Redis commands and APIs, improving command coverage
- **Alibaba Cloud Tair Extension**: Support for Alibaba Cloud Tair enterprise-exclusive data structures (TairString/TairHash)

### Testing & Quality
- **RedisFact Smart Test Attribute**: Introduced `RedisFact` attribute to automatically skip unavailable Redis integration tests, improving CI test stability
- **Redis Version & ACL Compatibility**: Enhanced compatibility with different Redis versions and ACL authentication scenarios, improved test robustness
- **StreamInfo Parsing Robustness**: Strengthened StreamInfo and other response parsing robustness, added unit test coverage

### Improvements
- **Stream/EventBus Tracing Enhancement**: Optimized RedisStream consumption tracing and logging, enhanced EventBus consumer loop startup/shutdown logic
- **Extension Package Documentation**: Added standalone READMEs for extension packages, marked legacy `NewLife.Extensions.Caching.Redis` as deprecated
- **Distributed Lock Refinement**: Improved distributed lock support for Redis 7.0+ features and multi-cloud compatibility

---

## v6.5.2026.0601 (2026-06-01)

### Release Maintenance
- **Stability Maintenance Release**: Routine June maintenance release with no new public APIs, fully compatible with the 6.5 series
- **Continuous Delivery**: Continues the previous release's improvements in RedisStream XINFO error recovery and ConsumeAsync robustness

---

## v6.5.2026.0501 (2026-05-01)

### Bug Fixes
- **[fix]** Fixed error handling in RedisStream when XINFO returns `ERR no such key`, enhanced ConsumeAsync auto-recovery capability

---

## v6.5.2026.0302 (2026-02-02 ~ 2026-03-02)

### New Features
- RESP3 protocol support with optimized core performance allocations
- Enhanced Garnet compatibility detection and documentation
- New RedisSwitch sample project (message queue producer/consumer)

### Fixes
- Fixed code review issues (UTC time, retry logic, redundant checks, unused properties)

### Improvements
- Refactored benchmarks, optimized documentation and startup process
- Restructured documentation system for better maintainability

---

## v6.4.2026.0201 (2026-02-01)

### New Features
- RedisStream queue info display
- Support for automatic removal of idle consumer groups

### Fixes
- Fixed FullRedis.CreateEventBus method
- Fixed ShowInfo display issue

### Improvements
- Require consumer count to be zero when removing idle consumer groups for enhanced safety

---

## v6.4.2026.0102 (2026-01-02)

### Improvements
- Optimized EventBus support
