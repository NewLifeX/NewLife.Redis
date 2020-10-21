using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching.Models;
using NewLife.Data;
using NewLife.Log;
using NewLife.Serialization;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Caching
{
    /// <summary>Redis5.0的Stream数据结构，完整态消息队列，支持多消费组</summary>
    /// <typeparam name="T"></typeparam>
    public class RedisStream<T> : RedisBase, IProducerConsumer<T>
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

        /// <summary>最大队列长度。默认100万</summary>
        public Int32 MaxLenngth { get; set; } = 1_000_000;

        /// <summary>最大重试次数。超过该次数后，消息将被抛弃，默认10次</summary>
        public Int32 MaxRetry { get; set; } = 10;

        /// <summary>开始编号。独立消费时使用，消费组消费时不使用，默认0-0</summary>
        public String StartId { get; set; } = "0-0";

        /// <summary>消费者组。指定消费组后，不再使用独立消费</summary>
        public String Group { get; set; }

        /// <summary>消费者</summary>
        public String Consumer { get; set; }

        /// <summary>是否在消息报文中自动注入TraceId。TraceId用于跨应用在生产者和消费者之间建立调用链，默认true</summary>
        public Boolean AttachTraceId { get; set; } = true;

        private Int32 _count;
        #endregion

        #region 构造
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisStream(Redis redis, String key) : base(redis, key)
        {
            Consumer = $"{Environment.MachineName}@{Process.GetCurrentProcess().Id}";
        }
        #endregion

        #region 核心生产消费
        /// <summary>生产添加</summary>
        /// <param name="value">消息体</param>
        /// <param name="msgId">消息ID</param>
        /// <returns>返回消息ID</returns>
        public String Add(T value, String msgId = null)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            using var span = Redis.Tracer?.NewSpan($"redismq:AddStream:{Key}", value);

            // 自动修剪超长部分，每1000次生产，修剪一次
            if (_count <= 0) _count = Count;
            Interlocked.Increment(ref _count);

            var args = new List<Object> { Key };
            if (MaxLenngth > 0 && _count % 1000 == 0)
            {
                _count = Count + 1;

                args.Add("maxlen");
                args.Add("~");
                args.Add(MaxLenngth);
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
            {
                foreach (var item in (value as Array))
                {
                    args.Add(item);
                }
            }
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

            return Execute(rc => rc.Execute<String>("XADD", args.ToArray()), true);
        }

        /// <summary>批量生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        Int32 IProducerConsumer<T>.Add(params T[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            foreach (var item in values)
            {
                Add(item);
            }

            return values.Length;
        }

        /// <summary>批量消费获取，前移指针StartId</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            var group = Group;
            if (!group.IsNullOrEmpty()) RetryAck();

            var rs = !group.IsNullOrEmpty() ?
                ReadGroup(group, Consumer, count) :
                Read(StartId, count);
            if (rs == null || rs.Count == 0) yield break;

            foreach (var item in rs)
            {
                if (group.IsNullOrEmpty()) SetNextId(item.Id);

                yield return item.GetBody<T>();
            }
        }

        private void SetNextId(String key)
        {
            var lastId = key;
            var ss = lastId.Split('-');
            if (ss.Length == 2) StartId = $"{ss[0]}-{ss[1].ToInt() + 1}";
        }

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
            var group = Group;
            if (!group.IsNullOrEmpty()) RetryAck();

            var rs = !group.IsNullOrEmpty() ?
                await ReadGroupAsync(group, Consumer, 1, timeout * 1000, cancellationToken) :
                await ReadAsync(StartId, 1, timeout * 1000, cancellationToken);
            if (rs == null || rs.Count == 0) return null;

            // 全局消费（非消费组）时，更新编号
            if (group.IsNullOrEmpty()) SetNextId(rs[0].Id);

            return rs[0];
        }

        /// <summary>消费确认</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Int32 Acknowledge(params String[] keys)
        {
            var rs = 0;
            foreach (var item in keys)
            {
                rs += Ack(Group, item);
            }
            return rs;
        }
        #endregion

        #region 死信处理
        private DateTime _nextRetry;
        /// <summary>处理未确认的死信，重新放入队列</summary>
        private void RetryAck()
        {
            var now = DateTime.Now;
            // 一定间隔处理当前ukey死信
            if (_nextRetry < now)
            {
                _nextRetry = now.AddSeconds(RetryInterval);
                var retry = RetryInterval * 1000;

                // 拿到死信，重新放入队列
                var list = Pending(Group, null, null, 100);
                foreach (var item in list)
                {
                    if (item.Idle > retry)
                    {
                        if (item.Delivery >= MaxRetry)
                        {
                            Delete(item.Id);
                            XTrace.WriteLine("删除多次失败死信：{0}", item.ToJson());
                        }
                        else
                        {
                            Claim(Group, Consumer, item.Id, retry);
                            XTrace.WriteLine("定时回滚：{0}", item.ToJson());
                        }
                    }
                }
            }
        }
        #endregion

        #region 内部命令
        /// <summary>删除指定消息</summary>
        /// <param name="id">消息Id</param>
        /// <returns></returns>
        public Int32 Delete(String id) => Execute(rc => rc.Execute<Int32>("XDEL", Key, id), true);

        /// <summary>裁剪队列到指定大小</summary>
        /// <param name="maxLen">最大长度。为了提高效率，最大长度并没有那么精准</param>
        /// <returns></returns>
        public Int32 Trim(Int32 maxLen) => Execute(rc => rc.Execute<Int32>("XTRIM", Key, "MAXLEN", "~", maxLen), true);

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
        public Int32 Claim(String group, String consumer, String id, Int32 msIdle = 3_600_000) => Execute(rc => rc.Execute<Int32>("XCLAIM", Key, group, consumer, msIdle, id), true);

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
            {
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
            }

            return null;
        }

        /// <summary>异步原始独立消费</summary>
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
            {
                if (vs[1] is Object[] vs2) return Parse(vs2);
            }

            return null;
        }

        private IList<Message> Parse(Object[] vs)
        {
            var list = new List<Message>();
            foreach (var item in vs)
            {
                if (item is Object[] vs3 && vs3.Length == 2 && vs3[0] is Packet pkId && vs3[1] is Object[] vs4)
                {
                    list.Add(new Message
                    {
                        Id = pkId.ToStr(),
                        Body = vs4.Select(e => (e as Packet).ToStr()).ToArray(),
                    });
                }
            }
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
        /// <param name="startId">开始编号。0表示从开头，$表示从末尾，收到下一条生产消息才开始消费</param>
        /// <returns></returns>
        public Boolean GroupCreate(String group, String startId = null)
        {
            if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));
            if (startId.IsNullOrEmpty()) startId = "0";

            return Execute(rc => rc.Execute<Boolean>("XGROUP", "CREATE", Key, group, startId), true);
        }

        /// <summary>销毁消费组</summary>
        /// <param name="group">消费组名称</param>
        /// <returns></returns>
        public Boolean GroupDestroy(String group)
        {
            if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

            return Execute(rc => rc.Execute<Boolean>("XGROUP", "DESTROY", Key, group), true);
        }

        /// <summary>销毁消费组</summary>
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

            return Execute(rc => rc.Execute<Boolean>("XGROUP", "SETID", Key, group, startId), true);
        }

        /// <summary>消费组消费</summary>
        /// <param name="group">消费组</param>
        /// <param name="consumer">消费组</param>
        /// <param name="count">消息个数</param>
        /// <returns></returns>
        public IList<Message> ReadGroup(String group, String consumer, Int32 count)
        {
            if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

            var rs = count > 0 ?
                Execute(rc => rc.Execute<Object[]>("XREADGROUP", "GROUP", group, consumer, "COUNT", count, "STREAMS", Key, ">"), true) :
                Execute(rc => rc.Execute<Object[]>("XREADGROUP", "GROUP", group, consumer, "STREAMS", Key, ">"), true);
            if (rs != null && rs.Length == 1 && rs[0] is Object[] vs && vs.Length == 2)
            {
                if (vs[1] is Object[] vs2) return Parse(vs2);
            }

            return null;
        }

        /// <summary>异步消费组消费</summary>
        /// <param name="group">消费组</param>
        /// <param name="consumer">消费组</param>
        /// <param name="count">消息个数</param>
        /// <param name="block">阻塞毫秒数，0表示永远</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public async Task<IList<Message>> ReadGroupAsync(String group, String consumer, Int32 count, Int32 block = -1, CancellationToken cancellationToken = default)
        {
            if (group.IsNullOrEmpty()) throw new ArgumentNullException(nameof(group));

            var rs = count > 0 ?
                await ExecuteAsync(rc => rc.ExecuteAsync<Object[]>("XREADGROUP", new Object[] { "GROUP", group, consumer, "BLOCK", block, "COUNT", count, "STREAMS", Key, ">" }, cancellationToken), true) :
                await ExecuteAsync(rc => rc.ExecuteAsync<Object[]>("XREADGROUP", new Object[] { "GROUP", group, consumer, "BLOCK", block, "STREAMS", Key, ">" }, cancellationToken), true);
            if (rs != null && rs.Length == 1 && rs[0] is Object[] vs && vs.Length == 2)
            {
                if (vs[1] is Object[] vs2) return Parse(vs2);
            }

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
    }
}