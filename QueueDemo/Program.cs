using System;
using System.Threading;
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
            redis.Init("server=centos.newlifex.com:6000;password=Pass@word;db=7");

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

            //Redis多消费组可重复消费的队列
            var consumerName = "Consumer1";
            var mq = new MultipleConsumerGroupsQueue<string>();
            mq.ConsumeGroupExistErrMsgKeyWord = "exist"; //不同版本的redis错误消息关键词可能不一样，这里注意设置合适的关键词
            mq.Connect("centos.newlifex.com", "MultipleConsumerGroupsQueue", 6000, "Pass@word", 1);
            mq.Received += (data) => { XTrace.WriteLine($"[Redis多消费组可重复消费的队列]收到列队消息：{data}"); };
            mq.StopSubscribe += (msg) =>
            {
                //队列不存在等情况都会导致停止
                //遇到异常时停止订阅，等待5秒后重新订阅，不遗漏消息
                XTrace.WriteLine($"[Redis多消费组可重复消费的队列]停止订阅原因：{msg}");
                XTrace.WriteLine("5秒后重新自动订阅……");
                Thread.Sleep(5000);
                mq.Subscribe(consumerName);
            };
            mq.Subscribe(consumerName); //开始订阅消息

            //多消费组可重复消费消息发布
            for (var i = 0; i < 10; i++) {
                mq.Publish($"多消息列队的消息{i}");
                Thread.Sleep(2000);
            }


            Console.ReadLine();
        }
    }
}