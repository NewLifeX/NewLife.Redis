namespace NewLife.Caching;

/// <summary>支持键前缀的Redis</summary>
public class PrefixedRedis : FullRedis
{
    #region 属性
    /// <summary>键前缀</summary>
    public String Prefix { get; set; }
    #endregion

    /// <summary>重载执行，支持集群</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public override T Execute<T>(String key, Func<RedisClient, T> func, Boolean write = false) => base.Execute(Prefix + key, func, write);

    /// <summary>重载执行，支持集群</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public override Task<T> ExecuteAsync<T>(String key, Func<RedisClient, Task<T>> func, Boolean write = false) => base.ExecuteAsync(Prefix + key, func, write);
}
