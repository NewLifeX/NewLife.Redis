using System.Collections.Concurrent;
using NewLife.Log;

namespace QueueDemo;

internal class MemoryQueue
{
    public static void Start()
    {
        var queue = new BlockingCollection<Area>();

        // 独立线程消费
        var source = new CancellationTokenSource();
        Task.Run(() => Consume(queue, source.Token));

        // 发布消息
        Public(queue);

        source.Cancel();
    }

    private static void Public(BlockingCollection<Area> queue)
    {
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
        Thread.Sleep(1000);
    }

    private static void Consume(BlockingCollection<Area> queue, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var msg = queue.Take(token);
            if (msg != null)
            {
                XTrace.WriteLine("Consume {0} {1}", msg.Code, msg.Name);
            }
        }
    }
}