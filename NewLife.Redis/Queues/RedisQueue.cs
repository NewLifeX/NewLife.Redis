﻿using NewLife.Data;

namespace NewLife.Caching.Queues;

/// <summary>Redis队列，左进右出</summary>
/// <remarks>
/// 默认弹出消费，不需要确认，使用非常简单，但如果消费者处理失败，消息将会丢失；
/// 
/// 普通Redis队列，每次生产操作1次Redis，消费操作1次Redis。
/// </remarks>
/// <typeparam name="T"></typeparam>
public class RedisQueue<T> : QueueBase, IProducerConsumer<T>
{
    #region 属性
    /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
    public Int32 MinPipeline { get; set; } = 3;

    /// <summary>个数</summary>
    public Int32 Count => Execute((r, k) => r.Execute<Int32>("LLEN", Key));

    /// <summary>是否为空</summary>
    public Boolean IsEmpty => Count == 0;
    #endregion

    #region 构造
    /// <summary>实例化队列</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public RedisQueue(Redis redis, String key) : base(redis, key) { }
    #endregion

    #region 核心方法
    /// <summary>生产添加</summary>
    /// <param name="value">消息</param>
    /// <returns></returns>
    public Int32 Add(T value)
    {
        using var span = Redis.Tracer?.NewSpan($"redismq:{TraceName}:Add", value);
        try
        {
            var val = AttachTraceId ? Redis.AttachTraceId(value) : value;
            var rs = 0;
            for (var i = 0; i <= RetryTimesWhenSendFailed; i++)
            {
                // 返回插入后的LIST长度。Redis执行命令不会失败，因此正常插入不应该返回0，如果返回了0或者服务，可能是中间代理出了问题
                rs = Execute((rc, k) => rc.Execute<Int32>("LPUSH", Key, val), true);
                if (rs > 0) return rs;

                span?.SetError(new InvalidOperationException($"发布到队列[{Topic}]失败！"), null);

                if (i < RetryTimesWhenSendFailed) Thread.Sleep(RetryIntervalWhenSendFailed);
            }

            ValidWhenSendFailed(span);

            return rs;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>批量生产添加</summary>
    /// <param name="values">消息集合</param>
    /// <returns></returns>
    public Int32 Add(params T[] values)
    {
        using var span = Redis.Tracer?.NewSpan($"redismq:{TraceName}:Add", values);
        try
        {
            var args = new List<Object> { Key };
            foreach (var item in values)
            {
                if (AttachTraceId)
                    args.Add(Redis.AttachTraceId(item));
                else
                    args.Add(item);
            }

            var rs = 0;
            for (var i = 0; i <= RetryTimesWhenSendFailed; i++)
            {
                // 返回插入后的LIST长度。Redis执行命令不会失败，因此正常插入不应该返回0，如果返回了0或者服务，可能是中间代理出了问题
                rs = Execute((rc, k) => rc.Execute<Int32>("LPUSH", args.ToArray()), true);
                if (rs > 0) return rs;

                span?.SetError(new InvalidOperationException($"发布到队列[{Topic}]失败！"), null);

                if (i < RetryTimesWhenSendFailed) Thread.Sleep(RetryIntervalWhenSendFailed);
            }

            ValidWhenSendFailed(span);

            return rs;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>消费获取，支持阻塞</summary>
    /// <param name="timeout">超时，0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <returns></returns>
    public T? TakeOne(Int32 timeout = -1)
    {
        if (timeout < 0) return Execute((rc, k) => rc.Execute<T>("RPOP", Key), true);

        if (timeout > 0 && Redis.Timeout < (timeout + 1) * 1000) Redis.Timeout = (timeout + 1) * 1000;

        var rs = Execute((rc, k) => rc.Execute<IPacket[]>("BRPOP", Key, timeout), true);
        return rs == null || rs.Length < 2 ? default : (T?)Redis.Encoder.Decode(rs[1], typeof(T));
    }

    /// <summary>异步消费获取</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<T?> TakeOneAsync(Int32 timeout = 0, CancellationToken cancellationToken = default)
    {
        if (timeout < 0) return await ExecuteAsync((rc, k) => rc.ExecuteAsync<T>("RPOP", Key), true).ConfigureAwait(false);

        if (timeout > 0 && Redis.Timeout < (timeout + 1) * 1000) Redis.Timeout = (timeout + 1) * 1000;

        var rs = await ExecuteAsync((rc, k) => rc.ExecuteAsync<IPacket[]>("BRPOP", [Key, timeout], cancellationToken), true).ConfigureAwait(false);
        return rs == null || rs.Length < 2 ? default : (T?)Redis.Encoder.Decode(rs[1], typeof(T));
    }

    /// <summary>异步消费获取</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <returns></returns>
    Task<T?> IProducerConsumer<T>.TakeOneAsync(Int32 timeout) => TakeOneAsync(timeout, default);

    /// <summary>批量消费获取</summary>
    /// <param name="count">要消费的消息个数</param>
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
                Execute((rc, k) => rc.Execute<T>("RPOP", Key), true);

            var rs = rds.StopPipeline(true);
            foreach (var item in rs)
                if (item != null) yield return (T)item;
        }
        else
            for (var i = 0; i < count; i++)
            {
                var value = Execute((rc, k) => rc.Execute<T>("RPOP", Key), true);
                if (Equals(value, default(T))) break;

                yield return value;
            }
    }

    /// <summary>确认消费。不支持</summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    Int32 IProducerConsumer<T>.Acknowledge(params String[] keys) => -1;
    #endregion
}