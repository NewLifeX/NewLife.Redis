using NewLife.Log;
using NewLife.Security;

namespace NewLife.Caching;

/// <summary>Redis RedLock 分布式锁（多实例Redlock算法）</summary>
/// <remarks>
/// Redlock算法基于Redis官方推荐的多实例分布式锁方案。
/// 需要至少3个独立的Redis实例（建议奇数个），在多数实例上加锁成功才算获得锁。
/// 适用于跨机房、跨数据中心的高可用分布式锁场景。
/// 
/// 使用示例：
/// <code>
/// var rds1 = new FullRedis("server1:6379", "pass", 0);
/// var rds2 = new FullRedis("server2:6379", "pass", 0);
/// var rds3 = new FullRedis("server3:6379", "pass", 0);
/// 
/// using var rl = RedisRedLock.Acquire([rds1, rds2, rds3], "my_lock", 1000, 30000);
/// if (rl != null)
/// {
///     // 获得锁，执行业务逻辑
/// }
/// else
/// {
///     // 获取锁失败
/// }
/// </code>
/// </remarks>
public class RedisRedLock : IDisposable
{
    #region 属性
    /// <summary>锁键名</summary>
    public String Key { get; }

    /// <summary>随机令牌</summary>
    public String Token { get; }

    /// <summary>已成功加锁的实例</summary>
    private readonly List<Redis> _lockedInstances = [];

    /// <summary>时钟漂移补偿因子（0.01=1%）</summary>
    public Double ClockDriftFactor { get; set; } = 0.01;

    /// <summary>重试延迟（毫秒）</summary>
    public Int32 RetryDelay { get; set; } = 200;
    #endregion

    #region 构造
    private RedisRedLock(String key, String token)
    {
        Key = key;
        Token = token;
    }
    #endregion

    #region 方法
    /// <summary>尝试获取RedLock分布式锁</summary>
    /// <param name="instances">Redis实例列表（建议3个以上奇数个独立实例）</param>
    /// <param name="key">锁键名</param>
    /// <param name="msTimeout">获取锁超时时间（毫秒）</param>
    /// <param name="msExpire">锁过期时间（毫秒）</param>
    /// <returns>锁对象，获取失败返回null</returns>
    public static RedisRedLock? Acquire(Redis[] instances, String key, Int32 msTimeout, Int32 msExpire)
    {
        if (instances == null || instances.Length == 0) throw new ArgumentNullException(nameof(instances));
        if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(key));
        if (msTimeout <= 0) throw new ArgumentOutOfRangeException(nameof(msTimeout), "超时时间必须大于0");
        if (msExpire <= 0) throw new ArgumentOutOfRangeException(nameof(msExpire), "过期时间必须大于0");

        var token = Rand.NextString(22);
        var rl = new RedisRedLock(key, token);
        var quorum = instances.Length / 2 + 1;
        var startTime = DateTime.UtcNow;
        var elapsed = 0;

        // 循环重试，直到超时
        for (var retry = 0; ; retry++)
        {
            var lockStart = DateTime.UtcNow;
            var successCount = 0;

            // 尝试在每个实例上加锁
            foreach (var rds in instances)
            {
                try
                {
                    if (rds.Set(key, token, msExpire / 1000))
                    {
                        rl._lockedInstances.Add(rds);
                        successCount++;
                    }
                }
                catch
                {
                    // 单实例失败不中断，继续尝试其他实例
                }
            }

            // 计算已用时间
            var lockElapsed = (Int32)(DateTime.UtcNow - lockStart).TotalMilliseconds;

            // 判断是否获取锁成功：
            // 1. 在多数节点上成功
            // 2. 总耗时小于锁有效期（减去时钟漂移）
            var validityTime = msExpire - lockElapsed - (Int32)(msExpire * rl.ClockDriftFactor);
            if (successCount >= quorum && validityTime > 0)
            {
                rl.WriteLog("RedLock.Acquire key={0} token={1} success={2}/{3} elapsed={4}ms validity={5}ms",
                    key, token, successCount, instances.Length, lockElapsed, validityTime);

                return rl;
            }

            // 加锁失败，释放已获得的锁
            foreach (var rds in rl._lockedInstances.ToList())
            {
                TryUnlock(rds, key, token);
            }
            rl._lockedInstances.Clear();

            // 检查是否超时
            elapsed = (Int32)(DateTime.UtcNow - startTime).TotalMilliseconds;
            if (elapsed >= msTimeout)
            {
                rl.WriteLog("RedLock.Acquire key={0} timeout={1}ms elapsed={2}ms success={3}/{4}",
                    key, msTimeout, elapsed, successCount, instances.Length);
                break;
            }

            // 等待后重试
            var delay = Math.Min(rl.RetryDelay + Rand.Next(50), msTimeout - elapsed);
            Thread.Sleep(delay);
        }

        return null;
    }

    private static void TryUnlock(Redis rds, String key, String token)
    {
        try
        {
            // 使用Lua脚本原子性检查并删除锁（防止误删他人锁）
            var lua = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('del', KEYS[1])
else
    return 0
end";
            rds.Execute(key, (rc, k) => rc.Execute<Int32>("EVAL", lua, 1, k, token), true);
        }
        catch
        {
            // 释放失败不抛异常
        }
    }

    /// <summary>释放所有已获取的锁</summary>
    public void Dispose()
    {
        foreach (var rds in _lockedInstances)
        {
            TryUnlock(rds, Key, Token);
        }
        _lockedInstances.Clear();

        WriteLog("RedLock.Release key={0} token={1}", Key, Token);
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);
    #endregion
}
