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

    /// <summary>Pika 服务器（豌豆荚开源的 RocksDB 存储引擎兼容实现）</summary>
    Pika = 3,

    /// <summary>DragonflyDB 服务器（新型高性能 Redis 兼容数据库）</summary>
    DragonflyDB = 4,

    /// <summary>华为云 DCS 集群版（CLUSTER NODES返回内网IP）</summary>
    HuaweiCloud = 10,

    /// <summary>阿里云 KVStore（Tair）</summary>
    AlibabaCloud = 11,

    /// <summary>腾讯云 Redis</summary>
    TencentCloud = 12,
}
