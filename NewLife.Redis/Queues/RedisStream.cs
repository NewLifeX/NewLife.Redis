using System.Diagnostics;
using NewLife.Caching.Common;
using NewLife.Data;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Caching.Queues;

/// <summary>Redis5.0的Stream数据结构，完整态消息队列，支持多消费组</summary>
/// <remarks>
/// 特殊的$，表示接收从阻塞那一刻开始添加到流的消息
/// </remarks>
/// <typeparam name="T"></typeparam>
public class RedisStream<T> : QueueBase, IProducerConsumer<T>
{
    #region 属性
    /// <summary>个数</summary>
    public Int32 Count => Execute(r => r.Execute<Int32>("XLEN", Key));

    /// <summary>是否为空</summary>
    public Boolean IsEmpty => Count == 0;

    /// <summary>重新处理确认队列中死信的间隔。默认60s</summary>
    public Int32 RetryInterval { get; set; } = 60;

    /// <summary>基元类型数据添加该key构成集合。默认__data</summary>
    public String PrimitiveKey { get; set; } = "__data";

    /// <summary>最大队列长度。要保留的消息个数，超过则移除较老消息，非精确，实际上略大于该值，默认100万</summary>
    public Int32 MaxLength { get; set; } = 1_000_000;

    /// <summary>最大重试次数。超过该次数后，消息将被抛弃，默认10次</summary>
    public Int32 MaxRetry { get; set; } = 10;

    /// <summary>异步消费时的阻塞时间。默认15秒</summary>
    public Int32 BlockTime { get; set; } = 15;

    /// <summary>开始编号。独立消费时使用，消费组消费时不使用，默认0-0</summary>
    public String StartId { get; set; } = "0-0";

    /// <summary>消费者组。指定消费组后，不再使用独立消费。通过SetGroup可自动创建消费组</summary>
    public String Group { get; set; }

    /// <summary>消费者。同一个消费组内的消费者标识必须唯一</summary>
    public String Consumer { get; set; }

    /// <summary>首次消费时的消费策略</summary>
    /// <remarks>
    /// 默认值false，表示从头部开始消费，等同于RocketMQ/Java版的CONSUME_FROM_FIRST_OFFSET
    /// 一个新的订阅组第一次启动从队列的最前位置开始消费，后续再启动接着上次消费的进度开始消费。
    /// </remarks>
    public Boolean FromLastOffset { get; set; }

    private Int32 _count;
    private Int32 _setGroupId;
    private Int32 _claims;
    #endregion

    #region 构造
    /// <summary>实例化队列</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public RedisStream(Redis redis, String key) : base(redis, key) => Consumer = $"{Environment.MachineName}@{Process.GetCurrentProcess().Id}";
    #endregion

    #region 核心生产消费
    /// <summary>设置消费组。如果消费组不存在则创建</summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public Boolean SetGroup(String group)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

        Group = group;

        // 如果Stream不存在，则直接创建消费组，此时会创建Stream
        if (!Redis.ContainsKey(Key)) return GroupCreate(group);

        var gs = GetGroups();
        if (gs == null || !gs.Any(e => e.Name == group))
            return GroupCreate(group);

