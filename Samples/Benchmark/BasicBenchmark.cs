using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using NewLife.Caching;
using NewLife.Security;

namespace Benchmark;

[SimpleJob(RunStrategy.ColdStart, iterationCount: 1)]
[MemoryDiagnoser]
public class BasicBenchmark
{
    public FullRedis Redis { get; set; }

    private String _key;
    private String[] _keys;

    [GlobalSetup]
    public void Setup()
    {
        var rds = new FullRedis
        {
            Tracer = DefaultTracer.Instance,
            Log = XTrace.Log,
        };
        rds.Init("server=127.0.0.1:6379;password=;db=3;timeout=5000");

        Redis = rds;

        _key = Rand.NextString(16);
        var ks = new String[100_000];
        for (var i = 0; i < ks.Length; i++)
        {
            ks[i] = Rand.NextString(16);
        }
        _keys = ks;

        rds.Set(_key, _key);
        var v = rds.Get<String>(_key);
        rds.Remove(_key);
    }

    [Benchmark]
    public void SetTest()
    {
        var rds = Redis;
        var value = Rand.NextString(16);

        for (var i = 0; i < _keys.Length; i++)
        {
            rds.Set(_keys[i], value);
        }
    }

    [Benchmark]
    public void GetTest()
    {
        var rds = Redis;

        for (var i = 0; i < _keys.Length; i++)
        {
            var value = rds.Get<String>(_keys[i]);
        }
    }

    [Benchmark]
    public void RemoveTest()
    {
        var rds = Redis;

        for (var i = 0; i < _keys.Length; i++)
        {
            rds.Remove(_keys[i]);
        }
    }
}
