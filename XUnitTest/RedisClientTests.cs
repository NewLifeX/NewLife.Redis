using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest;

[Collection("Localization")]
public class RedisClientTests
{
    [Fact(DisplayName = "连接串支持IPv6和双栈开关")]
    public void InitConfigWithIPv6Options()
    {
        var redis = new Redis();
        redis.Init("server=127.0.0.1:6379;db=9;timeout=5000;IPv6=false;DualMode=false;");

        Assert.Equal("127.0.0.1:6379", redis.Server);
        Assert.Equal(9, redis.Db);
        Assert.Equal(5000, redis.Timeout);
        Assert.False(redis.IPv6);
        Assert.False(redis.DualMode);
    }

    [Fact(DisplayName = "禁用IPv6时仅保留IPv4地址")]
    public void FilterAddressesDisableIPv6()
    {
        var redis = new Redis
        {
            IPv6 = false
        };
        var client = new TestRedisClient(redis);

        var addrs = client.Filter([
            IPAddress.Loopback,
            IPAddress.IPv6Loopback,
            IPAddress.Parse("::ffff:127.0.0.1")
        ]);

        Assert.Single(addrs);
        Assert.Equal(AddressFamily.InterNetwork, addrs[0].AddressFamily);
        Assert.Equal(IPAddress.Loopback, addrs[0]);
    }

    [Fact(DisplayName = "禁用双栈时仅保留IPv6地址")]
    public void FilterAddressesDisableDualMode()
    {
        var redis = new Redis
        {
            IPv6 = true,
            DualMode = false
        };
        var client = new TestRedisClient(redis);

        var addrs = client.Filter([
            IPAddress.Loopback,
            IPAddress.IPv6Loopback,
            IPAddress.Parse("::ffff:127.0.0.1")
        ]);

        Assert.Equal(2, addrs.Length);
        Assert.All(addrs, e => Assert.Equal(AddressFamily.InterNetworkV6, e.AddressFamily));
    }

    [Fact(DisplayName = "禁用IPv6时创建IPv4客户端")]
    public void CreateClientDisableIPv6()
    {
        var redis = new Redis
        {
            IPv6 = false
        };
        using var client = new TestRedisClient(redis);
        using var tcp = client.Create(3_000);

        Assert.Equal(AddressFamily.InterNetwork, tcp.Client.AddressFamily);
    }

    [Fact(DisplayName = "禁用IPv6且无IPv4地址时抛出异常")]
    public void FilterAddressesThrowWhenMissingIPv4()
    {
        var redis = new Redis
        {
            IPv6 = false
        };
        var client = new TestRedisClient(redis);

        var ex = Assert.Throws<SocketException>(() => client.Filter([IPAddress.IPv6Loopback]));
        Assert.Equal(SocketError.AddressNotAvailable, ex.SocketErrorCode);
    }

    [Theory(DisplayName = "值类型参数估算充足不溢出")]
    [MemberData(nameof(ValueTypeArgs))]
    public void ValueTypeArgNoOverflow(Object value)
    {
        var redis = new Redis();
        var client = new TestRedisClient(redis);

        // DateTime/大Decimal序列化后约30-40字符，旧版固定估算20字节会缓冲区溢出
        var args = new Object[] { "key", value };
        var size = client.CommandSize("SET", args);
        var buf = new Byte[size];
        var n = client.Write(buf, "SET", args);

        Assert.True(n > 0);
        Assert.True(n <= buf.Length, $"写入{n}字节超过估算{size}字节，缓冲区将溢出");
    }

    public static IEnumerable<Object[]> ValueTypeArgs()
    {
        yield return [new DateTime(2026, 8, 1, 8, 30, 15, 123).AddTicks(4567)];
        yield return [Decimal.MaxValue];
        yield return [Int64.MinValue];
        yield return [Double.MaxValue];
    }

