using NewLife.Collections;

namespace NewLife.Caching;

/// <summary>连接池配置。控制 Redis 连接池的最小空闲数、最大容量、空闲清理时间、最大生命周期和借用等待超时</summary>
public class RedisPoolConfig
{
    /// <summary>连接池最小空闲数。默认10</summary>
    public Int32 Min { get; set; } = 10;

    /// <summary>连接池最大容量。默认100000</summary>
    public Int32 Max { get; set; } = 100000;

    /// <summary>连接池空闲清理时间（秒）。最小个数之上的连接超过空闲时间时被清理，默认30s</summary>
    public Int32 IdleTime { get; set; } = 30;

    /// <summary>连接最大生命周期（秒）。超过后强制回收，默认300s。适用于主从切换、代理滚动更新等场景</summary>
    public Int32 MaxLifetime { get; set; } = 300;

    /// <summary>池满时借出等待超时（秒）。默认15s，0表示不等待立即抛出PoolFullException</summary>
    public Int32 WaitTimeout { get; set; } = 15;

    /// <summary>从配置字典加载连接池参数</summary>
    /// <param name="dic">配置字典</param>
    public void Load(IDictionary<String, String> dic)
    {
        if (dic.TryGetValue("PoolMin", out var str) && str.ToInt(-1) >= 0)
            Min = str.ToInt();
        if (dic.TryGetValue("PoolMax", out str) && str.ToInt(-1) >= 0)
            Max = str.ToInt();
        if (dic.TryGetValue("PoolIdleTime", out str) && str.ToInt(-1) >= 0)
            IdleTime = str.ToInt();
        if (dic.TryGetValue("MaxLifetime", out str) && str.ToInt(-1) >= 0)
            MaxLifetime = str.ToInt();
        if (dic.TryGetValue("WaitTimeout", out str) && str.ToInt(-1) >= 0)
            WaitTimeout = str.ToInt();
    }
}
