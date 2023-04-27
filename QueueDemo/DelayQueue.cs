using NewLife.Caching;
using NewLife.Log;
using NewLife.Serialization;

namespace QueueDemo;

class DelayQueue
{
    public static void Start(FullRedis redis)
    {
        var topic = "DelayQueue";

        // 独立线程消费
        var source = new CancellationTokenSource();
        Task.Run(() => ConsumeAsync(redis, topic, source.Token));
        Thread.Sleep(100);

        // 发布消息
        Public(redis, topic);

        Thread.Sleep(1500);
        source.Cancel();
        Thread.Sleep(100);
    }

    private static void Public(FullRedis redis, String topic)
    {
        var queue = redis.GetDelayQueue<Area>(topic);
        queue.Delay = 2;

        var area = new Area { Code = 110000, Name = "北京市" };
        XTrace.WriteLine("Public {0} {1}", area.Code, area.Name);
        queue.Add(area, 2);
        Thread.Sleep(1000);

        area = new Area { Code = 310000, Name = "上海市" };
        XTrace.WriteLine("Public {0} {1}", area.Code, area.Name);
        queue.Add(area);
        Thread.Sleep(1000);

        area = new Area { Code = 440100, Name = "广州市" };
        XTrace.WriteLine("Public {0} {1}", area.Code, area.Name);
        queue.Add(area);
    }

    private static async Task ConsumeAsync(FullRedis redis, String topic, CancellationToken token)
    {
        var queue = redis.GetDelayQueue<String>(topic);

        XTrace.WriteLine("Start Consume");

        while (!token.IsCancellationRequested)
        {
            try
            {
                var mqMsg = await queue.TakeOneAsync(10, token);
                if (mqMsg != null)
                {
                    var msg = mqMsg.ToJsonEntity<Area>();
                    XTrace.WriteLine("Consume {0} {1}", msg.Code, msg.Name);

                    queue.Acknowledge(mqMsg);
                }
            }
            catch (Exception ex) { }
        }

        XTrace.WriteLine("Finish Consume");
    }
}
