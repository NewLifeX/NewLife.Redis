# NewLife.Redis ChangeLog

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
