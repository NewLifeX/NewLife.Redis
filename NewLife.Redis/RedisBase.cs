namespace NewLife.Caching;

/// <summary>基础结构</summary>
public abstract class RedisBase
{
    #region 属性
    /// <summary>客户端对象</summary>
    public Redis Redis { get; }

    /// <summary>键</summary>
    public String Key { get; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public RedisBase(Redis redis, String key)
    {
        Redis = redis;
        Key = redis is FullRedis rds ? rds.GetKey(key) : key;
    }
    #endregion

    #region 方法
    /// <summary>获取经前缀处理后的键名</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual String GetKey(String key) => Redis is FullRedis rds ? rds.GetKey(key) : key;

    /// <summary>执行命令</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public virtual T Execute<T>(Func<RedisClient, String, T> func, Boolean write = false) => Redis.Execute(Key, func, write);

    /// <summary>异步执行命令</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public virtual Task<T> ExecuteAsync<T>(Func<RedisClient, String, Task<T>> func, Boolean write = false) => Redis.ExecuteAsync(Key, func, write);
    #endregion
}