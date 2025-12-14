using NewLife.Caching.Queues;
using NewLife.Log;
using NewLife.Messaging;
using System.Diagnostics.CodeAnalysis;

namespace NewLife.Caching.Services;

/// <summary>Redis事件上下文</summary>
public class RedisEventContext<TEvent>(IEventBus<TEvent> eventBus, Queues.Message message) : IEventContext<TEvent>
{
    /// <summary>事件总线</summary>
    public IEventBus<TEvent> EventBus { get; set; } = eventBus;

    /// <summary>原始消息</summary>
    public Queues.Message Message { get; set; } = message;
}

/// <summary>Redis事件总线</summary>
/// <typeparam name="TEvent"></typeparam>
/// <remarks>实例化消息队列事件总线</remarks>
public class RedisEventBus<TEvent>(FullRedis cache, String topic, String group) : EventBus<TEvent>
{
    private RedisStream<TEvent>? _queue;
    private CancellationTokenSource? _source;

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _source?.TryDispose();
    }

    /// <summary>初始化</summary>
    [MemberNotNull(nameof(_queue))]
    protected virtual void Init()
    {
        if (_queue != null) return;

        // 创建Stream队列，指定消费组，从最后位置开始消费
        var stream = cache.GetStream<TEvent>(topic);
        stream.Group = group;
        stream.FromLastOffset = true;
        stream.Expire = TimeSpan.FromDays(3);

        _queue = stream;

        if (_source != null)
            _ = Task.Factory.StartNew(() => ConsumeMessage(_source), TaskCreationOptions.LongRunning);
    }

    /// <summary>发布消息到消息队列</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override Task<Int32> PublishAsync(TEvent @event, IEventContext<TEvent>? context = null, CancellationToken cancellationToken = default)
    {
        Init();
        var rs = _queue.Add(@event);

        return Task.FromResult(1);
    }

    /// <summary>订阅消息。启动大循环，从消息队列订阅消息，再分发到本地订阅者</summary>
    /// <param name="handler">处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public override Boolean Subscribe(IEventHandler<TEvent> handler, String clientId = "")
    {
        if (_source == null)
        {
            var source = new CancellationTokenSource();
            if (Interlocked.CompareExchange(ref _source, source, null) == null)
            {
                Init();
            }
        }

        // 本进程订阅。从队列中消费到消息时，会发布到本进程的事件总线，这里订阅可以让目标处理器直接收到消息
        return base.Subscribe(handler, clientId);
    }

    /// <summary>从队列中消费消息，经事件总线送给设备会话</summary>
    /// <param name="source"></param>
    /// <returns></returns>
    protected virtual async Task ConsumeMessage(CancellationTokenSource source)
    {
        DefaultSpan.Current = null;
        var cancellationToken = source.Token;
        var stream = _queue!;
        if (!stream.Group.IsNullOrEmpty()) stream.SetGroup(stream.Group);

        var context = new RedisEventContext<TEvent>(this, null!);
        while (!cancellationToken.IsCancellationRequested)
        {
            // try-catch 放在循环内，避免单次异常退出循环
            try
            {
                var msg = await stream.TakeMessageAsync(15, cancellationToken).ConfigureAwait(false);
                if (msg != null)
                {
                    var msg2 = msg.GetBody<TEvent>();
                    if (msg2 != null)
                    {
                        // 发布到事件总线
                        context.Message = msg;
                        await base.PublishAsync(msg2, context, cancellationToken).ConfigureAwait(false);
                        context.Message = null!;
                    }

                    // 确认消息
                    stream.Acknowledge(msg.Id!);
                }
                else
                {
                    await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (ThreadAbortException) { break; }
            catch (ThreadInterruptedException) { break; }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) break;

                XTrace.WriteException(ex);
            }
        }

        // 通知取消
        try
        {
            if (!source.IsCancellationRequested) source.Cancel();
        }
        catch (ObjectDisposedException) { }
        _queue = null;
    }
}
