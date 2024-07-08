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
    public Task<Byte[]?> GetAsync(String key, CancellationToken token = default) => Task.Run(() => base.Get<Byte[]>(key), token);

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
    public Task SetAsync(String key, Byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.Run(() => Set(key, value, options), token);

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
    /// <exception cref="ArgumentNullException"></exception>
    public Task RefreshAsync(String key, CancellationToken token = default) => Task.Run(() => Refresh(key), token);

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="key"></param>
    public void Remove(String key) => base.Remove(key);

    /// <summary>
    /// 异步删除
    /// </summary>
    /// <param name="key"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveAsync(String key, CancellationToken token = default) => Task.Run(() => base.Remove(key), token);
}