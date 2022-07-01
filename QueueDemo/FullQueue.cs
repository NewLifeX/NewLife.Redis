using NewLife.Caching;
using NewLife.Caching.Models;
using NewLife.Log;

namespace QueueDemo;

class FullQueue
{
    public static void Start(FullRedis redis)
    {
        var topic = "FullQueue";

        // 两个消费组各自独立消费
        var source = new CancellationTokenSource();
        {
            var queue = redis.GetStream<Area>(topic);
            queue.Group = "Group1";

            _ = queue.ConsumeAsync(OnConsume, source.Token);
        }
        {
            var queue = redis.GetStream<Area>(topic);
            queue.Group = "Group2";

            _ = queue.ConsumeAsync(OnConsume2, source.Token);
        }

        // 发布消息
        Public(redis, topic);

        Thread.Sleep(1000);
        source.Cancel();
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

    private static void OnConsume(Area area)
    {
        XTrace.WriteLine("Group1.Consume {0} {1}", area.Code, area.Name);
    }

    private static Task OnConsume2(Area area, Message message, CancellationToken token)
    {
        XTrace.WriteLine("Group2.Consume {0} {1} Id={2}", area.Code, area.Name, message.Id);

        return Task.CompletedTask;
    }
}