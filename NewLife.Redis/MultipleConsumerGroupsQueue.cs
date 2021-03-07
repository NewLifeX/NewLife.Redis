using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Caching
{
    /// <summary>
    /// Redis多消费组可重复消费的队列
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    public class MultipleConsumerGroupsQueue<T> : IDisposable
    {
        /// <summary>
        /// Redis客户端
        /// </summary>
        FullRedis _Redis;

        /// <summary>
        /// 消息列队
        /// </summary>
        RedisStream<T> _Queue;

        /// <summary>
        /// 读写超时(默认15000ms)
        /// </summary>
        public int TimeOut
        {
            set;
            get;
        } = 15_000;

        /// <summary>
        /// 订阅者名称
        /// </summary>
        public string SubscribeAppName
        {
            private set;
            get;
        }

        /// <summary>
        /// 消费者组名已经存在的Redis错误消息关键词
        /// </summary>
        public String ConsumeGroupExistErrMsgKeyWord { set; get; } = "exists";

        /// <summary>
        /// 列队长度
        /// </summary>
        public int QueueLen { set; get; } = 2000;


        /// <summary>
        /// 日志对像
        /// </summary>
        public ILog Log
        {
            set { if (_Redis != null) _Redis.Log = value; }
            get
            {
                if (_Redis != null)
                    return _Redis.Log;
                else
                    return null;
            }
        }

        /// <summary>
        /// 连接Redis服务器
        /// </summary>
        /// <param name="host">Redis地址</param>
        /// <param name="queueName">列队名称</param>
        /// <param name="port">端口(默认6379)</param>
        /// <param name="password">密码</param>
        /// <param name="db">连接Redis数据库</param>
        public void Connect(string host, string queueName, int port = 6379, string password = "", int db = 0)
        {
            _Redis = new FullRedis($"{host}:{port}", password, db) { Timeout = TimeOut };
            if (_Redis != null)
            {
                _Queue = _Redis.GetStream<T>(queueName);
                _Queue.MaxLenngth = QueueLen;
            }
            else
            {
                throw new NullReferenceException("连接Redis服务器失败。");
            }

        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="data"></param>
        public void Publish(T data)
        {
            _Queue.Add(data);
        }


        /// <summary>
        /// 独立线程消费
        /// </summary>
        CancellationTokenSource _Cts;

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="subscribeAppName">消费者名称</param>
        public void Subscribe(string subscribeAppName)
        {
            SubscribeAppName = subscribeAppName;
            _Cts = new CancellationTokenSource();

            if (_Redis == null || _Queue == null)
            {
                OnDisconnected("订阅时列队对像为Null。");
            }

            //尝试创建消费组
            try
            {
                _Queue.Group = subscribeAppName;
                _Queue.GroupCreate(subscribeAppName);
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

#if NET40
            var thread = new Thread(s => getSubscribe(subscribeAppName));
            thread.Start();
            
#else
            Task.Run(() => getSubscribe(subscribeAppName), _Cts.Token);
#endif
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void UnSubscribe()
        {
            _Cts.Cancel();
        }

        /// <summary>
        /// 获取消费消息
        /// </summary>
        /// <param name="subscribeAppName">订阅APP名称</param>
        private async Task getSubscribe(string subscribeAppName)
        {
            if (_Queue == null)
            {
                _Cts.Cancel();
                OnDisconnected("消息列队对像为Null");
                return;
            }
            while (!_Cts.IsCancellationRequested)
            {
                if (_Redis == null || _Queue == null)
                {
                    OnDisconnected("获取订阅消息时列队对像为Null。");
                }

                try
                {
                    var msg = await _Queue.TakeMessageAsync(10);
                    if (msg != null)
                    {

                        var data = msg.GetBody<T>();
                        _Queue.Acknowledge(msg.Id);
                        //通知订阅者
                        OnReceived(data);

                    }
                }
                catch (Exception err)
                {
                    if (XTrace.Debug) XTrace.WriteException(err);

                    _Cts.Cancel();
                    OnStopSubscribe(err.Message);
                    return;
                }
            }
        }

        /// <summary>
        /// 销毁对像
        /// </summary>
        public void Dispose()
        {
            if (_Cts != null) _Cts.Cancel();
            if (_Queue != null) _Queue = null;
            if (_Redis != null) _Redis.Dispose();
        }


        #region 事件

        /// <summary>
        /// 通知订阅者接收到新消息
        /// </summary>
        /// <param name="data">命令</param>
        public delegate void ReceivedHandler(T data);

        /// <summary>
        /// 通知订阅者接收到新消息
        /// </summary>
        public event ReceivedHandler Received;

        /// <summary>
        /// 通知订阅者接收到新消息
        /// </summary>
        /// <param name="data"></param>
        protected void OnReceived(T data)
        {
            Received?.Invoke(data);
        }

        /// <summary>
        /// 通知订阅者停止订阅
        /// </summary>
        /// <param name="msg">停止消息</param>
        public delegate void StopSubscribeHandler(string msg);

        /// <summary>
        /// 通知订阅者停止订阅
        /// </summary>
        /// <remarks>可以在这里处理重新订阅的相关业务逻辑</remarks>
        public event StopSubscribeHandler StopSubscribe;

        // <summary>
        /// 通知订阅者停止订阅
        /// </summary>
        /// <param name="msg">停止消息</param>
        protected void OnStopSubscribe(string msg)
        {
            StopSubscribe?.Invoke(msg);
        }

        /// <summary>
        /// 通知订阅者断开连接
        /// </summary>
        /// <param name="msg">停止消息</param>
        public delegate void DisconnectedHandler(string msg);

        /// <summary>
        /// 通知订阅者断开连接
        /// </summary>
        /// <remarks>可以在这里处理重新连接的相关业务逻辑</remarks>
        public event DisconnectedHandler Disconnected;

        /// <summary>
        /// 通知订阅者断开连接
        /// </summary>
        /// <param name="msg">停止消息</param>
        protected void OnDisconnected(string msg)
        {
            Disconnected?.Invoke(msg);
        }


        #endregion

    }
}