using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.Log;
using NewLife.Serialization;

namespace QueueDemo;

class MultipleConsumer
{
    public static void Start(FullRedis redis)
    {
        var topic = "MultipleConsumer";

        var consumerName = "Consumer1";
        var mq = new MultipleConsumerGroupsQueue<CommandInfo<Object>>
        {
            //不同版本的redis错误消息关键词可能不一样，这里注意设置合适的关键词
            ConsumeGroupExistErrMsgKeyWord = "exist"
        };
        mq.Connect(redis, topic);
        mq.Received += (msgId,data) =>
        {
            XTrace.WriteLine($"[Redis多消费组]收到列队消息，ID：{msgId}，内容：{data.Data.ToJson()}");
        };
        mq.StopSubscribe += (msg) =>
        {
            //队列不存在等情况都会导致停止
            //遇到异常时停止订阅，等待5秒后重新订阅，不遗漏消息
            XTrace.WriteLine($"[Redis多消费组]停止订阅原因：{msg}");
            XTrace.WriteLine("5秒后重新自动订阅……");
            Thread.Sleep(5000);
            mq.Subscribe(consumerName);
        };
        mq.Disconnected += (msg) =>
        {
            //一般不会进入这里。（可能这个事件还可以再优化一下）
            XTrace.WriteLine($"因“{msg}”断开连接，进入重连模式。");
            mq.Connect(redis, topic);
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

        // 发布消息
        Public(mq);
    }

    private static void Public(MultipleConsumerGroupsQueue<CommandInfo<Object>> queue)
    {
        queue.Publish(new CommandInfo<Object>(1001, "命令内容"));
        Thread.Sleep(2000);

        var data1 = new DataModel() { DeviceID = "设备编号", Name = "设备名称" };
        queue.Publish(new CommandInfo<Object>(1001, data1));
        Thread.Sleep(2000);

        queue.Publish(new CommandInfo<Object>(1001, 8888));
        Thread.Sleep(2000);

        queue.Publish(new CommandInfo<Object>(1001, 999.00));
        Thread.Sleep(2000);

        queue.Publish(new CommandInfo<Object>(1001, DateTime.Today));
        //Thread.Sleep(2000);
    }

    #region Redis多消费组可重复消费的队列测试用类
    /// <summary>
    /// 数据实体类
    /// </summary>
    public class DataModel
    {
        public String DeviceID { set; get; }
        public String Name { set; get; }

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
        public CommandInfo(Int32 cmdType) => CommandType = cmdType;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdType">命令类型</param>
        /// <param name="data">数据</param>
        public CommandInfo(Int32 cmdType, T data)
        {
            CommandType = cmdType;
            Data = data;
            SendTime = DateTime.Now;
        }

        /// <summary>
        /// 命令类型
        /// </summary>
        public Int32 CommandType
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