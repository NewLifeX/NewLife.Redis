namespace NewLife.Caching;

/// <summary>Redis事务。独占一条连接执行 MULTI/EXEC，事务期间占用该连接直到提交或释放</summary>
/// <remarks>
/// 典型用法：<c>using var tx = redis.BeginTransaction(); tx.Begin(); tx.Execute&lt;String&gt;("SET", "k", "v"); var rs = tx.Commit();</c>
/// WATCH 必须在 Begin 之前调用；被监视键在提交前被其他客户端修改时，事务中止（提交返回空结果）。
/// 事务需独占连接，不适合高频短事务；集群模式仅支持同一槽位的键。
/// </remarks>
public class RedisTransaction : DisposeBase
{
    #region 属性
    private readonly Redis _redis;
    private readonly RedisClient _client;
    private Boolean _began;
    private Boolean _finished;
    #endregion

    #region 构造
    /// <summary>实例化事务，立即从连接池取出一条独占连接</summary>
    /// <param name="redis">Redis客户端</param>
    public RedisTransaction(Redis redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _client = redis.Pool.Get();
    }

    /// <summary>销毁。未提交的事务自动放弃，连接归还连接池</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        if (disposing)
        {
            var ok = true;
            if (_began && !_finished)
            {
                // 连接异常时不再归还池，交由池重建
                try
                {
                    _client.Execute<String>("DISCARD");
                }
                catch
                {
                    ok = false;
                }
            }

            if (ok)
                _redis.Pool.Return(_client);
            else
                _client.TryDispose();
        }

        base.Dispose(disposing);
    }
    #endregion

    #region 方法
    /// <summary>监视指定键（WATCH）。提交前任一被监视键被其他客户端修改，则事务中止</summary>
    /// <param name="keys">键列表</param>
    /// <returns>成功返回true</returns>
    public Boolean Watch(params String[] keys)
    {
        if (_began) throw new InvalidOperationException("必须在Begin之前调用WATCH");
        if (keys == null || keys.Length == 0) return false;

        var ps = new List<Object>(keys.Length);
        foreach (var item in keys)
        {
            ps.Add(_redis.MapKey(item));
        }

        return _client.Execute<String>("WATCH", ps.ToArray()) == "OK";
    }

    /// <summary>取消监视全部键（UNWATCH）</summary>
    /// <returns>成功返回true</returns>
    public Boolean Unwatch() => _client.Execute<String>("UNWATCH") == "OK";

    /// <summary>开始事务（MULTI）。此后所有命令进入队列，直到提交或放弃</summary>
    /// <returns>当前事务</returns>
    public RedisTransaction Begin()
    {
        if (_began) throw new InvalidOperationException("事务已开启");

        _client.Execute<String>("MULTI");
        _began = true;

        return this;
    }

    /// <summary>在事务中排队执行命令，服务端返回 QUEUED</summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="command">命令名称</param>
    /// <param name="args">命令参数</param>
    /// <returns>排队结果，通常为 QUEUED</returns>
    public T? Execute<T>(String command, params Object[] args)
    {
        if (!_began) throw new InvalidOperationException("请先调用 Begin");
        if (_finished) throw new InvalidOperationException("事务已结束");

        return _client.Execute<T>(command, args);
    }

    /// <summary>提交事务（EXEC），一次性执行队列中所有命令</summary>
    /// <returns>各命令的执行结果数组；被监视键已修改导致事务中止时返回空结果</returns>
    public Object[]? Commit()
    {
        if (!_began) throw new InvalidOperationException("请先调用 Begin");
        if (_finished) throw new InvalidOperationException("事务已结束");

        var rs = _client.Execute<Object[]>("EXEC");
        _finished = true;

        return rs;
    }

    /// <summary>放弃事务（DISCARD），清空命令队列</summary>
    public void Discard()
    {
        if (!_began || _finished) return;

        _client.Execute<String>("DISCARD");
        _finished = true;
    }
    #endregion
}
