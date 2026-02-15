using System;
using System.Linq;
using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

/// <summary>Garnet 兼容性测试</summary>
public class GarnetCompatibilityTests
{
    private readonly FullRedis _redis;

    public GarnetCompatibilityTests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    [Fact(DisplayName = "服务器类型检测")]
    public void ServerTypeDetection()
    {
        // 获取服务器类型
        var serverType = _redis.ServerType;
        Assert.NotEqual(ServerType.Unknown, serverType);

        // 输出服务器信息
        XTrace.WriteLine($"服务器类型：{serverType}");
        XTrace.WriteLine($"服务器版本：{_redis.Version}");
        XTrace.WriteLine($"是否为 Garnet：{_redis.IsGarnet}");

        // 验证 Info 包含必要字段
        var info = _redis.Info;
        Assert.NotNull(info);
        Assert.True(info.ContainsKey("redis_version"));
    }

    [Fact(DisplayName = "基础功能兼容性")]
    public void BasicOperationsCompatibility()
    {
        var key = "garnet:test:basic";

        // 字符串操作
        _redis.Set(key, "TestValue", 60);
        var value = _redis.Get<String>(key);
        Assert.Equal("TestValue", value);

        // 数值操作
        var counterKey = "garnet:test:counter";
        _redis.Set(counterKey, 0);
        _redis.Increment(counterKey, 1);
        var counter = _redis.Get<Int32>(counterKey);
        Assert.Equal(1, counter);

        // 清理
        _redis.Remove(key, counterKey);
    }

    [Fact(DisplayName = "List 操作兼容性")]
    public void ListOperationsCompatibility()
    {
        var key = "garnet:test:list";
        _redis.Remove(key);

        var list = _redis.GetList<String>(key);
        list.Add("item1");
        list.Add("item2");
        list.Add("item3");

        Assert.Equal(3, list.Count);
        Assert.Equal("item1", list[0]);

        _redis.Remove(key);
    }

    [Fact(DisplayName = "Hash 操作兼容性")]
    public void HashOperationsCompatibility()
    {
        var key = "garnet:test:hash";
        _redis.Remove(key);

        var hash = _redis.GetDictionary<String>(key);
        hash["field1"] = "value1";
        hash["field2"] = "value2";

        Assert.Equal(2, hash.Count);
        Assert.Equal("value1", hash["field1"]);

        _redis.Remove(key);
    }

    [Fact(DisplayName = "RedisQueue 兼容性")]
    public void RedisQueueCompatibility()
    {
        var key = "garnet:test:queue";
        _redis.Remove(key);

        var queue = _redis.GetQueue<String>(key);
        queue.Add("msg1");
        queue.Add("msg2");

        var msg = queue.TakeOne(0);
        Assert.Equal("msg1", msg);

        _redis.Remove(key);
    }

    [Fact(DisplayName = "Stream 功能检测")]
    public void StreamSupportDetection()
    {
        var key = "garnet:test:stream";
        _redis.Remove(key);

        var stream = _redis.GetStream<String>(key);

        // 检查是否支持 Stream
        XTrace.WriteLine($"Stream 功能支持：{stream.IsSupported}");

        if (_redis.IsGarnet)
        {
            // Garnet 不支持 Stream
            Assert.False(stream.IsSupported);

            // 尝试使用应该抛出异常
            Assert.Throws<NotSupportedException>(() => stream.Add("test"));
        }
        else if (_redis.Version >= new Version(5, 0))
        {
            // Redis 5.0+ 支持 Stream
            Assert.True(stream.IsSupported);

            // 正常使用
            var id = stream.Add("test");
            Assert.NotNull(id);
        }

        _redis.Remove(key);
    }

    [Fact(DisplayName = "Stream 替代方案")]
    public void StreamAlternative()
    {
        var key = "garnet:test:alternative";
        _redis.Remove(key);

        // 根据服务器类型选择合适的队列实现
        if (_redis.IsGarnet || _redis.Version < new Version(5, 0))
        {
            // 使用 RedisQueue 作为替代
            var queue = _redis.GetQueue<TestMessage>(key);
            queue.Add(new TestMessage { Id = 1, Text = "Hello" });

            var msg = queue.TakeOne(0);
            Assert.NotNull(msg);
            Assert.Equal(1, msg.Id);
            Assert.Equal("Hello", msg.Text);
        }
        else
        {
            // 使用 RedisStream
            var stream = _redis.GetStream<TestMessage>(key);
            stream.Add(new TestMessage { Id = 1, Text = "Hello" });

            var messages = stream.Take(1);
            var msg = messages.FirstOrDefault();
            Assert.NotNull(msg);
            Assert.Equal(1, msg.Id);
            Assert.Equal("Hello", msg.Text);
        }

        _redis.Remove(key);
    }

    class TestMessage
    {
        public Int32 Id { get; set; }
        public String Text { get; set; }
    }
}
