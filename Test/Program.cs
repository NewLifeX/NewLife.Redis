using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;

namespace Test
{
    class Program
    {
        static void Main(String[] args)
        {
            XTrace.UseConsole();

            FullRedis.Register();

            Test3();

            Console.ReadKey();
        }

        static void Test1()
        {
            var ic = Redis.Create("127.0.0.1:6000", 3);
            ic.Log = XTrace.Log;

            // 简单操作
            Console.WriteLine("共有缓存对象 {0} 个", ic.Count);

            ic.Set("name", "大石头");
            Console.WriteLine(ic.Get<String>("name"));

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

            Console.WriteLine("共有缓存对象 {0} 个", ic.Count);
        }

        static void Test2()
        {
            var ic = Redis.Create("127.0.0.1", 3);

            // 简单操作
            Console.WriteLine("共有缓存对象 {0} 个", ic.Count);

            var count = 2000000;
            Console.WriteLine("准备插入缓存{0:n0}项", count);
            var sw = Stopwatch.StartNew();

            var prg = 0;
            var t = new TimerX(s =>
            {
                XTrace.WriteLine("已处理 {0:n0} 进度 {1:p2} 速度 {2:n0}tps", prg, (Double)prg / count, prg * 1000 / sw.ElapsedMilliseconds);
            }, null, 1000, 1000);

            var buf = Rand.NextBytes(2800);
            for (var i = 0; i < count; i++)
            {
                var key = "BILL:" + (i + 1).ToString("000000000000");
                ic.Set(key, buf, 48 * 3600);

                prg++;
            }

            t.TryDispose();
        }

        static void Test3()
        {
            var ic = Redis.Create("127.0.0.1:6000", 3);
            //ic.Log = XTrace.Log;

            var list = ic.GetList<String>("kkk");
            for (var i = 0; i < 100; i++)
            {
                list.Add(Rand.NextString(256));
            }
            ic.SetExpire("kkk", TimeSpan.FromSeconds(120));

            var arr = list.ToArray();
            Console.WriteLine(arr.Length);
            foreach (var item in arr)
            {
                Console.WriteLine(item);
            }
        }
    }
}