    [LocalizationFact(DisplayName = "协议解析不受区域小数点与负号影响")]
    public void ParseUsesInvariantCulture()
    {
        WithCulture("de-DE", () =>
        {
            var client = new TestRedisClient(new Redis());

            Assert.True(client.TryChangeType("12.34", typeof(Double), out var d));
            Assert.Equal(12.34d, d);

            Assert.True(client.TryChangeType("56.78", typeof(Decimal), out var m));
            Assert.Equal(56.78m, m);

            Assert.True(client.TryChangeType("2026-08-01 08:30:15", typeof(DateTime), out var dt));
            Assert.Equal(new DateTime(2026, 8, 1, 8, 30, 15), dt);

            Assert.True(client.TryChangeType("Monday", typeof(DayOfWeek), out var day));
            Assert.Equal(DayOfWeek.Monday, day);
        });

        WithCulture("ar-SA", () =>
        {
            var client = new TestRedisClient(new Redis());

            Assert.True(client.TryChangeType("-5", typeof(Int32), out var i));
            Assert.Equal(-5, i);

            Assert.True(client.TryChangeType("-7", typeof(Int64), out var l));
            Assert.Equal(-7L, l);
        });

        WithCulture("th-TH", () =>
        {
            var client = new TestRedisClient(new Redis());

            Assert.True(client.TryChangeType("2026-08-01 08:30:15", typeof(DateTime), out var dt));
            Assert.Equal(new DateTime(2026, 8, 1, 8, 30, 15), dt);
        });
    }

    [LocalizationFact(DisplayName = "协议编码不受区域小数点与日期格式影响")]
    public void EncodeUsesInvariantCulture()
    {
        WithCulture("de-DE", () =>
        {
            var client = new TestRedisClient(new Redis());
            var args = new Object[] { "key", 12.34d, -12.34d, -5L, 56.78m, new DateTime(2026, 8, 1, 8, 30, 15, 123) };
            var size = client.CommandSize("SET", args);
            var buf = new Byte[size];
            var n = client.Write(buf, "SET", args);
            var text = Encoding.UTF8.GetString(buf, 0, n);

            Assert.Contains("12.34", text);
            Assert.Contains("-12.34", text);
            Assert.Contains("-5", text);
            Assert.Contains("56.78", text);
            Assert.Contains("2026-08-01 08:30:15.123", text);
            Assert.DoesNotContain("12,34", text);
            Assert.DoesNotContain("56,78", text);
        });
    }

    [RedisFact(DisplayName = "逗号小数文化下有序集合分值往返正常")]
    public void SortedSetScoreRoundTripUnderCommaCulture()
    {
        if (!LocalizationTests.CulturesAvailable) return;

        WithCulture("de-DE", () =>
        {
            using var redis = new FullRedis();
            redis.Init(BasicTest.GetConfig());

            var key = "zset_culture_" + Guid.NewGuid().ToString("N");
            try
            {
                var zset = new RedisSortedSet<String>(redis, key);

                Assert.Equal(1, zset.Add("stone", 12.34));
                Assert.Equal(12.34, zset.GetScore("stone"));
            }
            finally
            {
                redis.Remove(key);
            }
        });
    }

    private static void WithCulture(String name, Action action)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    private class TestRedisClient : RedisClient
    {
        public TestRedisClient(Redis redis) : base(redis, new NetUri("tcp://test.invalid:6379")) { }

        public IPAddress[] Filter(IPAddress[] addrs) => base.FilterAddresses(addrs);

        public TcpClient Create(Int32 timeout) => base.CreateClient(timeout);

        /// <summary>估算命令数据包大小</summary>
        public Int32 CommandSize(String cmd, Object[] args)
        {
            IPacket?[]? encoded = null;
            return base.GetCommandSize(cmd, args, ref encoded);
        }

        /// <summary>把命令写入缓冲区，返回实际写入字节数</summary>
        public Int32 Write(Byte[] buffer, String cmd, Object[] args) => base.GetRequest(buffer.AsMemory(), cmd, args);
    }
}
