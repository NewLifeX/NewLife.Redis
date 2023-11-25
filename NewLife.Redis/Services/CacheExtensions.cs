using NewLife.Caching.Queues;
using NewLife.Caching.Services;

namespace NewLife.Caching;

/// <summary>缓存扩展</summary>
public static class CacheExtensions
{
    /// <summary>获取Stream队列</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cacheProvider"></param>
    /// <param name="topic"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    public static RedisStream<T>? GetStream<T>(this ICacheProvider cacheProvider, String topic, String group) => cacheProvider.GetQueue<T>(topic, !group.IsNullOrEmpty() ? group : "Group") as RedisStream<T>;

    /// <summary>获取延迟队列</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cacheProvider"></param>
    /// <param name="topic"></param>
    /// <returns></returns>
    public static RedisDelayQueue<T>? GetDelayQueue<T>(this ICacheProvider cacheProvider, String topic) => cacheProvider is not RedisCacheProvider rp ? null : rp.RedisQueue?.GetDelayQueue<T>(topic);
}
