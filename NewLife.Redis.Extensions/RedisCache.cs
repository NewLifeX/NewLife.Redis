using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NewLife.Caching;

namespace NewLife.Redis.Extensions;

/// <summary>
/// Redis分布式缓存
/// </summary>
public class RedisCache : FullRedis, IDistributedCache, IDisposable
{
    #region 属性

    /// <summary>刷新时的过期时间。默认24小时</summary>
    public new TimeSpan Expire { get; set; } = TimeSpan.FromHours(24);
    #endregion

    #region 构造
    /// <summary>
    /// 实例化Redis分布式缓存
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="optionsAccessor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public RedisCache(IServiceProvider serviceProvider, IOptions<RedisOptions> optionsAccessor) : base(serviceProvider, optionsAccessor.Value)
    {
        if (optionsAccessor == null) throw new ArgumentNullException(nameof(optionsAccessor));
    }

    #endregion

    /// <summary>
    /// 获取
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Byte[]? Get(String key) => base.Get<Byte[]?>(key);

    /// <summary>
    /// 异步获取
    /// </summary>
    /// <param name="key"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Byte[]?> GetAsync(String key, CancellationToken token = default)
        => ExecuteAsync<Byte[]?>(key, (rds, k) => rds.ExecuteAsync<Byte[]?>("GET", [k], token));

    /// <summary>
    /// 设置
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Set(String key, Byte[] value, DistributedCacheEntryOptions options)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (options == null)
            base.Set(key, value);
        else
            if (options.AbsoluteExpiration != null)
            base.Set(key, value, options.AbsoluteExpiration.Value - DateTime.Now);
        else if (options.AbsoluteExpirationRelativeToNow != null)
            base.Set(key, value, options.AbsoluteExpirationRelativeToNow.Value);
        else if (options.SlidingExpiration != null)
            base.Set(key, value, options.SlidingExpiration.Value);
        else
            base.Set(key, value);
    }

    /// <summary>
    /// 异步设置
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task SetAsync(String key, Byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        // 解析过期时间，与同步Set保持一致
        var expire = -1;
        if (options != null)
        {
            if (options.AbsoluteExpiration != null)
                expire = (Int32)(options.AbsoluteExpiration.Value - DateTime.Now).TotalSeconds;
            else if (options.AbsoluteExpirationRelativeToNow != null)
                expire = (Int32)options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds;
            else if (options.SlidingExpiration != null)
                expire = (Int32)options.SlidingExpiration.Value.TotalSeconds;
        }
        if (expire < 0) expire = (Int32)Expire.TotalSeconds;

        // 真异步，避免 Task.Run 假异步占用线程池
        return ExecuteAsync<String>(key, async (rds, k) =>
        {
            var rs = expire <= 0 ?
                await rds.ExecuteAsync<String>("SET", [k, value], token).ConfigureAwait(false) :
                await rds.ExecuteAsync<String>("SETEX", [k, expire, value], token).ConfigureAwait(false);

            return rs ?? "";
        }, true);
    }

    /// <summary>
    /// 刷新
    /// </summary>
    /// <param name="key"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Refresh(String key) => base.SetExpire(key, Expire);

    /// <summary>
    /// 异步刷新
    /// </summary>
    /// <param name="key"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RefreshAsync(String key, CancellationToken token = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        // 真异步，避免 Task.Run 假异步占用线程池
        return ExecuteAsync<String>(key, async (rds, k) =>
        {
            var rs = await rds.ExecuteAsync<String>("EXPIRE", [k, (Int32)Expire.TotalSeconds], token).ConfigureAwait(false);

            return rs ?? "";
        }, true);
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="key"></param>
    public new void Remove(String key) => base.Remove(key);

    /// <summary>
    /// 异步删除
    /// </summary>
    /// <param name="key"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveAsync(String key, CancellationToken token = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        // 真异步，避免 Task.Run 假异步占用线程池
        return ExecuteAsync<Int32>(key, async (rds, k) =>
        {
            var task = rds.ExecuteAsync<Int32>("DEL", [k], token);
            Int32? rs = await task.ConfigureAwait(false);

            return rs ?? 0;
        }, true);
    }
}