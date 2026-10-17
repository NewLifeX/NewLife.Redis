namespace NewLife.Caching;

/// <summary>发布订阅</summary>
public class PubSub : RedisBase
{
    #region 实例化
    /// <summary>实例化发布订阅</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public PubSub(Redis redis, String key) : base(redis, key) { }
    #endregion

    #region 订阅
    /// <summary>订阅大循环</summary>
    /// <param name="onMessage">回调函数，参数为(频道, 消息)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SubscribeAsync(Action<String, String> onMessage, CancellationToken cancellationToken = default)
    {
        if (onMessage == null) throw new ArgumentNullException(nameof(onMessage));

        var client = Redis.Pool.Get();
        var returnToPool = false;
        try
        {
            client.Reset();

            var channels = Key.Split(",", ";").Cast<Object>().ToArray();
            await client.ExecuteAsync<String[]>("SUBSCRIBE", channels, cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                using var source = new CancellationTokenSource(Redis.Timeout);
                using var source2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, source.Token);

                String[]? rs;
                try
                {
                    rs = await client.ReadMoreAsync<String[]>(source2.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // 读取超时，继续等待下一条消息
                    continue;
                }
                catch (OperationCanceledException)
                {
                    // 主动取消，跳出循环后退订，避免把订阅态连接归还连接池
                    break;
                }

                if (rs != null && rs.Length == 3 && rs[0] == "message") onMessage(rs[1], rs[2]);
            }

            // 清空取消时残留的推送帧，确保退订命令与响应一一对应
            client.Reset();
            returnToPool = await TryUnsubscribeAsync(client, "UNSUBSCRIBE", channels, Redis.Timeout).ConfigureAwait(false);
        }
        finally
        {
            if (returnToPool) Redis.Pool.Return(client); else client.TryDispose();
        }
    }

    /// <summary>模式订阅大循环</summary>
    /// <remarks>
    /// 支持的模式(pattern)有:
    /// h? llo 订阅 hello, hallo 和 hxllo
    /// h* llo 订阅 hllo 和 heeeello
    /// h[ae]llo 订阅 hello 和 hallo, 但不包含 hillo
    /// 如果想输入普通的字符，可以在前面添加\
    /// </remarks>
    /// <param name="onMessage">回调函数，参数为(模式, 频道, 消息)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task PSubscribeAsync(Action<String, String, String> onMessage, CancellationToken cancellationToken = default)
    {
        if (onMessage == null) throw new ArgumentNullException(nameof(onMessage));

        var client = Redis.Pool.Get();
        var returnToPool = false;
        try
        {
            client.Reset();

            var patterns = Key.Split(",", ";").Cast<Object>().ToArray();
            await client.ExecuteAsync<String[]>("PSUBSCRIBE", patterns, cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                using var source = new CancellationTokenSource(Redis.Timeout);
                using var source2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, source.Token);

                String[]? rs;
                try
                {
                    rs = await client.ReadMoreAsync<String[]>(source2.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    continue;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (rs != null && rs.Length == 4 && rs[0] == "pmessage") onMessage(rs[1], rs[2], rs[3]);
            }

            // 清空取消时残留的推送帧，确保退订命令与响应一一对应
            client.Reset();
            returnToPool = await TryUnsubscribeAsync(client, "PUNSUBSCRIBE", patterns, Redis.Timeout).ConfigureAwait(false);
        }
        finally
        {
            if (returnToPool) Redis.Pool.Return(client); else client.TryDispose();
        }
    }
    #endregion

    #region 发布
    /// <summary>发布消息</summary>
    /// <param name="message">消息内容</param>
    /// <returns>返回接收到消息的客户端个数</returns>
    public Int32 Publish(String message) => Execute((rc, k) => rc.Execute<Int32>("PUBLISH", Key, message), true);

    /// <summary>分片发布消息（Redis 7.0+ SPUBLISH）</summary>
    /// <remarks>分片发布将消息路由到特定集群分片，适合大规模消息广播</remarks>
    /// <param name="message">消息内容</param>
    /// <returns>返回接收到消息的客户端个数</returns>
    public Int32 SPublish(String message) => Execute((rc, k) => rc.Execute<Int32>("SPUBLISH", Key, message), true);
    #endregion

    #region 分片订阅
    /// <summary>分片订阅大循环（Redis 7.0+）</summary>
    /// <param name="onMessage">回调函数，参数为(频道, 消息)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SSubscribeAsync(Action<String, String> onMessage, CancellationToken cancellationToken = default)
    {
        if (onMessage == null) throw new ArgumentNullException(nameof(onMessage));

        var client = Redis.Pool.Get();
        var returnToPool = false;
        try
        {
            client.Reset();

            var channels = Key.Split(",", ";").Cast<Object>().ToArray();
            await client.ExecuteAsync<String[]>("SSUBSCRIBE", channels, cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                using var source = new CancellationTokenSource(Redis.Timeout);
                using var source2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, source.Token);

                String[]? rs;
                try
                {
                    rs = await client.ReadMoreAsync<String[]>(source2.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    continue;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (rs != null && rs.Length == 3 && rs[0] == "smessage") onMessage(rs[1], rs[2]);
            }

            // 清空取消时残留的推送帧，确保退订命令与响应一一对应
            client.Reset();
            returnToPool = await TryUnsubscribeAsync(client, "SUNSUBSCRIBE", channels, Redis.Timeout).ConfigureAwait(false);
        }
        finally
        {
            if (returnToPool) Redis.Pool.Return(client); else client.TryDispose();
        }
    }

    /// <summary>尝试退出订阅模式。失败时丢弃该连接，避免订阅态连接污染连接池</summary>
    /// <param name="client">客户端连接</param>
    /// <param name="command">退订命令</param>
    /// <param name="channels">频道或模式</param>
    /// <param name="timeout">退订超时时间，单位毫秒</param>
    /// <returns>连接是否已退出订阅模式并可归还连接池</returns>
    private static async Task<Boolean> TryUnsubscribeAsync(RedisClient client, String command, Object[] channels, Int32 timeout)
    {
        try
        {
            // 退订不能无限等待，Redis 无响应时直接丢弃该连接
            using var source = new CancellationTokenSource(timeout > 0 ? timeout : 5_000);
            await client.ExecuteAsync<String[]>(command, channels, source.Token).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion

    #region 自省
    /// <summary>查看活跃频道列表</summary>
    /// <param name="pattern">可选的匹配模式，不指定返回所有频道</param>
    /// <returns>活跃频道名称列表</returns>
    public String[]? PubSubChannels(String? pattern = null)
    {
        if (pattern != null)
            return Execute((rc, k) => rc.Execute<String[]>("PUBSUB", "CHANNELS", pattern));
        else
            return Execute((rc, k) => rc.Execute<String[]>("PUBSUB", "CHANNELS"));
    }

    /// <summary>查看指定频道的订阅者数量</summary>
    /// <param name="channels">频道名称列表</param>
    /// <returns>各频道的订阅者数量</returns>
    public Object[]? PubSubNumSub(params String[] channels)
    {
        if (channels == null || channels.Length == 0)
            return Execute((rc, k) => rc.Execute<Object[]>("PUBSUB", "NUMSUB"));
        return Execute((rc, k) => rc.Execute<Object[]>("PUBSUB", "NUMSUB", channels));
    }

    /// <summary>查看模式订阅数量</summary>
    /// <returns>当前服务器上模式订阅的总数</returns>
    public Int32 PubSubNumPat() => Execute((rc, k) => rc.Execute<Int32>("PUBSUB", "NUMPAT"));
    #endregion
}