using NewLife.Caching;
using NewLife.Log;

namespace QueueDemo;

class Program
{
    static void Main(String[] args)
    {
        XTrace.UseConsole();

        var connStr = "server=redis.newlifex.com;password=Pass@word;db=7";

        // 初始化Redis
        var redis = new FullRedis { Timeout = 15_000 };
        redis.Init(connStr);
        redis.Log = XTrace.Log;
#if DEBUG
        redis.ClientLog = XTrace.Log;
#endif

        XTrace.WriteLine("Redis Queue Demo! Keys= {0}", redis.Count);

        //内存队列
        Console.WriteLine();
        Console.WriteLine("内存队列 BlockingCollection");
        MemoryQueue.Start();

        //普通队列
        Console.WriteLine();
        Console.WriteLine("普通队列 RedisQueue");
        EasyQueue.Start(redis);

        //需要确认的可信队列
        Console.WriteLine();
        Console.WriteLine("可信队列 RedisReliableQueue （需要确认）");
        AckQueue.Start(redis);

        //延迟队列
        Console.WriteLine();
        Console.WriteLine("延迟队列 RedisDelayQueue");
        DelayQueue.Start(redis);

        //完整队列
        Console.WriteLine();
        Console.WriteLine("完整队列 RedisStream");
        FullQueue.Start(redis);

        //Redis多消费组可重复消费的队列
        Console.WriteLine();
        Console.WriteLine("多消费组 MultipleConsumerGroupsQueue");
        MultipleConsumer.Start(redis, connStr);

        Console.WriteLine("Finish!");
        Console.ReadLine();
    }
}