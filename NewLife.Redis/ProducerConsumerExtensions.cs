using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Serialization;
#if !NET40
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Caching
{
    /// <summary>IProducerConsumer接口扩展</summary>
    public static class ProducerConsumerExtensions
    {
        #region 循环消费
        /// <summary>队列消费大循环，处理消息后自动确认</summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列</param>
        /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
        /// <param name="onException">异常处理</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="tracer">性能跟踪</param>
        /// <returns></returns>
        public static async Task RunLoopAsync<T>(this IProducerConsumer<String> queue, Func<T, Task> onMessage, Action<Exception> onException = null, CancellationToken cancellationToken = default, ITracer tracer = null)
        {
            // 主题
            var topic = (queue as RedisBase).Key;
            if (topic.IsNullOrEmpty()) topic = queue.GetType().Name;

            // 超时时间，用于阻塞等待
            var timeout = 2;
            if (queue is RedisBase rb && rb.Redis != null) timeout = rb.Redis.Timeout / 1000 - 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                ISpan span = null;
                try
                {
                    // 异步阻塞消费
                    var msg = await queue.TakeOneAsync(timeout);
                    if (msg != null)
                    {
                        // 反序列化消息
                        var message = msg.ToJsonEntity<T>();
                        span = tracer?.NewSpan($"mq:{topic}", msg);

                        // 处理消息
                        await onMessage(message);

                        // 确认消息
                        queue.Acknowledge(msg);
                    }
                    else
                    {
                        // 没有消息，歇一会
                        await TaskEx.Delay(1000);
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch (Exception ex)
                {
                    span?.SetError(ex, null);

                    onException?.Invoke(ex);
                }
                finally
                {
                    span?.Dispose();
                }
            }
        }
        #endregion
    }
}