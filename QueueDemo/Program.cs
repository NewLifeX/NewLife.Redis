using System.Data;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Serialization;

namespace QueueDemo;

class Program
{
    static void Main(string[] args)
    {
        XTrace.UseConsole();

        var connStr = "server=redis.newlifex.com;password=Pass@word;db=7";

        // 初始化Redis
        var redis = new FullRedis { Timeout = 15_000 };
        redis.Init(connStr);
#if DEBUG
        redis.Log = XTrace.Log;
#endif

        XTrace.WriteLine("Redis Queue Demo! Keys= {0}", redis.Count);

        //内存队列
        Console.WriteLine();
        Console.WriteLine("内存队列");
        MemoryQueue.Start();

        //普通队列
        Console.WriteLine();
        Console.WriteLine("普通队列 RedisQueue");
        EasyQueue.Start(redis);

        //需要确认的可信队列
        Console.WriteLine();
        Console.WriteLine("需要确认的可信队列 RedisReliableQueue");
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
        var consumerName = "Consumer1";
        var mq = new MultipleConsumerGroupsQueue<CommandInfo<object>>();
        mq.ConsumeGroupExistErrMsgKeyWord = "exist"; //不同版本的redis错误消息关键词可能不一样，这里注意设置合适的关键词
        mq.Connect("127.0.0.1", "BCGCommandQueue", 6379, "", 0);
        mq.Received += (data) => { 
            XTrace.WriteLine($"[Redis多消费组可重复消费的队列]收到列队消息：{data.Data.ToJson()}"); 
        };
        mq.StopSubscribe += (msg) =>
        {
            //队列不存在等情况都会导致停止
            //遇到异常时停止订阅，等待5秒后重新订阅，不遗漏消息
            XTrace.WriteLine($"[Redis多消费组可重复消费的队列]停止订阅原因：{msg}");
            XTrace.WriteLine("5秒后重新自动订阅……");
            Thread.Sleep(5000);
            mq.Subscribe(consumerName);
        };
        mq.Disconnected += (msg) =>
        {
            //一般不会进入这里。（可能这个事件还可以再优化一下）
            XTrace.WriteLine($"因“{msg}”断开连接，进入重连模式。");
            mq.Connect("centos.newlifex.com", "MultipleConsumerGroupsQueue", 6000, "Pass@word", 7);
        };
        mq.Subscribe(consumerName); //开始订阅消息

        //多消费组可重复消费消息发布
        //for (var i = 0; i < 2; i++)
        //{
        //    var data = new DataModel() { DeviceID = $"{i}" , Name = $"命令内容{i}" };
        //    mq.Publish(new CommandInfo<object>(1001, data));
        //    Thread.Sleep(2000);
        //}

        //Console.WriteLine("天哪".ToJsonEntity(typeof(object))); //会异常


        mq.Publish(new CommandInfo<object>(1001, "命令内容"));
        Thread.Sleep(2000);

        var data1 = new DataModel() { DeviceID = "设备编号", Name = "设备名称" };
        mq.Publish(new CommandInfo<object>(1001, data1));
        Thread.Sleep(2000);

        mq.Publish(new CommandInfo<object>(1001, 8888));
        Thread.Sleep(2000);

        mq.Publish(new CommandInfo<object>(1001, 999.00));
        Thread.Sleep(2000);

        mq.Publish(new CommandInfo<object>(1001, DateTime.Today));
        Thread.Sleep(2000);

        Console.WriteLine("Finish!");
        Console.ReadLine();
    }


    #region Redis多消费组可重复消费的队列测试用类
    /// <summary>
    /// 数据实体类
    /// </summary>
    public class DataModel
    {
        public string DeviceID { set; get; }
        public string Name { set; get; }

        public override String ToString() => $"DeviceID:{DeviceID},Name:{Name}";
    }

    /// <summary>
    /// 测试实体类信息
    /// </summary>
    public class CommandInfo<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public CommandInfo()
        {

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdType">命令类型</param>
        public CommandInfo(int cmdType)
        {
            CommandType = cmdType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdType">命令类型</param>
        /// <param name="data">数据</param>
        public CommandInfo(int cmdType, T data)
        {
            CommandType = cmdType;
            Data = data;
            SendTime = DateTime.Now;
        }

        /// <summary>
        /// 命令类型
        /// </summary>
        public int CommandType
        {
            set;
            get;
        } = 0;

        /// <summary>
        /// 命令参数
        /// </summary>
        public T Data
        {
            set;
            get;
        }

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SendTime
        {
            set;
            get;
        } = DateTime.Now;
    }

    #endregion
}