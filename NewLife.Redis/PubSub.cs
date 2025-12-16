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

    ///// <summary>订阅给定的模式</summary>
    ///// <remarks>
    ///// 支持的模式(patterns)有:
    ///// h? llo subscribes to hello, hallo and hxllo
    ///// h* llo subscribes to hllo and heeeello
    ///// h[ae]llo subscribes to hello and hallo, but not hillo
    ///// 如果想输入普通的字符，可以在前面添加\
    ///// </remarks>
    ///// <param name="patterns"></param>
    ///// <returns></returns>
    //public Int32 PSubscribe(params String[] patterns) =>
    //    //var args = new List<Object>
    //    //{
    //    //    Key
    //    //};
    //    //foreach (var item in patterns)
    //    //{
    //    //    args.Add(item);
    //    //}
    //    //return Execute(rc => rc.Execute<Int32>("PSUBSCRIBE", args.ToArray()), true);
    //    Execute(rc => rc.Execute<Int32>("PSUBSCRIBE", patterns), true);

    ///// <summary>指示客户端退订指定模式，若果没有提供模式则退出所有模式</summary>
    ///// <returns></returns>
    //public Int32 PUnSubscribe(params String[] patterns)
    //{
    //    if (patterns != null && patterns.Length > 0)
    //        return Execute(rc => rc.Execute<Int32>("PUNSUBSCRIBE", patterns), true);
    //    else
    //        return Execute(rc => rc.Execute<Int32>("PUNSUBSCRIBE"), true);
    //}

    ///// <summary>订阅给指定频道的信息</summary>
    ///// <param name="channels"></param>
    ///// <returns></returns>
    //public Int32 Subscribe(params String[] channels) => Execute(rc => rc.Execute<Int32>("SUBSCRIBE", channels), true);

    /// <summary>订阅大循环</summary>
    /// <param name="onMessage"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SubscribeAsync(Action<String, String> onMessage, CancellationToken cancellationToken = default)
    {
        var client = Redis.Pool.Get();
        client.Reset();

        var channels = Key.Split(",", ";").Cast<Object>().ToArray();
        await client.ExecuteAsync<String[]>("SUBSCRIBE", channels, cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            var source = new CancellationTokenSource(Redis.Timeout);
            var source2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, source.Token);

            //var rs = await client.ExecuteAsync<String[]>(null, new Object[] { new Object() }, source2.Token);
            var rs = await client.ReadMoreAsync<String[]>(source2.Token).ConfigureAwait(false);
            if (rs != null && rs.Length == 3 && rs[0] == "message") onMessage(rs[1], rs[2]);
        }

        await client.ExecuteAsync<String[]>("UNSUBSCRIBE", channels, cancellationToken).ConfigureAwait(false);

        Redis.Pool.Return(client);
    }

    ///// <summary>退订给定的频道</summary>
    ///// <returns></returns>
    //public Int32 UnSubscribe(params String[] channels)
    //{
    //    if (channels != null && channels.Length > 0)
    //        return Execute(rc => rc.Execute<Int32>("UNSUBSCRIBE", channels), true);
    //    else
    //        return Execute(rc => rc.Execute<Int32>("UNSUBSCRIBE"), true);
    //}

    /// <summary>发布消息</summary>
    /// <param name="message">消息内容</param>
    /// <returns>返回接收到消息的客户端个数</returns>
    public Int32 Publish(String message) => Execute((rc, k) => rc.Execute<Int32>("PUBLISH", Key, message), true);

    ///// <summary>自省</summary>
    ///// <returns></returns>
    //public Int32 Pubsub() => 0;
}