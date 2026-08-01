using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Net;
using Xunit;

namespace XUnitTest;

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
