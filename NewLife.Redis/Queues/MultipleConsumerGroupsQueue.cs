using NewLife.Data;
using NewLife.Log;

namespace NewLife.Caching.Queues;

/// <summary>
/// Redis多消费组可重复消费的队列
/// </summary>
/// <typeparam name="T">消息类型</typeparam>
public class MultipleConsumerGroupsQueue<T> : IDisposable
{
    /// <summary>
    /// Redis客户端
    /// </summary>
    private FullRedis? _Redis;

    /// <summary>
    /// 消息列队
    /// </summary>
    private RedisStream<T>? _Queue;

    /// <summary>
    /// 编码器
    /// </summary>
    public IPacketEncoder Encoder { set; get; } = new RedisJsonEncoder();

    /// <summary>
    /// 读写超时(默认15000ms)
    /// </summary>
    public Int32 TimeOut { set; get; } = 15_000;

    /// <summary>
    /// 订阅者名称
    /// </summary>
    public String? SubscribeAppName { private set; get; }

    /// <summary>
    /// 消费者组名已经存在的Redis错误消息关键词
    /// </summary>
    public String ConsumeGroupExistErrMsgKeyWord { set; get; } = "exists";

    /// <summary>
    /// 列队长度
    /// </summary>
    public Int32 QueueLen { set; get; } = 1_000_000;

    /// <summary>
    /// 忽略异常消息(在对消息进行解析时发生异常，依然对当前消息进行消费)
    /// </summary>
    public Boolean IgnoreErrMsg { set; get; } = true;

    /// <summary>
    /// 订阅消费消息后自动确认
    /// </summary>
    /// <remarks>
    /// 如改为false请确保手动调用Acknowledge确认消费。
    /// </remarks>
    public Boolean AutoConfirmConsumption { set; get; } = true;

