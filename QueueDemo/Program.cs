using System;
using NewLife.Caching;
using NewLife.Log;

namespace QueueDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            // 初始化Redis
            var redis = new FullRedis { Timeout = 15_000, Log = XTrace.Log };
            redis.Init("server=127.0.0.1;passowrd=;db=9");

            XTrace.WriteLine("Redis Queue Demo! Keys= {0}", redis.Count);

            // 普通队列
            EasyQueue.Start(redis);

            Console.WriteLine("Hello World!");
        }
    }
}