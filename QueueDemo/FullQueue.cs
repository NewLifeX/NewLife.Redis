using NewLife;
using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.Log;

namespace QueueDemo;

class FullQueue
{
    public static void Start(FullRedis redis)
    {
        var topic = "FullQueue";

        // 删除已有数据
        redis.Remove(topic);

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

        // 再来一个消费组，批量消费
        {
            var queue = redis.GetStream<Area>(topic);
            queue.Group = "Group3";
            _ = queue.ConsumeAsync(OnConsume3, 2, source.Token);
        }
        {
            var queue = redis.GetStream<Area>(topic);
            queue.Group = "Group4";
            _ = queue.ConsumeAsync(OnConsume4, 2, source.Token);
        }

        Thread.Sleep(1000);
        source.Cancel();
        Thread.Sleep(100);
    }

    private static void Public(FullRedis redis, String topic)
    {
        var queue = redis.GetStream<Area>(topic);

        var area = new Area { Code = 110000, Name = "北京市" };
        XTrace.WriteLine("Public {0} {1}", area.Code, area.Name);
        queue.Add(area);
        Thread.Sleep(1000);

        area = new Area { Code = 310000, Name = "上海市" };
        XTrace.WriteLine("Public {0} {1}", area.Code, area.Name);
        queue.Add(area);
        Thread.Sleep(1000);

        area = new Area { Code = 440100, Name = "广州市" };
        XTrace.WriteLine("Public {0} {1}", area.Code, area.Name);
        queue.Add(area);
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

    private static void OnConsume3(Area[] areas)
    {
        XTrace.WriteLine("Group3.Consume 消费[{0}]条 {1}", areas.Length, areas.Join(",", e => e.Name));
    }

    private static Task OnConsume4(Area[] areas, Message[] messages, CancellationToken token)
    {
        XTrace.WriteLine("Group4.Consume 消费[{0}]条 id={1}", areas.Length, messages.Join(",", e => e.Id));

        return Task.CompletedTask;
    }
}