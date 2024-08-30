namespace NewLife.Caching;

/// <summary>Redis栈，右进右出</summary>
/// <typeparam name="T"></typeparam>
public class RedisStack<T> : RedisBase, IProducerConsumer<T>
{
    #region 属性
    /// <summary>个数</summary>
    public Int32 Count => Execute((r, k) => r.Execute<Int32>("LLEN", Key));

    /// <summary>是否为空</summary>
    public Boolean IsEmpty => Count == 0;

    /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
    public Int32 MinPipeline { get; set; } = 3;
    #endregion

    #region 构造
    /// <summary>实例化队列</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public RedisStack(Redis redis, String key) : base(redis, key) { }
    #endregion

    /// <summary>批量生产添加</summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public Int32 Add(params T[] values)
    {
        var args = new List<Object> { Key };
        foreach (var item in values)
        {
            args.Add(item);
        }
        return Execute((rc, k) => rc.Execute<Int32>("RPUSH", args.ToArray()), true);
    }

    /// <summary>批量消费获取</summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public IEnumerable<T> Take(Int32 count = 1)
    {
        if (count <= 0) yield break;

        // 借助管道支持批量获取
        if (count >= MinPipeline)
        {
            var rds = Redis;
            rds.StartPipeline();

            for (var i = 0; i < count; i++)
            {
                Execute((rc, k) => rc.Execute<T>("RPOP", Key), true);
            }

            var rs = rds.StopPipeline(true);
            foreach (var item in rs)
            {
                if (item is null || Equals(item, default(T))) { break; }

                yield return (T)item;
            }
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                var value = Execute((rc, k) => rc.Execute<T>("RPOP", Key), true);
                if (value is null || Equals(value, default(T))) break;

                yield return value;
            }
        }
    }

    /// <summary>消费获取，支持阻塞</summary>
    /// <param name="timeout">超时，0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <returns></returns>
    public T? TakeOne(Int32 timeout = -1)
    {
        if (timeout < 0) return Execute((rc, k) => rc.Execute<T>("RPOP", Key), true);

        var rs = Execute((rc, k) => rc.Execute<IPacket[]>("BRPOP", Key, timeout), true);
        return rs == null || rs.Length < 2 ? default : (T?)Redis.Encoder.Decode(rs[1], typeof(T));
    }

    /// <summary>异步消费获取</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<T?> TakeOneAsync(Int32 timeout = 0, CancellationToken cancellationToken = default)
    {
        if (timeout < 0) return await ExecuteAsync((rc, k) => rc.ExecuteAsync<T>("RPOP", Key), true);

        var rs = await ExecuteAsync((rc, k) => rc.ExecuteAsync<IPacket[]>("BRPOP", new Object[] { Key, timeout }, cancellationToken), true);
        return rs == null || rs.Length < 2 ? default : (T?)Redis.Encoder.Decode(rs[1], typeof(T));
    }

    /// <summary>异步消费获取</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <returns></returns>
    Task<T?> IProducerConsumer<T>.TakeOneAsync(Int32 timeout) => TakeOneAsync(timeout, default);

    Int32 IProducerConsumer<T>.Acknowledge(params String[] keys) => throw new NotSupportedException();
}