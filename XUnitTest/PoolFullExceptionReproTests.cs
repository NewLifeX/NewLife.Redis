using System;
using System.IO;
using System.Reflection;

using NewLife;
using NewLife.Caching;
using NewLife.Caching.Clusters;
using NewLife.Collections;

using Xunit;

namespace XUnitTest;

/// <summary>
/// 复现 AllPool 打满：池名 All，连续失败次数超过 PoolMax。
/// 修复前超过容量即抛 PoolFullException；修复后每次失败都归还槽位，不会池满。
/// </summary>
public class PoolFullExceptionReproTests
{
    /// <summary>与异常消息一致的池容量</summary>
    private const Int32 PoolMax = 5;

    /// <summary>套上统一配置，并在首次借出前定死池容量</summary>
    /// <param name="redis">客户端</param>
    /// <param name="max">池容量</param>
    private static void Apply(Redis redis, Int32 max)
    {
        redis.Init(BasicTest.GetConfig());
        redis.Name = "All";
        redis.Retry = 3;
        redis.Timeout = 2_000;

        var cfg = redis.PoolConfig;
        cfg.Min = 0;
        cfg.Max = max;
        cfg.WaitTimeout = 0;
        cfg.IdleTime = 0;
    }

    /// <summary>创建基类客户端，走 Redis.Execute</summary>
    /// <param name="max">池容量</param>
    /// <returns>Redis 客户端</returns>
    private static Redis CreateRedis(Int32 max)
    {
        var redis = new Redis();
        Apply(redis, max);
        return redis;
    }

    /// <summary>创建 FullRedis。它重载了 Execute、ExecuteAsync，节点执行不走基类</summary>
    /// <param name="max">池容量</param>
    /// <returns>FullRedis 客户端</returns>
    private static FullRedis CreateFullRedis(Int32 max)
    {
        var redis = new FullRedis();
        Apply(redis, max);
        return redis;
    }

    /// <summary>连续 IO 失败。漏还槽位时，超过容量会抛出 PoolFullException</summary>
    /// <param name="redis">Redis 客户端</param>
    /// <param name="times">失败次数</param>
    private static void FailMany(Redis redis, Int32 times)
    {
        for (var i = 0; i < times; i++)
        {
            try
            {
                redis.Execute<Int32>("poolslot:fail", (_, _) => throw new IOException("reset"));
            }
            catch (PoolFullException ex)
            {
                Assert.Fail(ex.Message);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>Redis.Execute 连续失败 100001 次，不应再出现 AllPool 已有 100,000</summary>
    [Fact(DisplayName = "连续失败超过PoolMax次不抛AllPool池满")]
    public void Redis_RepeatFailure_PastPoolMax_DoesNotFillPool()
    {
        using var redis = CreateRedis(PoolMax);

        FailMany(redis, PoolMax + 1);

        var pool = (ObjectPool<RedisClient>)redis.Pool;
        Assert.Equal("AllPool", pool.Name);
        Assert.Equal(0, pool.BusyCount);

        var pong = redis.Execute("poolslot:ping", (c, _) => c.Execute<String>("PING"));
        Assert.Equal("PONG", pong);
        Assert.Equal(0, pool.BusyCount);
    }

    /// <summary>FullRedis 节点池同样连续失败超过容量</summary>
    [Fact(DisplayName = "FullRedis节点池连续失败超过容量不池满")]
    public void FullRedis_RepeatFailure_PastPoolMax_DoesNotFillPool()
    {
        using var redis = CreateFullRedis(PoolMax);

        var server = redis.Server ?? "127.0.0.1:6379";
        var comma = server.IndexOf(',');
        if (comma > 0) server = server[..comma];
        var node = new RedisNode { Owner = redis, EndPoint = server };

        for (var i = 0; i < PoolMax + 1; i++)
        {
            try
            {
                redis.Execute<Int32>(_ => throw new IOException("reset"), node);
            }
            catch (PoolFullException ex)
            {
                Assert.Fail(ex.Message);
            }
            catch (IOException)
            {
            }
        }

        var pool = (ObjectPool<RedisClient>)redis.GetPool(node);
        Assert.Equal(0, pool.BusyCount);

        var pong = redis.Execute(c => c.Execute<String>("PING"), node);
        Assert.Equal("PONG", pong);
        Assert.Equal(0, pool.BusyCount);
    }

    /// <summary>订阅失败连续超过容量时，连接必须归还，否则会池满</summary>
    [RedisFact(DisplayName = "PubSub连续失败超过容量不池满")]
    public async System.Threading.Tasks.Task PubSub_RepeatFailure_PastMax_DoesNotFillPool()
    {
        const Int32 max = 8;
        using var redis = CreateRedis(max);
        var pb = new PubSub(redis, "poolslot:sub");

        for (var i = 0; i < max + 1; i++)
        {
            using var source = new System.Threading.CancellationTokenSource();
            source.Cancel();
            try
            {
                await pb.SubscribeAsync((_, _) => { }, source.Token);
            }
            catch (PoolFullException ex)
            {
                Assert.Fail(ex.Message);
            }
            catch
            {
            }
        }

        Assert.Equal(0, ((ObjectPool<RedisClient>)redis.Pool).BusyCount);
    }

    /// <summary>DISCARD 失败连续超过容量时，连接必须归还，否则会池满</summary>
    [Fact(DisplayName = "事务DISCARD连续失败超过容量不池满")]
    public void Transaction_RepeatFailure_PastMax_DoesNotFillPool()
    {
        const Int32 max = 8;
        using var redis = CreateRedis(max);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        for (var i = 0; i < max + 1; i++)
        {
            var tx = redis.BeginTransaction();
            try
            {
                typeof(RedisTransaction).GetField("_began", flags)!.SetValue(tx, true);
                var client = (RedisClient)typeof(RedisTransaction).GetField("_client", flags)!.GetValue(tx)!;
                client.TryDispose();
            }
            catch (PoolFullException ex)
            {
                Assert.Fail(ex.Message);
            }
            finally
            {
                tx.Dispose();
            }
        }

        Assert.Equal(0, ((ObjectPool<RedisClient>)redis.Pool).BusyCount);
    }
}
