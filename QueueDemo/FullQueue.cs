using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Serialization;

namespace QueueDemo
{
    class FullQueue
    {
        public static void Start(FullRedis redis)
        {
            var topic = "FullQueue";
            var queue = redis.GetStream<String>(topic);

            // 独立线程消费
            var source = new CancellationTokenSource();
            Task.Run(() => ConsumeAsync(redis, topic, source.Token));

            // 发布消息
            Public(redis, topic);

            //source.Cancel();
        }

        private static void Public(FullRedis redis, String topic)
        {
            var queue = redis.GetStream<Area>(topic);

            queue.Add(new Area { Code = 110000, Name = "北京市" });
            Thread.Sleep(1000);
            queue.Add(new Area { Code = 310000, Name = "上海市" });
            Thread.Sleep(1000);
            queue.Add(new Area { Code = 440100, Name = "广州市" });
            Thread.Sleep(1000);
        }

        private static async Task ConsumeAsync(FullRedis redis, String topic, CancellationToken token)
        {
            var queue = redis.GetStream<String>(topic);
            queue.Group = "test";
            queue.GroupCreate(queue.Group);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var mqMsg = await queue.TakeMessageAsync(10);
                    if (mqMsg != null)
                    {
                        var msg = mqMsg.GetBody<Area>();
                        XTrace.WriteLine("Consume {0} {1}", msg.Code, msg.Name);

                        queue.Acknowledge(mqMsg.Id);
                    }
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }
        }
    }
}