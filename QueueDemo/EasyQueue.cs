using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Log;

namespace QueueDemo
{
    class EasyQueue
    {
        public static void Start(FullRedis redis)
        {
            Console.Clear();

            var topic = "EasyQueue";

            // 独立线程消费
            var thread = new Thread(s => Consume(redis, topic));
            thread.Start();

            // 发布消息
            Public(redis, topic);
        }

        private static void Public(FullRedis redis, String topic)
        {
            var queue = redis.GetQueue<Area>(topic);

            queue.Add(new Area { Code = 110000, Name = "北京市" });
            Thread.Sleep(500);
            queue.Add(new Area { Code = 310000, Name = "上海市" });
            Thread.Sleep(500);
            queue.Add(new Area { Code = 440100, Name = "广州市" });
            Thread.Sleep(500);
        }

        private static void Consume(FullRedis redis, String topic)
        {
            var queue = redis.GetQueue<Area>(topic);

            while (true)
            {
                var msg = queue.TakeOne(10);
                if (msg != null)
                {
                    XTrace.WriteLine("Consume {0} {1}", msg.Code, msg.Name);
                }
            }
        }
    }
}