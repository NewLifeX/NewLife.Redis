using System.Text;
using BenchmarkDotNet.Attributes;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Net;
using NewLife.Security;

namespace Benchmark;

/// <summary>RedisClient核心方法性能测试，覆盖请求序列化、类型转换、命令执行与管道模式</summary>
[SimpleJob]
[MemoryDiagnoser]
public class RedisClientBenchmark
{
    #region 属性
    private FullRedis _redis = null!;
    private BenchmarkRedisClient _client = null!;
    private Byte[] _buffer = null!;

    private String _key = null!;
    private String _value32 = null!;
    private String _value1k = null!;
    private String _value64k = null!;
    private String[] _batchKeys = null!;
    private IPacket _testPacket = null!;
    private Object[] _testObjectArray = null!;
    #endregion

    #region 构造
    [GlobalSetup]
    public void Setup()
    {
        var rds = new FullRedis();
        rds.Init("server=127.0.0.1:6379;password=;db=3;timeout=5000");
        _redis = rds;

        var server = new NetUri("tcp://127.0.0.1:6379");
        _client = new BenchmarkRedisClient(rds, server);

        // 256KB 序列化缓冲区
        _buffer = new Byte[256 * 1024];

        _key = "bench:client:" + Rand.NextString(8);
        _value32 = Rand.NextString(32);
        _value1k = Rand.NextString(1024);
        _value64k = Rand.NextString(64 * 1024);

        // 批量Key
        _batchKeys = new String[100];
        for (var i = 0; i < _batchKeys.Length; i++)
            _batchKeys[i] = "bench:pipe:" + i;

        // 用于 TryChangeType 的 IPacket
        _testPacket = rds.Encoder.Encode("12345")!;

        // 用于 TryChangeType 的 Object[]
        var pkArr = new Object[10];
        for (var i = 0; i < pkArr.Length; i++)
            pkArr[i] = rds.Encoder.Encode(i.ToString())!;
        _testObjectArray = pkArr;

        // 预热连接
        _client.Ping();
        rds.Set(_key, _value32);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _redis?.Remove(_key);
        _redis?.Remove(_batchKeys);
        _client?.Dispose();
        _redis?.Dispose();
    }
    #endregion

    #region 请求序列化

    [Benchmark(Description = "GetRequest_无参数(PING)")]
    public Int32 GetRequest_NoArgs()
    {
        return _client.BenchGetRequest(_buffer, "PING", null);
    }

    [Benchmark(Description = "GetRequest_1个参数(GET)")]
    public Int32 GetRequest_OneArg()
    {
        return _client.BenchGetRequest(_buffer, "GET", [_key]);
    }

    [Benchmark(Description = "GetRequest_2参数_32B(SET)")]
    public Int32 GetRequest_TwoArgs_32B()
    {
        return _client.BenchGetRequest(_buffer, "SET", [_key, _value32]);
    }

    [Benchmark(Description = "GetRequest_2参数_1KB(SET)")]
    public Int32 GetRequest_TwoArgs_1KB()
    {
        return _client.BenchGetRequest(_buffer, "SET", [_key, _value1k]);
    }

    [Benchmark(Description = "GetRequest_2参数_64KB(SET)")]
    public Int32 GetRequest_TwoArgs_64KB()
    {
        return _client.BenchGetRequest(_buffer, "SET", [_key, _value64k]);
    }

    [Benchmark(Description = "GetRequest_多参数(MSET×10)")]
    public Int32 GetRequest_ManyArgs()
    {
        // 模拟 MSET key1 val1 key2 val2 ...（10对）
        var args = new Object[20];
        for (var i = 0; i < 10; i++)
        {
            args[i * 2] = _batchKeys[i];
            args[i * 2 + 1] = _value32;
        }
        return _client.BenchGetRequest(_buffer, "MSET", args);
    }

    #endregion

    #region 类型转换

    [Benchmark(Description = "TryChangeType_String→Int32")]
    public Boolean TryChangeType_StringToInt()
    {
        return _client.TryChangeType("12345", typeof(Int32), out _);
    }

    [Benchmark(Description = "TryChangeType_String→Int64")]
    public Boolean TryChangeType_StringToLong()
    {
        return _client.TryChangeType("1234567890123", typeof(Int64), out _);
    }

    [Benchmark(Description = "TryChangeType_String→Boolean(OK)")]
    public Boolean TryChangeType_StringToBool()
    {
        return _client.TryChangeType("OK", typeof(Boolean), out _);
    }

    [Benchmark(Description = "TryChangeType_Packet→String")]
    public Boolean TryChangeType_PacketToString()
    {
        return _client.TryChangeType(_testPacket, typeof(String), out _);
    }

    [Benchmark(Description = "TryChangeType_ObjectArray→StringArray")]
    public Boolean TryChangeType_ArrayToStringArray()
    {
        return _client.TryChangeType(_testObjectArray, typeof(String[]), out _);
    }

    #endregion

    #region 命令执行

    [Benchmark(Description = "Execute_Ping")]
    public Boolean Execute_Ping()
    {
        return _client.Ping();
    }

    [Benchmark(Description = "Execute_Set_32B")]
    public String Execute_Set_32B()
    {
        return _client.Execute<String>("SET", _key, _value32);
    }

    [Benchmark(Description = "Execute_Set_1KB")]
    public String Execute_Set_1KB()
    {
        return _client.Execute<String>("SET", _key, _value1k);
    }

    [Benchmark(Description = "Execute_Get")]
    public String Execute_Get()
    {
        return _client.Execute<String>("GET", _key);
    }

    [Benchmark(Description = "Execute_IncrBy")]
    public Int64 Execute_IncrBy()
    {
        return _client.Execute<Int64>("INCRBY", _key + ":counter", "1");
    }

    #endregion

    #region 管道模式

    [Benchmark(Description = "Pipeline_10条SET")]
    public Object[] Pipeline_10()
    {
        _client.StartPipeline();
        for (var i = 0; i < 10; i++)
        {
            _client.Execute<String>("SET", _batchKeys[i], _value32);
        }
        return _client.StopPipeline(true);
    }

    [Benchmark(Description = "Pipeline_100条SET")]
    public Object[] Pipeline_100()
    {
        _client.StartPipeline();
        for (var i = 0; i < 100; i++)
        {
            _client.Execute<String>("SET", _batchKeys[i], _value32);
        }
        return _client.StopPipeline(true);
    }

    [Benchmark(Description = "Pipeline_100条GET")]
    public Object[] Pipeline_100_Get()
    {
        _client.StartPipeline();
        for (var i = 0; i < 100; i++)
        {
            _client.Execute<String>("GET", _batchKeys[i]);
        }
        return _client.StopPipeline(true);
    }

    #endregion
}

/// <summary>可测试的RedisClient子类，暴露受保护方法供性能测试使用</summary>
internal class BenchmarkRedisClient : RedisClient
{
    /// <summary>实例化</summary>
    /// <param name="redis">宿主</param>
    /// <param name="server">服务器地址</param>
    public BenchmarkRedisClient(Redis redis, NetUri server) : base(redis, server) { }

    /// <summary>暴露GetRequest用于性能测试</summary>
    /// <param name="buffer">缓冲区</param>
    /// <param name="cmd">命令</param>
    /// <param name="args">参数数组</param>
    /// <returns>写入字节数</returns>
    public Int32 BenchGetRequest(Byte[] buffer, String cmd, Object[] args)
        => GetRequest(buffer.AsMemory(), cmd, args);
}
