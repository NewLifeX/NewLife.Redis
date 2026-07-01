using NewLife.Caching;
using NewLife.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTest;

public class StackTests
{
    private FullRedis _redis;

    public StackTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Retry = 0;
        _redis.Timeout = 5_000;
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    [RedisFact]
    public void Stack_Normal()
    {
        var key = "Stack_Normal";

        // 删除已有
        _redis.Remove(key);
        var s = _redis.GetStack<String>(key);
        _redis.SetExpire(key, TimeSpan.FromMinutes(60));

        var stack = s as RedisStack<String>;
        Assert.NotNull(stack);

        // 取出个数
        var count = stack.Count;
        Assert.True(stack.IsEmpty);
        Assert.Equal(0, count);

        // 添加
        var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
        stack.Add(vs);

        // 对比个数
        var count2 = stack.Count;
        Assert.False(stack.IsEmpty);
        Assert.Equal(count + vs.Length, count2);

        // 取出来
        var vs2 = stack.Take(2).ToArray();
        Assert.Equal(2, vs2.Length);
        Assert.Equal(vs[3], vs2[0]);
        Assert.Equal(vs[2], vs2[1]);

        var vs3 = s.Take(2).ToArray();
        Assert.Equal(2, vs3.Length);
        Assert.Equal(vs[1], vs3[0]);
        Assert.Equal(vs[0], vs3[1]);

        // 对比个数
        var count3 = stack.Count;
        Assert.True(stack.IsEmpty);
        Assert.Equal(count, count3);
    }

    [RedisFact]
    public async Task Queue_Async()
    {
        var key = "Stack_Async";

        // 删除已有
        _redis.Remove(key);
        var q = _redis.GetStack<String>(key);

        // 添加
        var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
        q.Add(vs);

        // 取出来
        Assert.Equal("ABEF", await q.TakeOneAsync(0));
        Assert.Equal("新生命团队", await q.TakeOneAsync(0));
        Assert.Equal("abcd", await q.TakeOneAsync(0));
        Assert.Equal("1234", await q.TakeOneAsync(0));

        // 空消息
        var sw = Stopwatch.StartNew();
        String? rs = null;
        try
        {
            rs = await q.TakeOneAsync(2);
        }
        catch (OperationCanceledException)
        {
            // Redis 超时与 CancellationToken 可能竞态导致取消，视为正常超时
        }
        sw.Stop();
        Assert.Null(rs);
        Assert.True(sw.ElapsedMilliseconds >= 1500, $"超时不足: {sw.ElapsedMilliseconds}ms");

        // 延迟2秒生产消息
        ThreadPool.QueueUserWorkItem(s => { Thread.Sleep(1500); q.Add("xxyy"); });
        sw = Stopwatch.StartNew();
        try
        {
            rs = await q.TakeOneAsync(3);
        }
        catch (OperationCanceledException)
        {
            // 同上，竞态异常视为超时
        }
        sw.Stop();
        Assert.Equal("xxyy", rs);
        Assert.True(sw.ElapsedMilliseconds >= 1000, $"超时不足: {sw.ElapsedMilliseconds}ms");
    }
}