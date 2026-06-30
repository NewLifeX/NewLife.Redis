using Xunit;

namespace XUnitTest;

/// <summary>集成测试标记特性。当Redis不可用时自动标记为Skip，避免测试卡死在连接超时</summary>
/// <remarks>
/// 替代 [Fact]，用于所有需要 Redis 连接的集成测试方法。
/// 在测试发现阶段检查 BasicTest.RedisAvailable，若Redis不可用则跳过。
/// 纯单元测试（不依赖Redis）仍使用普通 [Fact]。
/// </remarks>
public class RedisFactAttribute : FactAttribute
{
    /// <summary>实例化集成测试特性。若Redis不可用则设置Skip</summary>
    public RedisFactAttribute()
    {
        if (!BasicTest.RedisAvailable)
            Skip = "Redis 服务器不可用，跳过集成测试。设置 REDIS_CONNECTION 环境变量或确保 127.0.0.1:6379 可用。";
    }
}
