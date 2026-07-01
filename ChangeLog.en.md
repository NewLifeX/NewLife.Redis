# NewLife.Redis ChangeLog

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
