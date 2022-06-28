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

        // 发布消息
        Public(redis, topic);

        source.Cancel();
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

    private static void Consume(IProducerConsumer<Area> queue, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var msg = queue.TakeOne(10);
            if (msg != null)
            {
                XTrace.WriteLine("Consume {0} {1}", msg.Code, msg.Name);
            }
        }
    }
}