﻿using NewLife.Log;

namespace NewLife.Caching.Queues;

/// <summary>Redis延迟队列</summary>
/// <remarks>
/// 延迟Redis队列，每次生产操作1次Redis，消费操作4次Redis。
/// </remarks>
public class RedisDelayQueue<T> : QueueBase, IProducerConsumer<T>
{
    #region 属性
    /// <summary>转移延迟消息到主队列的间隔。默认10s</summary>
    public Int32 TransferInterval { get; set; } = 10;

    /// <summary>个数</summary>
    public Int32 Count => _sort?.Count ?? 0;

    /// <summary>是否为空</summary>
    public Boolean IsEmpty => Count == 0;

    /// <summary>默认延迟时间。默认60秒</summary>
    public Int32 Delay { get; set; } = 60;

    private readonly RedisSortedSet<T> _sort;
    #endregion

    #region 构造
    /// <summary>实例化延迟队列</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public RedisDelayQueue(Redis redis, String key) : base(redis, key) => _sort = new RedisSortedSet<T>(redis, redis is FullRedis rds ? rds.GetKey(key) : key);
    #endregion

    #region 核心方法
    /// <summary>添加延迟消息</summary>
    /// <param name="value"></param>
    /// <param name="delay">延迟时间。单位秒</param>
    /// <returns></returns>
    public Int32 Add(T value, Int32 delay)
    {
        using var span = Redis.Tracer?.NewSpan($"redismq:{TraceName}:Add", value);
        try
        {
            var target = DateTime.UtcNow.ToInt() + delay;
            var rs = 0;
            for (var i = 0; i <= RetryTimesWhenSendFailed; i++)
            {
                // 添加到有序集合的成员数量，不包括已经存在更新分数的成员
                rs = _sort.Add(value, target);
                if (rs >= 0) return rs;

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

    /// <summary>批量生产，延迟时间来自Delay属性</summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public Int32 Add(params T[] values)
    {
        if (values == null || values.Length == 0) return 0;

        using var span = Redis.Tracer?.NewSpan($"redismq:{TraceName}:Add", values);
        try
        {
            var target = DateTime.UtcNow.ToInt() + Delay;
            var rs = 0;
            for (var i = 0; i <= RetryTimesWhenSendFailed; i++)
            {
                rs = _sort.Add(values, target);
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

    /// <summary>删除项</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Int32 Remove(T value) => _sort.Remove(value);

    /// <summary>获取一个</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <returns></returns>
    public T? TakeOne(Int32 timeout = 0)
    {
        //RetryAck();

        // 最长等待
        if (timeout == 0) timeout = 60;

        while (true)
        {
            var score = DateTime.UtcNow.ToInt();
            var rs = _sort.RangeByScore(0, score, 0, 1);
            if (rs != null && rs.Length > 0 && TryPop(rs[0])) return rs[0];

            // 是否需要等待
            if (timeout <= 0) break;

            Thread.Sleep(1000);
            timeout--;
        }

        return default;
    }

    /// <summary>异步获取一个</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<T?> TakeOneAsync(Int32 timeout = 0, CancellationToken cancellationToken = default)
    {
        //RetryAck();

        // 最长等待
        if (timeout == 0) timeout = 60;

        while (!cancellationToken.IsCancellationRequested)
        {
            var score = DateTime.UtcNow.ToInt();
            var rs = await _sort.RangeByScoreAsync(0, score, 0, 1, cancellationToken).ConfigureAwait(false);
            if (rs != null && rs.Length > 0 && TryPop(rs[0])) return rs[0];

            // 是否需要等待
            if (timeout <= 0) break;

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            timeout--;
        }

        return default;
    }

    /// <summary>异步消费获取</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <returns></returns>
    Task<T?> IProducerConsumer<T>.TakeOneAsync(Int32 timeout) => TakeOneAsync(timeout, default);

    /// <summary>获取一批</summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public IEnumerable<T> Take(Int32 count = 1)
    {
        if (count <= 0) yield break;

        //RetryAck();

        var score = DateTime.UtcNow.ToInt();
        var rs = _sort.RangeByScore(0, score, 0, count);
        if (rs == null || rs.Length == 0) yield break;

        foreach (var item in rs)
        {
            // 争夺消费
            if (TryPop(item)) yield return item;
        }
    }

    /// <summary>争夺消费，只有一个线程能够成功删除，作为抢到的标志。同时备份到Ack队列</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private Boolean TryPop(T value)
    {
        //if (_ack != null)
        //{
        //    // 先备份，再删除。备份到Ack队列
        //    var score = DateTime.Now.ToInt() + RetryInterval;
        //    _ack.Add(value, score);
        //}

        // 删除作为抢夺
        return _sort.Remove(value) > 0;
    }

    /// <summary>确认删除</summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public Int32 Acknowledge(params T[] keys) => -1;

    /// <summary>确认删除</summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    Int32 IProducerConsumer<T>.Acknowledge(params String[] keys) => -1;
    #endregion

    #region 消息交换
    /// <summary>异步转移消息，已到期消息转移到目标队列</summary>
    /// <param name="queue">队列</param>
    /// <param name="onException">异常处理</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task TransferAsync(IProducerConsumer<T> queue, Action<Exception>? onException = null, CancellationToken cancellationToken = default)
    {
        // 大循环之前，打断性能追踪调用链
        DefaultSpan.Current = null;

        // 超时时间，用于阻塞等待
        //var timeout = Redis.Timeout / 1000 - 1;
        //var topic = Key;
        var tracer = Redis.Tracer;

        while (!cancellationToken.IsCancellationRequested)
        {
            ISpan? span = null;
            try
            {
                // 异步阻塞消费
                var score = DateTime.UtcNow.ToInt();
                var msgs = await _sort.RangeByScoreAsync(0, score, 0, 10, cancellationToken).ConfigureAwait(false);
                if (msgs != null && msgs.Length > 0)
                {
                    // 删除消息后直接进入目标队列，无需进入Ack
                    span = tracer?.NewSpan($"redismq:{TraceName}:Transfer", msgs);

                    // 逐个删除，多线程争夺可能失败
                    var list = new List<T>();
                    for (var i = 0; i < msgs.Length; i++)
                    {
                        if (Remove(msgs[i]) > 0) list.Add(msgs[i]);
                    }

                    // 转移消息
                    if (list.Count > 0) queue.Add(list.ToArray());
                }
                else
                {
                    // 没有消息，歇一会
                    await Task.Delay(TransferInterval * 1000, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (ThreadAbortException) { break; }
            catch (ThreadInterruptedException) { break; }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) break;

                span?.SetError(ex, null);

                onException?.Invoke(ex);
            }
            finally
            {
                span?.Dispose();
            }
        }
    }
    #endregion
}