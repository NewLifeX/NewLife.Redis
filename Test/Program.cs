using System;
using System.Threading;
using NewLife.Caching;
using NewLife.Log;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            var cfg = CacheConfig.Current;
            var set = cfg.GetOrAdd("local");
            if (set.Provider.IsNullOrEmpty())
            {
                set.Value = "127.0.0.1:6379";
                set.Provider = "redis";
                cfg.Save();
            }
            set = cfg.GetOrAdd("memory");
            if (set.Provider.IsNullOrEmpty())
            {
                set.Provider = "memory";
                cfg.Save();
            }

            //var ic = Cache.Default;
            var ic = Cache.Create("local");
            //var ic = Cache.Create("memory");

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
            Console.WriteLine(list[list.Count - 1].ToFullString());

            // 字典
            var dic = ic.GetDictionary<DateTime>("dic");
            dic.Add("xxx", DateTime.Now);
            Console.WriteLine(dic["xxx"].ToFullString());

            Console.WriteLine("共有缓存对象 {0} 个", ic.Count);

            Console.ReadKey();
        }
    }
}