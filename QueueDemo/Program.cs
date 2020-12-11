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

            // 内存队列
            Console.Clear();
            MemoryQueue.Start();

            // 普通队列
            Console.Clear();
            EasyQueue.Start(redis);

            // 需要确认的可信队列
            Console.Clear();
            AckQueue.Start(redis);

            // 延迟队列
            Console.Clear();
            DelayQueue.Start(redis);

            var redis2 = new FullRedis { Timeout = 15_000, Log = XTrace.Log };
            redis2.Init("server=centos.newlifex.com:6000;password=Pass@word;db=7");

            XTrace.WriteLine("Redis 5.0! Keys= {0}", redis2.Count);

            // 完整队列
            Console.Clear();
            FullQueue.Start(redis2);

            Console.ReadLine();
        }
    }
}