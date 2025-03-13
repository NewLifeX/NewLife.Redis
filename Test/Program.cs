using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Caching;
using NewLife.Caching.Clusters;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;

namespace Test;

class Program
{
    static void Main(String[] args)
    {
        XTrace.UseConsole();

        try
        {
            //TestHyperLogLog();
            Test2();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        Runtime.FreeMemory();

        Console.WriteLine("OK!");
        Console.ReadKey();
    }

    static void Test1()
    {
        //var services = new ServiceCollection();
        //services.AddRedis("test1", "server=127.0.0.1;db=9");
        //services.AddRedis("test2", "server=127.0.0.1;db=9");
        //var provider = services.BuildServiceProvider();

        //var ic = provider.GetRequiredService<Redis>();
        //var ic = provider.GetServices<Redis>().FirstOrDefault(e => e.Name == "test1");
        var ic = new FullRedis("127.0.0.1:6379", null, 3);
        //var ic = new FullRedis();
        //ic.Server = "127.0.0.1:6379";
        //ic.Db = 3;
        ic.Log = XTrace.Log;

        // 简单操作
        Console.WriteLine("共有缓存对象 {0} 个", ic.Count);

        ic.Set("name", "大石头");
        Console.WriteLine(ic.Get<String>("name"));

        var ks = ic.Execute(null, (c, k) => c.Execute<String[]>("KEYS", "*"));
        var keys = ic.Keys;

        ic.Set("time", DateTime.Now, 1);
        Console.WriteLine(ic.Get<DateTime>("time").ToFullString());
        Thread.Sleep(1100);
        Console.WriteLine(ic.Get<DateTime>("time").ToFullString());

        // 列表
        var list = ic.GetList<DateTime>("list");
        list.Add(DateTime.Now);
        list.Add(DateTime.Now.Date);
        list.RemoveAt(1);
        Console.WriteLine(list[list.Count - 1].ToFullString());

        // 字典
        var dic = ic.GetDictionary<DateTime>("dic");
        dic.Add("xxx", DateTime.Now);
        Console.WriteLine(dic["xxx"].ToFullString());

        // 队列
        var mq = ic.GetQueue<String>("queue");
        mq.Add(new[] { "abc", "g", "e", "m" });
        var arr = mq.Take(3);
        Console.WriteLine(arr.Join(","));

        // 集合
        var set = ic.GetSet<String>("181110_1234");
        set.Add("xx1");
        set.Add("xx2");
        set.Add("xx3");
        Console.WriteLine(set.Count);
        Console.WriteLine(set.Contains("xx2"));


        Console.WriteLine("共有缓存对象 {0} 个", ic.Count);
    }

    /// <summary>性能压测</summary>
    static void Test2()
    {
        var args = Environment.GetCommandLineArgs();
        var cp = new CommandParser();
        var dic = cp.Parse(args);

        if (!dic.TryGetValue("server", out var server)) server = "127.0.0.1";
        if (!dic.TryGetValue("pass", out var pass)) pass = "";

        if (server.IsNullOrEmpty()) server = "127.0.0.1";

        var ic = new FullRedis(server, pass, 3);

        // 性能压测
        //ic.AutoPipeline = -1;
        ic.Bench();

        //Thread.Sleep(1000);

        //Console.WriteLine();
        //var dic = ic.GetInfo();
        //foreach (var item in dic)
        //{
        //    Console.WriteLine("{0}:\t{1}", item.Key, item.Value);
        //}
    }

    static void Test3()
    {
        var redis = new FullRedis("127.0.0.1:6379", null, 3);
        //redis.Log = XTrace.Log;

        //var services = new ServiceCollection();
        //services.AddRedis(options =>
        //{
        //    options.Server = "127.0.0.1:6379";
        //    options.Db = 3;
        //});

        //var sp = services.BuildServiceProvider();
        //var redis = sp.GetService<FullRedis>();

        var key = "ReliableQueue_unique";

        var hash = new HashSet<String>();

        for (var i = 0; i < 1_000_000; i++)
        {
            var q = redis.GetReliableQueue<String>(key);

            //Assert.DoesNotContain(q.AckKey, hash);
            var rs = hash.Contains(q.AckKey);

            hash.Add(q.AckKey);
        }
    }

    static void Test4()
    {
        var rds = new FullRedis("127.0.0.1:6001", null, 0);
        rds.Log = XTrace.Log;
        //rds.Init(null);

        Thread.Sleep(1000);

        var fr = rds as FullRedis;
        var cluster = fr.Cluster as RedisCluster;

        cluster.Meet("127.0.0.1", 6002);
        cluster.Meet("127.0.0.1", 6003);
        cluster.Meet("127.0.0.1", 6004);

        Thread.Sleep(1000);

        cluster.Rebalance();

        rds.Set("name", "Stone");

        var name = rds.Get<String>("name");
    }

    class User
    {
        public String Name { get; set; }
        public DateTime CreateTime { get; set; }
    }
    static void Test5()
    {
        var user = new User { Name = "NewLife", CreateTime = DateTime.Now };
        var rds = new FullRedis("127.0.0.1:7002,127.0.0.1:6003", null, 2);
        rds.Log = XTrace.Log;
        rds.Set("user", user, 3600);
        var user2 = rds.Get<User>("user");
        XTrace.WriteLine("Json: {0}", user2.ToJson());
        XTrace.WriteLine("Json: {0}", rds.Get<String>("user"));
        if (rds.ContainsKey("user")) XTrace.WriteLine("存在！");
        //rds.Remove("user");

        for (var i = 0; i < 1000; i++)
        {
            try
            {
                user2 = rds.Get<User>("user");
                XTrace.WriteLine("Json: {0}", user2.ToJson());
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            Thread.Sleep(1000);
        }
    }

    static async void Test6()
    {
        var user = new User { Name = "NewLife", CreateTime = DateTime.Now };
        var rds = new FullRedis("127.0.0.1:6002,127.0.0.1:6003", null, 0);
        //rds.Log = XTrace.Log;

        var rn = new PubSub(rds, "__sentinel__:hello");
        await rn.SubscribeAsync((k, v) =>
        {
            //XTrace.WriteLine("k={0} v={1}", k, v);
            //XTrace.WriteLine(v);
            var inf = SentinelInfo.Parse(v);
            XTrace.WriteLine(inf.ToJson());
        });
    }

    static void TestHyperLogLog()
    {
        var rds = new FullRedis("127.0.0.1", null, 1);

        rds.Remove("ips");
        var log = new HyperLogLog(rds, "ips");

        XTrace.WriteLine("log.Count={0:n0}", log.Count);

        var count = 1_000_000;
        XTrace.WriteLine("准备添加[{0:n0}]个IP地址", count);
        Parallel.For(0, count, k =>
        {
            var n = Rand.Next();
            var ip = new IPAddress(n);
            log.Add(ip + "");
        });
        XTrace.WriteLine("log.Count={0:n0}", log.Count);
    }

    /// <summary>
    /// 测试列表相关命令
    /// LPUSH、RPUSH、BLPOP、RLPOP
    /// </summary>
    static void TestList()
    {
        //TODO 使用模型        
        FullRedis fullRedis = new FullRedis("127.0.0.1:6379", "", 1);
        fullRedis.Log = XTrace.Log;
        #region 以对象方式读写
        //LPUSH
        fullRedis.LPUSH("vm", new VmModel[] { new VmModel() {
        Id=Guid.NewGuid(),
        Name="测试1"
        },new VmModel (){
        Id=Guid.NewGuid(),
        Name="测试2"
        } ,new VmModel (){
        Id=Guid.NewGuid(),
        Name="测试3"
        }});

        //RPUSH
        fullRedis.RPUSH("vm", new VmModel[] { new VmModel() {
        Id=Guid.NewGuid(),
        Name="测试4"
        },new VmModel (){
        Id=Guid.NewGuid(),
        Name="测试5"
        } ,new VmModel (){
        Id=Guid.NewGuid(),
        Name="测试6"
        }});

        //BLPOP
        fullRedis.Timeout = 20000;  //需要注意BLPOP、RLPOP在列表无元素时候会阻塞进程，超时时间不能超过FullRedis默认的Timeout时间
        var vm = fullRedis.BLPOP<VmModel>("vm", 10);
        if (vm != null)
        {
            Console.WriteLine($"BLPOP得到的vm名称:{vm.Name}");
        }
        //BRPOP
        vm = fullRedis.BRPOP<VmModel>("vm", 10);
        if (vm != null)
        {
            Console.WriteLine($"BRPOP得到的vm名称:{vm.Name}");
        }
        //RPOPLPUSH
        vm = fullRedis.RPOPLPUSH<VmModel>("vm", "vmback");
        Console.WriteLine($"RPOPLPUSH的结果值:{vm.Name}");

        //BRPOPLPUSH
        vm = fullRedis.BRPOPLPUSH<VmModel>("vm", "vmback1", 2);
        Console.WriteLine($"BRPOPLPUSH的结果值:{vm.Name}");

        #endregion

        #region 以常规数据类型读写
        //LPUSH
        fullRedis.LPUSH("test", new int[] { 1, 2, 3 });
        //RPUSH
        fullRedis.RPUSH("test", new int[] { 4, 5, 6 });
        //BLPOP
        var testInt = fullRedis.BLPOP<int>("test", 10);
        if (testInt > 0)
        {
            Console.WriteLine($"BLPOP得到的testInt:{testInt.ToString()}");
        }
        //BRPOP
        testInt = fullRedis.BRPOP<int>("test", 10);
        if (testInt > 0)
        {
            Console.WriteLine($"BRPOP得到的testInt:{testInt.ToString()}");
        }
        //RPOPLPUSH
        var rpopTest = fullRedis.RPOPLPUSH<int>("test", "testback");
        Console.WriteLine($"RPOPLPUSH的结果值:{rpopTest.ToString()}");

        //BRPOPLPUSH
        var brpopTest = fullRedis.BRPOPLPUSH<int>("test", "testback1", 2);
        Console.WriteLine($"BRPOPLPUSH的结果值:{brpopTest.ToString()}");
        #endregion

    }

}

public class VmModel
{
    public Guid Id { get; set; }

    public string Name { get; set; }
}