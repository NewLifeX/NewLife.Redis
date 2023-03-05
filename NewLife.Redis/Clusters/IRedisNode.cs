using NewLife.Collections;

namespace NewLife.Caching.Clusters;

/// <summary>集群中的节点</summary>
public interface IRedisNode
{
    /// <summary>连接池</summary>
    IPool<RedisClient> Pool { get; }
}