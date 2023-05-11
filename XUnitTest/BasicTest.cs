using System;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

// 所有测试用例放入一个汇编级集合，除非单独指定Collection特性
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace XUnitTest;

[Collection("Basic")]
public class BasicTest
{
    private readonly FullRedis _redis;

    public BasicTest()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Db = 2;
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    private static String _config;
    public static String GetConfig()
    {
        if (_config != null) return _config;
        lock (typeof(BasicTest))
        {
            if (_config != null) return _config;

            var config = "";
            var file = @"config\redis.config";
            if (File.Exists(file)) config = File.ReadAllText(file.GetFullPath())?.Trim();
            if (config.IsNullOrEmpty()) config = "server=127.0.0.1:6379;db=3";
            if (!File.Exists(file)) File.WriteAllText(file.EnsureDirectory(true).GetFullPath(), config);

            return _config = config;
        }
    }

    [Fact(DisplayName = "信息测试", Timeout = 1000)]
    public void InfoTest()
    {
        var inf = _redis.Execute(null, (client, k) => client.Execute<String>("info"));
        Assert.NotNull(inf);
    }

    [Fact(DisplayName = "字符串测试")]
    public void GetSet()
    {
        var ic = _redis;
        var key = "Name";

        // 添加删除
        ic.Set(key, Environment.UserName);
        ic.Append(key, "_XXX");
        var name = ic.Get<String>(key);
        Assert.Equal(Environment.UserName + "_XXX", name);

        var name2 = ic.GetRange(key, 0, Environment.UserName.Length - 1);
        Assert.Equal(Environment.UserName, name2);

        ic.SetRange(key, name.Length - 2, "YY");
        var name3 = ic.Get<String>(key);
        Assert.Equal(Environment.UserName + "_XYY", name3);

        var len = ic.StrLen(key);
        Assert.Equal((Environment.UserName + "_XYY").Length, len);
    }

    [Fact(DisplayName = "搜索测试")]
    public void SearchTest()
    {
        var ic = _redis;
        var key = "Name";
        var key2 = "Name2";

        // 添加删除
        ic.Set(key, Environment.UserName);
        ic.Rename(key, key2);
        Assert.True(ic.ContainsKey(key2));
        Assert.False(ic.ContainsKey(key));

        //var ss = ic.Search("*");
        //Assert.True(ss.Length > 0);

        var ss2 = ic.Search("Name*", 10).ToArray();
        Assert.True(ss2.Length > 0);

        //var ss3 = ic.Search("ReliableQueue:Status:*", 100).ToArray();
        //Assert.True(ss3.Length > 0);
    }

    [Fact]
    public void GetInfo()
    {
        var rds = _redis.CreateSub(0);
        var inf = rds.GetInfo(true);
        Assert.NotNull(inf);
    }
}