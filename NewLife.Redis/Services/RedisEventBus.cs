using System.Diagnostics.CodeAnalysis;
using NewLife.Caching.Queues;
using NewLife.Log;
using NewLife.Messaging;
using Message = NewLife.Caching.Queues.Message;

namespace NewLife.Caching.Services;

/// <summary>Redis事件上下文</summary>
public class RedisEventContext(IEventBus eventBus, Message message) : IEventContext
{
    /// <summary>事件总线</summary>
    public IEventBus EventBus { get; set; } = eventBus;

    /// <summary>原始消息</summary>
    public Message Message { get; set; } = message;
}

/// <summary>Redis事件总线</summary>
/// <typeparam name="TEvent"></typeparam>
/// <remarks>实例化消息队列事件总线</remarks>
public class RedisEventBus<TEvent>(FullRedis cache, String topic, String group) : EventBus<TEvent>, ITracerFeature
{
    private RedisStream<TEvent>? _queue;
    /// <summary>队列。默认RedisStream实现，借助队列重试机制来确保业务成功</summary>
    public IProducerConsumer<TEvent> Queue => _queue!;

    /// <summary>链路追踪</summary>
    public ITracer? Tracer { get; set; }

    private RedisEventContext? _context;
    private CancellationTokenSource? _source;
    private Int32 _consuming;

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        // 取消消费循环，停止 ConsumeAsync 内部循环
        try
        {
            _source?.Cancel();
        }
        catch (ObjectDisposedException) { }

        _source?.TryDispose();
    }

    /// <summary>确保队列已初始化（仅创建队列，不启动消费）</summary>
    [MemberNotNull(nameof(_queue))]
    protected virtual void EnsureQueue()
    {
        if (_queue != null) return;

        Tracer ??= (cache as ITracerFeature)?.Tracer;

        // 创建Stream队列，指定消费组，从最后位置开始消费
        var stream = cache.GetStream<TEvent>(topic);
        stream.Group = group;
        stream.FromLastOffset = true;
        stream.Expire = TimeSpan.FromDays(3);

        _queue = stream;
    }

    /// <summary>启动消费循环</summary>
    /// <remarks>
    /// 调用 RedisStream.ConsumeAsync 启动后台消费大循环。
    /// 曾经尝试过 Task.Factory.StartNew(() => ConsumeMessage(_source), TaskCreationOptions.LongRunning) 手动循环，
    /// 但改用 RedisStream 内置的 ConsumeAsync 更稳定可靠，且支持消费组自动管理。
    /// </remarks>
    protected virtual void StartConsuming()
    {
        if (_source == null) return;
        if (Interlocked.CompareExchange(ref _consuming, 1, 0) != 0) return;

        _ = _queue!.ConsumeAsync(OnMessage, _source.Token);
    }

    /// <summary>发布消息到消息队列</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override Task<Int32> PublishAsync(TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        // 待发布消息增加追踪标识
        if (@event is ITraceMessage tm && tm.TraceId.IsNullOrEmpty()) tm.TraceId = DefaultSpan.Current?.ToString();

        EnsureQueue();

        var rs = _queue.Add(@event);

        return Task.FromResult(1);
    }

    /// <summary>订阅消息。启动大循环，从消息队列订阅消息，再分发到本地订阅者</summary>
    /// <param name="handler">处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override Task<Boolean> SubscribeAsync(IEventHandler<TEvent> handler, String clientId = "", CancellationToken cancellationToken = default)
    {
        if (_source == null)
        {
            var source = new CancellationTokenSource();
            if (Interlocked.CompareExchange(ref _source, source, null) == null)
            {
                EnsureQueue();
                StartConsuming();
            }
        }

        return base.SubscribeAsync(handler, clientId, cancellationToken);
    }

    /// <summary>消费到事件消息，分发给内部订阅者</summary>
    /// <param name="event"></param>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual async Task OnMessage(TEvent @event, Message message, CancellationToken cancellationToken)
    {
        // 发布到事件总线
        _context ??= new RedisEventContext(this, null!);
        _context.Message = message;
        await base.PublishAsync(@event, _context, cancellationToken).ConfigureAwait(false);
        _context.Message = null!;
    }

}