    /// <summary>
    /// 日志对像
    /// </summary>
    public ILog Log
    {
        get => _Redis != null ? _Redis.Log : Logger.Null;
        set { if (_Redis != null) _Redis.Log = value; }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="ignoreErrMsg">忽略异常消息(在对消息进行解析时发生异常，依然对当前消息进行消费)</param>
    /// <param name="encoder">编码器</param>
    public MultipleConsumerGroupsQueue(Boolean ignoreErrMsg = true, IPacketEncoder? encoder = null)
    {
        if (encoder != null)
            Encoder = encoder;

        IgnoreErrMsg = ignoreErrMsg;
    }

    /// <summary>
    /// 连接Redis服务器o
    /// </summary>
    /// <param name="host">Redis地址</param>
    /// <param name="queueName">列队名称</param>
    /// <param name="port">端口(默认6379)</param>
    /// <param name="password">密码</param>
    /// <param name="db">连接Redis数据库</param>
    public void Connect(String host, String queueName, Int32 port = 6379, String password = "", Int32 db = 0)
    {
        _Redis = new FullRedis($"{host}:{port}", password, db) { Timeout = TimeOut, Encoder = Encoder };
        if (_Redis != null)
        {
            _Queue = _Redis.GetStream<T>(queueName);
            _Queue.MaxLength = QueueLen;
        }
        else
            throw new NullReferenceException("连接Redis服务器失败。");

    }

    /// <summary>
    /// 连接Redis服务器
    /// </summary>
    /// <param name="connStr">连接字串</param>
    /// <param name="queueName">列队名称</param>
    /// <exception cref="NullReferenceException"></exception>
    public void Connect(String connStr, String queueName)
    {
        _Redis = new FullRedis() { Timeout = TimeOut, Encoder = Encoder };
        _Redis.Init(connStr);
        if (_Redis == null) throw new NullReferenceException("连接Redis服务器失败。");

        _Queue = _Redis.GetStream<T>(queueName);
        _Queue.MaxLength = QueueLen;
    }

    /// <summary>
    /// 连接Redis服务器
    /// </summary>
    /// <param name="redis">Redis对像</param>
    /// <param name="queueName">列队名称</param>
    /// <exception cref="NullReferenceException"></exception>
    public void Connect(FullRedis redis, String queueName)
    {
        _Redis = redis;
        if (_Redis == null) throw new NullReferenceException("连接Redis服务器失败。");

        _Queue = _Redis.GetStream<T>(queueName);
        _Queue.MaxLength = QueueLen;
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="data"></param>
    public void Publish(T data) => _Queue?.Add(data);

    /// <summary>
    /// 独立线程消费
    /// </summary>
    private CancellationTokenSource? _Cts;

    /// <summary>
    /// 订阅
    /// </summary>
    /// <param name="subscribeAppName">消费者名称</param>
    public void Subscribe(String subscribeAppName)
    {
        SubscribeAppName = subscribeAppName;
        _Cts = new CancellationTokenSource();

        if (_Redis == null || _Queue == null)
        {
            OnDisconnected("订阅时列队对像为Null。");
            return;
        }

        //尝试创建消费组
        try
        {
            //_Queue.Group = subscribeAppName;
            _Queue.SetGroup(subscribeAppName);
        }
        catch (Exception err)
        {
            //遇到其它非消费组名已经存在的错误消息时，停止消费并提示消息
            if (err.Message.IndexOf(ConsumeGroupExistErrMsgKeyWord) < 0)
            {
                if (XTrace.Debug) XTrace.WriteException(err);
                OnStopSubscribe(err.Message);
                return;
            }


        }

        Task.Factory.StartNew(() => getSubscribe(subscribeAppName), _Cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <summary>
    /// 确认消费消息
    /// </summary>
    /// <param name="msgIds">消息编号</param>
    public void Acknowledge(params String[] msgIds)
    {
        _Queue?.Acknowledge(msgIds);
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public void UnSubscribe() => _Cts?.Cancel();

    /// <summary>
    /// 获取消费消息
    /// </summary>
    /// <param name="subscribeAppName">订阅APP名称</param>
    private async Task getSubscribe(String subscribeAppName)
    {
        if (_Redis == null)
        {
            OnDisconnected("Redis对像为Null。");
            return;
        }
        if (_Queue == null)
        {
            _Cts?.Cancel();
            OnDisconnected("消息列队对像为Null");
            return;
        }

        while (_Cts != null && !_Cts.IsCancellationRequested)
        {

            var msg = await _Queue.TakeMessageAsync(10).ConfigureAwait(false);
            if (msg != null && !msg.Id.IsNullOrEmpty())
            {
                try
                {
                    var data = msg.GetBody<T>();

                    //通知订阅者
                    if (data != null) OnReceived(msg.Id, data);

                    if (AutoConfirmConsumption)
                    {
                        _Queue.Acknowledge(msg.Id);
                    }
                }
                catch (Exception err)
                {

                    if (XTrace.Debug) XTrace.WriteException(err);
                    //多消费组中，假如当前消息解析异常，原因大多是因为新增加消息格式等原因导致
                    //所以都可以正常忽略，如有特殊需要配置IgnoreErrMsg为false
                    if (IgnoreErrMsg)
                    {
                        _Queue.Acknowledge(msg.Id);
                    }
                    else
                    {
                        _Cts.Cancel();
                        OnStopSubscribe(err.Message);
                        return;
                    }

                }
            }

        }
    }

    /// <summary>
    /// 销毁对像
    /// </summary>
    public void Dispose()
    {
        _Cts?.Cancel();
        _Redis?.Dispose();
        _Queue = null;
    }

    #region 事件

    /// <summary>
    /// 通知订阅者接收到新消息
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="data">命令</param>
    public delegate void ReceivedHandler(String msgId, T data);

    /// <summary>
    /// 通知订阅者接收到新消息
    /// </summary>
    public event ReceivedHandler? Received;

    /// <summary>
    /// 通知订阅者接收到新消息
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="data">消息实体信息</param>
    protected void OnReceived(String msgId, T data) => Received?.Invoke(msgId, data);

    /// <summary>
    /// 通知订阅者停止订阅
    /// </summary>
    /// <param name="msg">停止消息</param>
    public delegate void StopSubscribeHandler(String msg);

    /// <summary>
    /// 通知订阅者停止订阅
    /// </summary>
    /// <remarks>可以在这里处理重新订阅的相关业务逻辑</remarks>
    public event StopSubscribeHandler? StopSubscribe;

    /// <summary>通知订阅者停止订阅</summary>
    /// <param name="msg">停止消息</param>
    protected void OnStopSubscribe(String msg) => StopSubscribe?.Invoke(msg);

    /// <summary>
    /// 通知订阅者断开连接
    /// </summary>
    /// <param name="msg">停止消息</param>
    public delegate void DisconnectedHandler(String msg);

    /// <summary>
    /// 通知订阅者断开连接
    /// </summary>
    /// <remarks>可以在这里处理重新连接的相关业务逻辑</remarks>
    public event DisconnectedHandler? Disconnected;

    /// <summary>
    /// 通知订阅者断开连接
    /// </summary>
    /// <param name="msg">停止消息</param>
    protected void OnDisconnected(String msg) => Disconnected?.Invoke(msg);
    #endregion
}