        return false;
    }

    /// <summary>生产添加</summary>
    /// <param name="value">消息体</param>
    /// <param name="msgId">消息ID</param>
    /// <returns>返回消息ID</returns>
    public String Add(T value, String msgId = null)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        // 自动修剪超长部分，每1000次生产，修剪一次
        var n = Interlocked.Increment(ref _count);
        var trim = MaxLength > 0 && n % 1000 == 0;
        if (n == 1) Trim(MaxLength, true);

        return AddInternal(value, msgId, trim, true);
    }

    private String AddInternal(T value, String msgId, Boolean trim, Boolean retryOnFailed)
    {
        using var span = Redis.Tracer?.NewSpan($"redismq:{TraceName}:Add", value);

        var args = new List<Object> { Key };
        if (trim)
        {
            args.Add("maxlen");
            args.Add("~");
            args.Add(MaxLength);
        }

        // *号表示服务器自动生成ID
        args.Add(msgId.IsNullOrEmpty() ? "*" : msgId);

        // 数组和复杂对象字典，分开处理
        if (Type.GetTypeCode(value.GetType()) != TypeCode.Object)
        {
            //throw new ArgumentOutOfRangeException(nameof(value), "消息体必须是复杂对象！");
            args.Add(PrimitiveKey);
            args.Add(value);
        }
        else if (value.GetType().IsArray)
            foreach (var item in value as Array)
                args.Add(item);
        else
        {
            // 在消息体内注入TraceId，用于构建调用链
            var val = AttachTraceId ? Redis.AttachTraceId(value) : value;
            foreach (var item in val.ToDictionary())
            {
                args.Add(item.Key);
                args.Add(item.Value);
            }
        }

        var rs = "";
        for (var i = 0; i <= RetryTimesWhenSendFailed; i++)
        {
            rs = Execute(rc => rc.Execute<String>("XADD", args.ToArray()), true);
            if (!retryOnFailed || !rs.IsNullOrEmpty()) return rs;

            span?.SetError(new RedisException($"发布到队列[{Topic}]失败！"), null);

            if (i < RetryTimesWhenSendFailed) Thread.Sleep(RetryIntervalWhenSendFailed);
        }

        ValidWhenSendFailed(span);

        return rs;
    }

    /// <summary>批量生产添加</summary>
    /// <param name="values"></param>
    /// <returns></returns>
    Int32 IProducerConsumer<T>.Add(params T[] values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));

        // 量少时直接插入，而不用管道
        if (values.Length <= 2)
        {
            for (var i = 0; i < values.Length; i++)
                Add(values[i]);
            return values.Length;
        }
        if (_count == 0) Trim(MaxLength, true);

        // 开启管道
        var rds = Redis;
        rds.StartPipeline();

        foreach (var item in values)
        {
            // 自动修剪超长部分，每1000次生产，修剪一次
            var n = Interlocked.Increment(ref _count);
            var trim = MaxLength > 0 && n % 1000 == 0;

            AddInternal(item, null, trim, false);
        }

        rds.StopPipeline(true);

        return values.Length;
    }

    /// <summary>批量消费获取，前移指针StartId</summary>
    /// <param name="count">批量消费消息数</param>
    /// <returns></returns>
    public IEnumerable<T> Take(Int32 count = 1)
    {
        var group = Group;
        if (!group.IsNullOrEmpty())
            // 优先处理未确认死信，避免在处理海量消息的过程中，未确认死信一直得不到处理
            _claims += RetryAck();

        // 抢过来的消息，优先处理，可能需要多次消费才能消耗完
        if (_claims > 0)
        {
            var rs2 = ReadGroup(group, Consumer, count, "0");
            if (rs2 != null && rs2.Count > 0)
            {
                _claims -= rs2.Count;
                XTrace.WriteLine("[{0}]优先处理历史：{1}", Group, rs2.Join(",", e => e.Id));

                foreach (var item in rs2)
                    yield return item.GetBody<T>();
            }
            _claims = 0;
        }

        var rs = !group.IsNullOrEmpty() ?
            ReadGroup(group, Consumer, count, ">") :
            Read(StartId, count);
        if (rs == null || rs.Count == 0) yield break;

        foreach (var item in rs)
        {
            if (group.IsNullOrEmpty()) SetNextId(item.Id);

            yield return item.GetBody<T>();
        }
    }

    /// <summary>后面的ID应该使用前一次报告的项目中最后一项的ID，否则将会丢失所有添加到这中间的条目。</summary>
    /// <param name="key"></param>
    private void SetNextId(String key) => StartId = key;

    /// <summary>消费获取一个</summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public T TakeOne(Int32 timeout = 0) => Take(1).FirstOrDefault();

    /// <summary>异步消费获取一个</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<T> TakeOneAsync(Int32 timeout = 0, CancellationToken cancellationToken = default)
    {
        var msg = await TakeMessageAsync(timeout, cancellationToken);
        if (msg == null) return default;

        return msg.GetBody<T>();
    }

    /// <summary>异步消费获取</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞；负数表示直接返回，不阻塞。</param>
    /// <returns></returns>
    Task<T> IProducerConsumer<T>.TakeOneAsync(Int32 timeout) => TakeOneAsync(timeout, default);

    /// <summary>异步消费获取一个</summary>
    /// <param name="timeout">超时时间，默认0秒永远阻塞</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<Message> TakeMessageAsync(Int32 timeout = 0, CancellationToken cancellationToken = default)
    {
        var rs = await TakeMessagesAsync(1, timeout, cancellationToken);
        return rs?.FirstOrDefault();
    }

    /// <summary>批量消费</summary>
    /// <param name="count">批量消费消息数</param>
    /// <param name="timeout">超时时间，默认10秒阻塞</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<IList<Message>> TakeMessagesAsync(Int32 count, Int32 timeout = 10, CancellationToken cancellationToken = default)
    {
        if (FromLastOffset && _setGroupId == 0 && Interlocked.CompareExchange(ref _setGroupId, 1, 0) == 0)
            GroupSetId(Group, "$");

        var group = Group;
        if (!group.IsNullOrEmpty())
            // 优先处理未确认死信，避免在处理海量消息的过程中，未确认死信一直得不到处理
            _claims += RetryAck();

        // 抢过来的消息，优先处理，可能需要多次消费才能消耗完
        if (_claims > 0)
        {
            var rs2 = await ReadGroupAsync(group, Consumer, count, 3_000, "0", cancellationToken);
            if (rs2 != null && rs2.Count > 0)
            {
                _claims -= rs2.Count;
                XTrace.WriteLine("[{0}]优先处理历史：{1}", Group, rs2.Join(",", e => e.Id));

                return rs2;
            }
            _claims = 0;
        }

        var t = timeout * 1000;
        if (timeout > 0 && Redis.Timeout < t) Redis.Timeout = t + 1000;

        //var id = FromLastOffset ? "$" : ">";

        var rs = !group.IsNullOrEmpty() ?
            await ReadGroupAsync(group, Consumer, count, t, ">", cancellationToken) :
            await ReadAsync(StartId, count, t, cancellationToken);
        if (rs == null || rs.Count == 0)
        {
            // id为>时，消费从未传递给消费者的消息
            // id为$时，消费从从阻塞开始新收到的消息
            // id为0时，消费当前消费者的历史待处理消息，包括自己未ACK和从其它消费者抢来的消息
            // 使用消费组时，如果拿不到消息，则尝试消费抢过来的历史消息
            if (!group.IsNullOrEmpty())
            {
                rs = await ReadGroupAsync(group, Consumer, count, 3_000, "0", cancellationToken);
                if (rs == null || rs.Count == 0) return null;

                XTrace.WriteLine("[{0}]处理历史：{1}", Group, rs.Join(",", e => e.Id));

                return rs;
            }

            return null;
        }

        // 全局消费（非消费组）时，更新编号
        if (group.IsNullOrEmpty()) SetNextId(rs[rs.Count - 1].Id);

        return rs;
    }

    /// <summary>消费确认</summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public Int32 Acknowledge(params String[] keys)
    {
        var rs = 0;
        foreach (var item in keys)
            rs += Ack(Group, item);
        return rs;
    }
    #endregion

    #region 死信处理
    private DateTime _nextRetry;
    /// <summary>处理未确认的死信，重新放入队列</summary>
    private Int32 RetryAck()
    {
        var count = 0;
        var now = DateTime.Now;
        // 一定间隔处理当前ukey死信
        if (_nextRetry < now)
        {
            _nextRetry = now.AddSeconds(RetryInterval);
            var retry = RetryInterval * 1000;

            // 拿到死信，重新放入队列
            String id = null;
            while (true)
            {
                var list = Pending(Group, id, null, 100);
                if (list.Length == 0) break;

                foreach (var item in list)
                    if (item.Idle > retry)
                        if (item.Delivery >= MaxRetry)
                        {
                            XTrace.WriteLine("[{0}]删除多次失败死信：{1}", Group, item.ToJson());
                            //Delete(item.Id);
                            Claim(Group, Consumer, item.Id, retry);
                            Ack(Group, item.Id);
                        }
                        else
                        {
                            XTrace.WriteLine("[{0}]定时回滚：{1}", Group, item.ToJson());
                            // 抢夺消息，所有者更改为当前消费者，Idle从零开始计算。这些消息需要尽快得到处理，否则会再次过期
                            Claim(Group, Consumer, item.Id, retry);

                            count++;
                        }

                // 下一个开始id
                id = list[^1].Id;
                var p = id.IndexOf('-');
                if (p > 0) id = $"{id[..p].ToLong() + 1}-0";
            }

            // 清理历史消费者
            var consumers = GetConsumers(Group);
            if (consumers != null)
                foreach (var item in consumers)
                    if (item.Pending == 0 && item.Idle > 3600_000)
                    {
                        XTrace.WriteLine("[{0}]删除空闲消费者：{1}", Group, item.ToJson());
                        GroupDeleteConsumer(Group, item.Name);
                    }
        }

        return count;
    }
    #endregion

    #region 内部命令
    /// <summary>删除指定消息</summary>
    /// <param name="id">消息Id</param>
    /// <returns></returns>
    public Int32 Delete(String id) => Execute(rc => rc.Execute<Int32>("XDEL", Key, id), true);

    /// <summary>裁剪队列到指定大小</summary>
    /// <param name="maxLen">最大长度。为了提高效率，最大长度并没有那么精准</param>
    /// <param name="accurate"></param>
    /// <returns></returns>
    public Int32 Trim(Int32 maxLen, Boolean accurate = false)
    {
        return accurate
            ? Execute(rc => rc.Execute<Int32>("XTRIM", Key, "MAXLEN", maxLen), true)
            : Execute(rc => rc.Execute<Int32>("XTRIM", Key, "MAXLEN", "~", maxLen), true);
    }

    /// <summary>确认消息</summary>
    /// <param name="group">消费组名称</param>
    /// <param name="id">消息Id</param>
    /// <returns></returns>
    public Int32 Ack(String group, String id) => Execute(rc => rc.Execute<Int32>("XACK", Key, group, id), true);

    /// <summary>改变待处理消息的所有权，抢夺他人未确认消息</summary>
    /// <param name="group">消费组名称</param>
    /// <param name="consumer">目标消费者</param>
    /// <param name="id">消息Id</param>
    /// <param name="msIdle">空闲时间。默认3600_000</param>
    /// <returns></returns>
    public Object[] Claim(String group, String consumer, String id, Int32 msIdle = 3_600_000) => Execute(rc => rc.Execute<Object[]>("XCLAIM", Key, group, consumer, msIdle, id), true);

    /// <summary>获取区间消息</summary>
    /// <param name="startId"></param>
    /// <param name="endId"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public IList<Message> Range(String startId, String endId, Int32 count = -1)
    {
        if (startId.IsNullOrEmpty()) startId = "-";
        if (endId.IsNullOrEmpty()) endId = "+";

        var rs = count > 0 ?
            Execute(rc => rc.Execute<Object[]>("XRANGE", Key, startId, endId, "COUNT", count), false) :
            Execute(rc => rc.Execute<Object[]>("XRANGE", Key, startId, endId), false);
        if (rs == null) return null;

        return Parse(rs);
    }

    /// <summary>获取区间消息</summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public IList<Message> Range(DateTime start, DateTime end, Int32 count = -1) => Range(start.ToLong() + "-0", end.ToLong() + "-0", count);

    /// <summary>原始独立消费</summary>
    /// <remarks>
    /// 特殊的$，表示接收从阻塞那一刻开始添加到流的消息
    /// </remarks>
    /// <param name="startId">开始编号</param>
    /// <param name="count">消息个数</param>
    /// <returns></returns>
    public IList<Message> Read(String startId, Int32 count)
    {
        if (startId.IsNullOrEmpty()) startId = "$";

        var args = new List<Object>();
        if (count > 0)
        {
            args.Add("count");
            args.Add(count);
        }
        args.Add("streams");
        args.Add(Key);
        args.Add(startId);

        var rs = Execute(rc => rc.Execute<Object[]>("XREAD", args.ToArray()), true);
        if (rs != null && rs.Length == 1 && rs[0] is Object[] vs && vs.Length == 2)
            /*
XREAD count 3 streams stream_key 0-0

1)  1)   "stream_key"
2)  1)  1)     "1593751768717-0"
        2)  1)      "__data"
            2)      "1234"


    2)  1)     "1593751768721-0"
        2)  1)      "name"
            2)      "bigStone"
            3)      "age"
            4)      "24"


    3)  1)     "1593751768733-0"
        2)  1)      "name"
            2)      "smartStone"
            3)      "age"
            4)      "36"
             */
            if (vs[1] is Object[] vs2) return Parse(vs2);

        return null;
    }

    /// <summary>异步原始独立消费</summary>
    /// <remarks>
    /// 特殊的$，表示接收从阻塞那一刻开始添加到流的消息
    /// </remarks>
    /// <param name="startId">开始编号</param>
    /// <param name="count">消息个数</param>
    /// <param name="block">阻塞毫秒数，0表示永远</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<IList<Message>> ReadAsync(String startId, Int32 count, Int32 block = -1, CancellationToken cancellationToken = default)
    {
        if (startId.IsNullOrEmpty()) startId = "$";

        var args = new List<Object>();
        if (block > 0)
        {
            args.Add("block");
            args.Add(block);
        }
        if (count > 0)
        {
            args.Add("count");
            args.Add(count);
        }
        args.Add("streams");
        args.Add(Key);
        args.Add(startId);

        var rs = await ExecuteAsync(rc => rc.ExecuteAsync<Object[]>("XREAD", args.ToArray(), cancellationToken), true);
        if (rs != null && rs.Length == 1 && rs[0] is Object[] vs && vs.Length == 2)
            if (vs[1] is Object[] vs2) return Parse(vs2);

        return null;
    }

    private IList<Message> Parse(Object[] vs)
    {
        var list = new List<Message>();
        foreach (var item in vs)
            if (item is Object[] vs3 && vs3.Length == 2 && vs3[0] is Packet pkId && vs3[1] is Object[] vs4)
                list.Add(new Message
                {
                    Id = pkId.ToStr(),
                    Body = vs4.Select(e => (e as Packet)?.ToStr()).ToArray(),
                });
        return list;
    }
    #endregion

    #region 等待列表
    /// <summary>获取等待列表信息</summary>
    /// <param name="group">消费组名称</param>
    /// <returns></returns>
    public PendingInfo GetPending(String group)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

        var rs = Execute(rc => rc.Execute<Object[]>("XPENDING", Key, group), false);
        if (rs == null) return null;

        var pi = new PendingInfo();
        pi.Parse(rs);

        return pi;
    }

    /// <summary>获取等待列表消息</summary>
    /// <param name="group">消费组名称</param>
    /// <param name="startId"></param>
    /// <param name="endId"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public PendingItem[] Pending(String group, String startId, String endId, Int32 count = -1)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));
        if (startId.IsNullOrEmpty()) startId = "-";
        if (endId.IsNullOrEmpty()) endId = "+";

        var rs = count > 0 ?
            Execute(rc => rc.Execute<Object[]>("XPENDING", Key, group, startId, endId, count), false) :
            Execute(rc => rc.Execute<Object[]>("XPENDING", Key, group, startId, endId), false);

        var list = new List<PendingItem>();
        foreach (Object[] item in rs)
        {
            var pi = new PendingItem();
            pi.Parse(item);
            list.Add(pi);
        }

        return list.ToArray();
    }
    #endregion

    #region 消费组
    /// <summary>创建消费组</summary>
    /// <param name="group">消费组名称</param>
    /// <param name="startId">开始编号。0表示从开头，$表示从末尾，收到下一条生产消息才开始消费 stream不存在，则会报错，所以在后面 加上 mkstream</param>
    /// <returns></returns>
    public Boolean GroupCreate(String group, String startId = null)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));
        if (startId.IsNullOrEmpty()) startId = "0";

        return Execute(rc => rc.Execute<String>("XGROUP", "CREATE", Key, group, startId, "MKSTREAM"), true) == "OK";
    }

    /// <summary>销毁消费组</summary>
    /// <param name="group">消费组名称</param>
    /// <returns></returns>
    public Int32 GroupDestroy(String group)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

        return Execute(rc => rc.Execute<Int32>("XGROUP", "DESTROY", Key, group), true);
    }

    /// <summary>销毁消费者</summary>
    /// <param name="group">消费组名称</param>
    /// <param name="consumer">消费者</param>
    /// <returns>返回消费者在被删除之前所拥有的待处理消息数量</returns>
    public Int32 GroupDeleteConsumer(String group, String consumer)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

        return Execute(rc => rc.Execute<Int32>("XGROUP", "DELCONSUMER", Key, group, consumer), true);
    }

    /// <summary>设置消费组Id</summary>
    /// <param name="group">消费组名称</param>
    /// <param name="startId">开始编号</param>
    /// <returns></returns>
    public Boolean GroupSetId(String group, String startId)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));
        if (startId.IsNullOrEmpty()) startId = "$";

        return Execute(rc => rc.Execute<String>("XGROUP", "SETID", Key, group, startId), true) == "OK";
    }

    /// <summary>消费组消费</summary>
    /// <param name="group">消费组</param>
    /// <param name="consumer">消费组</param>
    /// <param name="count">消息个数</param>
    /// <param name="id">消息id。为大于号时，消费从未传递给消费者的消息；为0是，消费理事会待处理消息</param>
    /// <returns></returns>
    public IList<Message> ReadGroup(String group, String consumer, Int32 count, String id = null)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

        if (FromLastOffset && _setGroupId == 0 && Interlocked.CompareExchange(ref _setGroupId, 1, 0) == 0)
            GroupSetId(Group, "$");

        // id为>时，消费从未传递给消费者的消息
        // id为$时，消费从从阻塞开始新收到的消息
        // id为0时，消费当前消费者的历史待处理消息，包括自己未ACK和从其它消费者抢来的消息
        if (id.IsNullOrEmpty()) id = ">";

        //var id = FromLastOffset ? "$" : ">";
        var rs = count > 0 ?
            Execute(rc => rc.Execute<Object[]>("XREADGROUP", "GROUP", group, consumer, "COUNT", count, "STREAMS", Key, id), true) :
            Execute(rc => rc.Execute<Object[]>("XREADGROUP", "GROUP", group, consumer, "STREAMS", Key, id), true);
        if (rs != null && rs.Length == 1 && rs[0] is Object[] vs && vs.Length == 2)
            if (vs[1] is Object[] vs2) return Parse(vs2);

        return null;
    }

    /// <summary>异步消费组消费</summary>
    /// <param name="group">消费组</param>
    /// <param name="consumer">消费组</param>
    /// <param name="count">消息个数</param>
    /// <param name="block">阻塞毫秒数，0表示永远</param>
    /// <param name="id">消息id。为大于号时，消费从未传递给消费者的消息；为0是，消费理事会待处理消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<IList<Message>> ReadGroupAsync(String group, String consumer, Int32 count, Int32 block = -1, String id = null, CancellationToken cancellationToken = default)
    {
        if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

        // id为>时，消费从未传递给消费者的消息
        // id为$时，消费从从阻塞开始新收到的消息
        // id为0时，消费当前消费者的历史待处理消息，包括自己未ACK和从其它消费者抢来的消息
        if (id.IsNullOrEmpty()) id = ">";

        var rs = count > 0 ?
            await ExecuteAsync(rc => rc.ExecuteAsync<Object[]>("XREADGROUP", new Object[] { "GROUP", group, consumer, "BLOCK", block, "COUNT", count, "STREAMS", Key, id }, cancellationToken), true) :
            await ExecuteAsync(rc => rc.ExecuteAsync<Object[]>("XREADGROUP", new Object[] { "GROUP", group, consumer, "BLOCK", block, "STREAMS", Key, id }, cancellationToken), true);
        if (rs != null && rs.Length == 1 && rs[0] is Object[] vs && vs.Length == 2)
            if (vs[1] is Object[] vs2) return Parse(vs2);

        return null;
    }
    #endregion

    #region 队列信息
    /// <summary>获取信息</summary>
    /// <returns></returns>
    public StreamInfo GetInfo()
    {
        var rs = Execute(rc => rc.Execute<Object[]>("XINFO", "STREAM", Key), false);
        if (rs == null) return null;

        var info = new StreamInfo();
        info.Parse(rs);

        return info;
    }

    /// <summary>获取消费组</summary>
    /// <returns></returns>
    public GroupInfo[] GetGroups()
    {
        var rs = Execute(rc => rc.Execute<Object[]>("XINFO", "GROUPS", Key), false);
        if (rs == null) return null;

        var gs = new GroupInfo[rs.Length];
        for (var i = 0; i < rs.Length; i++)
        {
            gs[i] = new GroupInfo();
            gs[i].Parse(rs[i] as Object[]);
        }

        return gs;
    }

    /// <summary>获取消费者</summary>
    /// <param name="group">消费组名称</param>
    /// <returns></returns>
    public ConsumerInfo[] GetConsumers(String group)
    {
        var rs = Execute(rc => rc.Execute<Object[]>("XINFO", "CONSUMERS", Key, group), false);
        if (rs == null) return null;

        var cs = new ConsumerInfo[rs.Length];
        for (var i = 0; i < rs.Length; i++)
        {
            cs[i] = new ConsumerInfo();
            cs[i].Parse(rs[i] as Object[]);
        }

        return cs;
    }
    #endregion

    #region 大循环
    /// <summary>队列消费大循环，处理消息后自动确认</summary>
    /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task ConsumeAsync(Func<T, Message, CancellationToken, Task> onMessage, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        // 自动创建消费组
        SetGroup(Group);

        // 主题
        var topic = Key;
        if (topic.IsNullOrEmpty()) topic = GetType().Name;

        // 超时时间，用于阻塞等待
        var timeout = BlockTime;

        while (!cancellationToken.IsCancellationRequested)
        {
            // 大循环之前，打断性能追踪调用链
            DefaultSpan.Current = null;

            //Message mqMsg = null;
            ISpan span = null;
            try
            {
                // 异步阻塞消费
                var mqMsg = await TakeMessageAsync(timeout, cancellationToken);
                if (mqMsg != null)
                {
                    // 埋点
                    span = Redis.Tracer?.NewSpan($"redismq:{topic}:Consume", mqMsg);

                    var bodys = mqMsg.Body;
                    for (var i = 0; i < bodys.Length; i++)
                        if (bodys[i].EqualIgnoreCase("traceParent") && i + 1 < bodys.Length) span.Detach(bodys[i + 1]);

                    // 解码
                    var msg = mqMsg.GetBody<T>();

                    // 处理消息
                    await onMessage(msg, mqMsg, cancellationToken);

                    // 确认消息
                    Acknowledge(mqMsg.Id);
                }
                else
                    // 没有消息，歇一会
                    await Task.Delay(1000, cancellationToken);
            }
            catch (ThreadAbortException) { break; }
            catch (ThreadInterruptedException) { break; }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) break;

                span?.SetError(ex, null);
                //XTrace.Log?.Error("[{0}/{1}]消息处理异常：{2} {3}", topic, mqMsg?.Id, mqMsg?.ToJson(), ex);
            }
            finally
            {
                span?.Dispose();
            }
        }
    }

    /// <summary>队列消费大循环，处理消息后自动确认</summary>
    /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task ConsumeAsync(Action<T> onMessage, CancellationToken cancellationToken = default) => await ConsumeAsync((m, k, t) => { onMessage(m); return Task.FromResult(0); }, cancellationToken);

    /// <summary>队列消费大循环，处理消息后自动确认</summary>
    /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
    /// <param name="batchSize">批大小。默认100</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task ConsumeAsync(Func<T[], Message[], CancellationToken, Task> onMessage, Int32 batchSize = 100, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        // 自动创建消费组
        SetGroup(Group);

        // 主题
        var topic = Key;
        if (topic.IsNullOrEmpty()) topic = GetType().Name;

        // 超时时间，用于阻塞等待
        var timeout = BlockTime;
        if (batchSize <= 0) batchSize = 100;

        while (!cancellationToken.IsCancellationRequested)
        {
            // 大循环之前，打断性能追踪调用链
            DefaultSpan.Current = null;

            ISpan span = null;
            try
            {
                // 异步阻塞消费
                var mqMsgs = await TakeMessagesAsync(batchSize, timeout, cancellationToken);
                if (mqMsgs != null && mqMsgs.Count > 0)
                {
                    // 埋点
                    span = Redis.Tracer?.NewSpan($"redismq:{topic}:Consumes", mqMsgs);

                    var bodys = mqMsgs[0].Body;
                    for (var i = 0; i < bodys.Length; i++)
                        if (bodys[i].EqualIgnoreCase("traceParent") && i + 1 < bodys.Length) span.Detach(bodys[i + 1]);

                    // 解码
                    var msgs = mqMsgs.Select(e => e.GetBody<T>()).ToArray();

                    // 处理消息
                    await onMessage(msgs, mqMsgs.ToArray(), cancellationToken);

                    // 确认消息
                    Acknowledge(mqMsgs.Select(e => e.Id).ToArray());
                }
                else
                    // 没有消息，歇一会
                    await Task.Delay(1000, cancellationToken);
            }
            catch (ThreadAbortException) { break; }
            catch (ThreadInterruptedException) { break; }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) break;

                span?.SetError(ex, null);
            }
            finally
            {
                span?.Dispose();
            }
        }
    }

    /// <summary>队列消费大循环，批量处理消息后自动确认</summary>
    /// <param name="onMessage">消息处理。如果处理消息时抛出异常，消息将延迟后回到队列</param>
    /// <param name="batchSize">批大小。默认100</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task ConsumeAsync(Action<T[]> onMessage, Int32 batchSize = 100, CancellationToken cancellationToken = default) => await ConsumeAsync((m, k, t) => { onMessage(m); return Task.FromResult(0); }, batchSize, cancellationToken);
    #endregion
}