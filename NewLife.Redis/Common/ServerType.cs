namespace NewLife.Caching;

/// <summary>Redis服务器类型</summary>
public enum ServerType
{
    /// <summary>未知类型</summary>
    Unknown = 0,

    /// <summary>标准 Redis 服务器</summary>
    Redis = 1,

    /// <summary>Garnet 服务器（微软的Redis兼容实现）</summary>
    Garnet = 2,
}
