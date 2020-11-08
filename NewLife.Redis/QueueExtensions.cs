#if !NET40
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching.Models;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Caching
{
    /// <summary>IProducerConsumer接口扩展</summary>
    public static class QueueExtensions
    {
        #region 循环消费
        /// <summary>队列消费大循环，处理消息后自动确认</summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列</param>
        /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="log">日志对象</param>
        /// <param name="idField">消息标识字段名，用于处理错误重试</param>
        /// <returns></returns>
        public static async Task ConsumeAsync<T>(this IProducerConsumer<String> queue, Func<T, String, CancellationToken, Task> onMessage, CancellationToken cancellationToken = default, ILog log = null, String idField = null)
        {
            // 大循环之前，打断性能追踪调用链
            DefaultSpan.Current = null;

            // 主题
            var topic = (queue as RedisBase).Key;
            if (topic.IsNullOrEmpty()) topic = queue.GetType().Name;

            var rds = (queue as RedisBase).Redis;
            var tracer = rds.Tracer;
            var errLog = log ?? XTrace.Log;

            var ids = new List<String> { "Id", "guid", "OrderId", "Code" };
            if (!idField.IsNullOrEmpty() && !ids.Contains(idField)) ids.Insert(0, idField);

            // 超时时间，用于阻塞等待
            var timeout = rds.Timeout / 1000 - 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                var msgId = "";
                var mqMsg = "";
                ISpan span = null;
                try
                {
                    // 异步阻塞消费
                    mqMsg = await queue.TakeOneAsync(timeout);
                    if (mqMsg != null)
                    {
                        // 埋点
                        span = tracer?.NewSpan($"redismq:{topic}", mqMsg);
                        log?.Info($"[{topic}]消息内容为：{mqMsg}");

                        // 解码
                        var dic = JsonParser.Decode(mqMsg);
                        var msg = JsonHelper.Convert<T>(dic);

                        if (dic.TryGetValue("traceParent", out var tp)) span.Detach(tp + "");

                        // 消息标识
                        foreach (var item in ids)
                        {
                            if (dic.TryGetValue(item, out var id))
                            {
                                msgId = id + "";
                                if (!msgId.IsNullOrEmpty()) break;
                            }
                        }

                        // 处理消息
                        await onMessage(msg, mqMsg, cancellationToken);

                        // 确认消息
                        queue.Acknowledge(mqMsg);
                    }
                    else
                    {
                        // 没有消息，歇一会
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch (Exception ex)
                {
                    span?.SetError(ex, null);

                    // 消息处理错误超过10次则抛弃
                    if (!mqMsg.IsNullOrEmpty())
                    {
                        if (msgId.IsNullOrEmpty()) msgId = mqMsg.MD5();
                        errLog?.Error("[{0}/{1}]消息处理异常：{2} {3}", topic, msgId, mqMsg, ex);
                        var key = $"{topic}:Error:{msgId}";

                        var rs = rds.Increment(key, 1);
                        if (rs < 10)
                            rds.SetExpire(key, TimeSpan.FromHours(24));
                        else
                        {
                            queue.Acknowledge(mqMsg);

                            errLog?.Error("[{0}/{1}]错误过多，删除消息", topic, msgId);
                        }
                    }
                }
                finally
                {
                    span?.Dispose();
                }
            }
        }

        /// <summary>队列消费大循环，处理消息后自动确认</summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列</param>
        /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="log">日志对象</param>
        /// <param name="idField">消息标识字段名，用于处理错误重试</param>
        /// <returns></returns>
        public static async Task ConsumeAsync<T>(this IProducerConsumer<String> queue, Action<T> onMessage, CancellationToken cancellationToken = default, ILog log = null, String idField = null)
        {
            await ConsumeAsync<T>(queue, (m, k, t) => { onMessage(m); return Task.FromResult(0); }, cancellationToken, log, idField);
        }

        /// <summary>队列消费大循环，处理消息后自动确认</summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列</param>
        /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="log">日志对象</param>
        /// <param name="idField">消息标识字段名，用于处理错误重试</param>
        /// <returns></returns>
        public static async Task ConsumeAsync<T>(this RedisReliableQueue<String> queue, Func<T, String, CancellationToken, Task> onMessage, CancellationToken cancellationToken = default, ILog log = null, String idField = null)
        {
            // 大循环之前，打断性能追踪调用链
            DefaultSpan.Current = null;

            // 主题
            var topic = queue.Key;
            if (topic.IsNullOrEmpty()) topic = queue.GetType().Name;

            var rds = queue.Redis;
            var tracer = rds.Tracer;
            var errLog = log ?? XTrace.Log;

            // 备用redis，容错、去重
            var rds2 = new FullRedis
            {
                Name = rds.Name + "Bak",
                Server = rds.Server,
                UserName = rds.UserName,
                Password = rds.Password,
                Db = rds.Db == 15 ? 0 : (rds.Db + 1),
                Tracer = rds.Tracer,
            };

            // 消息去重
            if (queue.DuplicateExpire > 0 && idField.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(idField), $"队列[{topic}]消息[{queue.DuplicateExpire}]秒去重，需要指定消息唯一标识idField");

            var ids = new List<String> { "Id", "guid", "OrderId", "Code" };
            if (!idField.IsNullOrEmpty() && !ids.Contains(idField)) ids.Insert(0, idField);

            // 超时时间，用于阻塞等待
            var timeout = rds.Timeout / 1000 - 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                var msgId = "";
                var mqMsg = "";
                ISpan span = null;
                try
                {
                    // 异步阻塞消费
                    mqMsg = await queue.TakeOneAsync(timeout, cancellationToken);
                    if (mqMsg != null)
                    {
                        // 埋点
                        span = tracer?.NewSpan($"redismq:{topic}", mqMsg);
                        log?.Info($"[{topic}]消息内容为：{mqMsg}");

                        // 解码
                        var dic = JsonParser.Decode(mqMsg);
                        var msg = JsonHelper.Convert<T>(dic);

                        if (dic.TryGetValue("traceParent", out var tp)) span.Detach(tp + "");

                        // 消息标识
                        foreach (var item in ids)
                        {
                            if (dic.TryGetValue(item, out var id))
                            {
                                msgId = id + "";
                                if (!msgId.IsNullOrEmpty()) break;
                            }
                        }

                        // 消息去重
                        if (queue.DuplicateExpire > 0)
                        {
                            // 抢占msgId，处理异常时退出抢占
                            var dkey = $"{topic}:Duplicate:{msgId}";
                            if (!rds2.Add(dkey, queue.Status.Key, queue.DuplicateExpire))
                            {
                                log?.Info("队列[{0}]遇到重复消息[{1}]，自动跳过", topic, msgId);
                            }
                            else
                            {
                                try
                                {
                                    await onMessage(msg, mqMsg, cancellationToken);
                                }
                                catch
                                {
                                    rds2.Remove(dkey);
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            // 处理消息
                            await onMessage(msg, mqMsg, cancellationToken);
                        }

                        // 确认消息
                        queue.Acknowledge(mqMsg);
                    }
                    else
                    {
                        // 没有消息，歇一会
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch (Exception ex)
                {
                    span?.SetError(ex, null);

                    // 消息处理错误超过10次则抛弃
                    if (!mqMsg.IsNullOrEmpty())
                    {
                        if (msgId.IsNullOrEmpty()) msgId = mqMsg.MD5();
                        errLog?.Error("[{0}/{1}]消息处理异常：{2} {3}", topic, msgId, mqMsg, ex);
                        var key = $"{topic}:Error:{msgId}";

                        var rs = rds2.Increment(key, 1);
                        if (rs < 10)
                            rds2.SetExpire(key, TimeSpan.FromHours(24));
                        else
                        {
                            queue.Acknowledge(mqMsg);

                            errLog?.Error("[{0}/{1}]错误过多，删除消息", topic, msgId);
                        }
                    }
                }
                finally
                {
                    span?.Dispose();
                }
            }
        }

        /// <summary>队列消费大循环，处理消息后自动确认</summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列</param>
        /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="log">日志对象</param>
        /// <param name="idField">消息标识字段名，用于处理错误重试</param>
        /// <returns></returns>
        public static async Task ConsumeAsync<T>(this RedisReliableQueue<String> queue, Action<T> onMessage, CancellationToken cancellationToken = default, ILog log = null, String idField = null)
        {
            await ConsumeAsync<T>(queue, (m, k, t) => { onMessage(m); return Task.FromResult(0); }, cancellationToken, log, idField);
        }

        /// <summary>队列消费大循环，处理消息后自动确认</summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列</param>
        /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="log">日志对象</param>
        /// <returns></returns>
        public static async Task ConsumeAsync<T>(this RedisStream<String> queue, Func<T, Message, CancellationToken, Task> onMessage, CancellationToken cancellationToken = default, ILog log = null)
        {
            // 大循环之前，打断性能追踪调用链
            DefaultSpan.Current = null;

            // 主题
            var topic = queue.Key;
            if (topic.IsNullOrEmpty()) topic = queue.GetType().Name;

            var rds = queue.Redis;
            var tracer = rds.Tracer;
            var errLog = log ?? XTrace.Log;

            // 超时时间，用于阻塞等待
            var timeout = rds.Timeout / 1000 - 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                Message mqMsg = null;
                ISpan span = null;
                try
                {
                    // 异步阻塞消费
                    mqMsg = await queue.TakeMessageAsync(timeout, cancellationToken);
                    if (mqMsg != null)
                    {
                        // 埋点
                        span = tracer?.NewSpan($"redismq:{topic}", mqMsg);
                        log?.Info($"[{topic}]消息内容为：{mqMsg}");

                        var bodys = mqMsg.Body;
                        for (var i = 0; i < bodys.Length; i++)
                        {
                            if (bodys[i].EqualIgnoreCase("traceParent") && i + 1 < bodys.Length) span.Detach(bodys[i + 1]);
                        }

                        // 解码
                        var msg = mqMsg.GetBody<T>();

                        // 处理消息
                        await onMessage(msg, mqMsg, cancellationToken);

                        // 确认消息
                        queue.Acknowledge(mqMsg.Id);
                    }
                    else
                    {
                        // 没有消息，歇一会
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch (Exception ex)
                {
                    span?.SetError(ex, null);
                    errLog?.Error("[{0}/{1}]消息处理异常：{2} {3}", topic, mqMsg?.Id, mqMsg?.ToJson(), ex);
                }
                finally
                {
                    span?.Dispose();
                }
            }
        }

        /// <summary>队列消费大循环，处理消息后自动确认</summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列</param>
        /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="log">日志对象</param>
        /// <returns></returns>
        public static async Task ConsumeAsync<T>(this RedisStream<String> queue, Action<T> onMessage, CancellationToken cancellationToken = default, ILog log = null)
        {
            await ConsumeAsync<T>(queue, (m, k, t) => { onMessage(m); return Task.FromResult(0); }, cancellationToken, log);
        }
        #endregion
    }
}
#endif