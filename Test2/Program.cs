using NewLife.Caching;
using NewLife.Log;
using System;
using System.Threading;

namespace Test2
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            // 激活FullRedis，否则Redis.Create会得到默认的Redis对象
            FullRedis.Register();

            Test1();

            Console.ReadKey();
        }

        private static void Test1()
        {
            var ic = Redis.Create("127.0.0.1:6379", 3);
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

            // 集合
            var set = ic.GetSet<String>("sss");
            set.Add("xx1");
            set.Add("xx2");
            set.Add("xx3");
            Console.WriteLine(set.Count);
            Console.WriteLine(set.Contains("xx2"));

            Console.WriteLine("共有缓存对象 {0} 个", ic.Count);
        }
    }
}
