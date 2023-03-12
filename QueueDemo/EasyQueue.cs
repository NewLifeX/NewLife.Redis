using NewLife.Caching;
using NewLife.Log;

namespace QueueDemo;

class EasyQueue
{
    public static void Start(FullRedis redis)
    {
        var topic = "EasyQueue";

        // 独立线程消费
        var queue = redis.GetQueue<Area>(topic);
        var source = new CancellationTokenSource();
        Task.Run(() => Consume(queue, source.Token));
        Thread.Sleep(100);

        // 发布消息
        Public(redis, topic);

        source.Cancel();
        Thread.Sleep(100);
    }

    private static void Public(FullRedis redis, String topic)
    {
        var queue = redis.GetQueue<Area>(topic);

        queue.Add(new Area { Code = 110000, Name = "北京市" });
        Thread.Sleep(1000);
        queue.Add(new Area { Code = 310000, Name = "上海市" });
        Thread.Sleep(1000);
        queue.Add(new Area { Code = 440100, Name = "广州市" });
        Thread.Sleep(1000);
    }

    private static async Task Consume(IProducerConsumer<Area> queue, CancellationToken token)
    {
        XTrace.WriteLine("Start Consume");
        try
        {
            while (!token.IsCancellationRequested)
            {
                var msg = await queue.TakeOneAsync(10, token);
                if (msg != null)
                {
                    XTrace.WriteLine("Consume {0} {1}", msg.Code, msg.Name);
                }
            }
        }
        catch (OperationCanceledException) { }
        XTrace.WriteLine("Finish Consume");
    }
}