using System;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

public class RedisTransactionTests
{
    private readonly FullRedis _redis;

    public RedisTransactionTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Log = XTrace.Log;

        if (BasicTest.EnableClientLog) _redis.ClientLog = XTrace.Log;
    }

    [RedisFact(DisplayName = "事务提交与放弃测试")]
    public void CommitAndDiscard()
    {
        var key1 = "tx_commit1";
        var key2 = "tx_commit2";
        var key3 = "tx_discard";

        _redis.Remove(key1, key2, key3);

        // 提交
        using (var tx = _redis.BeginTransaction())
        {
            tx.Begin();
            tx.Execute<String>("SET", key1, "v1");
            tx.Execute<String>("SET", key2, "v2");

            // 事务未提交前，其他连接不可见
            Assert.Null(_redis.Get<String>(key1));

            var rs = tx.Commit();
            Assert.NotNull(rs);
            Assert.Equal(2, rs.Length);
        }

        Assert.Equal("v1", _redis.Get<String>(key1));
        Assert.Equal("v2", _redis.Get<String>(key2));

        // 放弃
        using (var tx = _redis.BeginTransaction())
        {
            tx.Begin();
            tx.Execute<String>("SET", key3, "v3");
            tx.Discard();
        }

        Assert.Null(_redis.Get<String>(key3));

        _redis.Remove(key1, key2, key3);
    }

    [RedisFact(DisplayName = "WATCH无冲突提交测试")]
    public void WatchCommit()
    {
        var key = "tx_watch_ok";

        _redis.Remove(key);
        _redis.Set(key, 1);

        using var tx = _redis.BeginTransaction();
        Assert.True(tx.Watch(key));
        tx.Begin();
        tx.Execute<String>("INCR", key);

        var rs = tx.Commit();
        Assert.NotNull(rs);
        Assert.Single(rs);

        // 自增生效
        Assert.Equal("2", _redis.Get<String>(key));

        _redis.Remove(key);
    }

    [RedisFact(DisplayName = "WATCH冲突事务中止测试")]
    public void WatchConflict()
    {
        var key = "tx_watch";

        _redis.Remove(key);
        _redis.Set(key, 10);

        using var tx = _redis.BeginTransaction();
        Assert.True(tx.Watch(key));
        tx.Begin();
        tx.Execute<String>("INCR", key);

        // 提交之前，其他客户端修改被监视键
        _redis.Set(key, 100);

        // 事务中止，返回空结果
        var rs = tx.Commit();
        Assert.True(rs == null || rs.Length == 0);

        // 自增未生效
        Assert.Equal("100", _redis.Get<String>(key));

        _redis.Remove(key);
    }

    [RedisFact(DisplayName = "事务对象释放后连接可用测试")]
    public void DisposeReturnsConnection()
    {
        var key = "tx_dispose";

        _redis.Remove(key);

        // 未提交直接释放，自动放弃事务
        using (var tx = _redis.BeginTransaction())
        {
            tx.Begin();
            tx.Execute<String>("SET", key, "v");
        }

        Assert.Null(_redis.Get<String>(key));

        // 连接池正常可用
        _redis.Set(key, "ok");
        Assert.Equal("ok", _redis.Get<String>(key));

        _redis.Remove(key);
    }
}
