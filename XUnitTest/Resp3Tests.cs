using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest;

/// <summary>RESP3协议测试</summary>
[Collection("Basic")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class Resp3Tests
{
    protected readonly FullRedis _redis;

    public Resp3Tests()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Db = 2;
        _redis.Retry = 0;
        _redis.Log = XTrace.Log;
    }

    [Fact(DisplayName = "RESP3协议协商")]
    public void HelloTest()
    {
        // 使用连接池获取客户端测试HELLO命令
        var client = _redis.Pool.Get();
        try
        {
            var rs = client.Hello(3);
            if (rs != null)
            {
                // 服务器支持RESP3
                Assert.True(rs.ContainsKey("server") || rs.ContainsKey("proto"));
                XTrace.WriteLine("HELLO 3 响应：");
                foreach (var item in rs)
                {
                    XTrace.WriteLine("  {0}: {1}", item.Key, item.Value);
                }
            }
            else
            {
                XTrace.WriteLine("服务器不支持HELLO命令，跳过RESP3测试");
            }
        }
        finally
        {
            _redis.Pool.Return(client);
        }
    }

    [Fact(DisplayName = "RESP3协议版本切换")]
    public void ProtocolVersionTest()
    {
        var config = BasicTest.GetConfig();

        var rds = new FullRedis();
        rds.Init(config);
        rds.Db = 2;
        rds.ProtocolVersion = 3;
        rds.Log = XTrace.Log;

        try
        {
            // 基本操作应该在RESP3下正常工作
            var key = "resp3_test_" + Rand.NextString(8);
            rds.Set(key, "hello_resp3");
            var val = rds.Get<String>(key);
            Assert.Equal("hello_resp3", val);

            // 清理
            rds.Remove(key);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command") || ex.Message.Contains("NOPROTO"))
        {
            XTrace.WriteLine("服务器不支持RESP3协议：{0}", ex.Message);
        }
    }

    [Fact(DisplayName = "RESP3计数器操作")]
    public void Resp3IncrTest()
    {
        var config = BasicTest.GetConfig();

        var rds = new FullRedis();
        rds.Init(config);
        rds.Db = 2;
        rds.ProtocolVersion = 3;

        try
        {
            var key = "resp3_incr_" + Rand.NextString(8);
            rds.Set(key, 0);
            var val = rds.Increment(key, 5);
            Assert.Equal(5, val);

            val = rds.Increment(key, 3);
            Assert.Equal(8, val);

            rds.Remove(key);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command") || ex.Message.Contains("NOPROTO"))
        {
            XTrace.WriteLine("服务器不支持RESP3协议：{0}", ex.Message);
        }
    }

    [Fact(DisplayName = "RESP3哈希操作")]
    public void Resp3HashTest()
    {
        var config = BasicTest.GetConfig();

        var rds = new FullRedis();
        rds.Init(config);
        rds.Db = 2;
        rds.ProtocolVersion = 3;

        try
        {
            var key = "resp3_hash_" + Rand.NextString(8);
            var hash = rds.GetDictionary<String>(key) as RedisHash<String, String>;
            Assert.NotNull(hash);

            hash.Add("field1", "value1");
            hash.Add("field2", "value2");
            hash.Add("field3", "value3");

            var all = hash.GetAll();
            Assert.Equal(3, all.Count);
            Assert.Equal("value1", all["field1"]);
            Assert.Equal("value2", all["field2"]);

            rds.Remove(key);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command") || ex.Message.Contains("NOPROTO"))
        {
            XTrace.WriteLine("服务器不支持RESP3协议：{0}", ex.Message);
        }
    }

    [Fact(DisplayName = "RESP3批量操作")]
    public void Resp3BatchTest()
    {
        var config = BasicTest.GetConfig();

        var rds = new FullRedis();
        rds.Init(config);
        rds.Db = 2;
        rds.ProtocolVersion = 3;

        try
        {
            var prefix = "resp3_batch_" + Rand.NextString(8);
            var dic = new Dictionary<String, String>
            {
                [$"{prefix}_k1"] = "v1",
                [$"{prefix}_k2"] = "v2",
                [$"{prefix}_k3"] = "v3",
            };

            rds.SetAll(dic);
            var result = rds.GetAll<String>(dic.Keys.ToArray());

            Assert.Equal(3, result.Count);
            Assert.Equal("v1", result[$"{prefix}_k1"]);
            Assert.Equal("v2", result[$"{prefix}_k2"]);
            Assert.Equal("v3", result[$"{prefix}_k3"]);

            rds.Remove(dic.Keys.ToArray());
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command") || ex.Message.Contains("NOPROTO"))
        {
            XTrace.WriteLine("服务器不支持RESP3协议：{0}", ex.Message);
        }
    }

    [Fact(DisplayName = "RESP3协议降级")]
    public void Resp3FallbackTest()
    {
        // 即使设置了RESP3，如果服务器不支持，应该自动降级到RESP2
        var config = BasicTest.GetConfig();

        var rds = new FullRedis();
        rds.Init(config);
        rds.Db = 2;
        rds.ProtocolVersion = 3;

        var key = "resp3_fallback_" + Rand.NextString(8);
        rds.Set(key, "test_value");
        var val = rds.Get<String>(key);
        Assert.Equal("test_value", val);

        rds.Remove(key);
    }

    [Fact(DisplayName = "RESP3连接字符串配置")]
    public void Resp3ConfigTest()
    {
        var rds = new FullRedis();
        rds.Init("server=127.0.0.1:6379;db=2;ProtocolVersion=3");

        Assert.Equal(3, rds.ProtocolVersion);
    }

    [Fact(DisplayName = "RESP3选项配置")]
    public void Resp3OptionsTest()
    {
        var options = new RedisOptions
        {
            Server = "127.0.0.1:6379",
            Db = 2,
            ProtocolVersion = 3
        };

        var rds = new FullRedis(options);
        Assert.Equal(3, rds.ProtocolVersion);
    }
}
