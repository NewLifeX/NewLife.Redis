using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;

namespace NewLife.Caching
{
    /// <summary>可靠Redis队列，左进右出</summary>
    /// <remarks>
    /// 严格模式下消费，弹出消息的同时插入Ack队列，消费者处理成功后将从ACK队列删除该消息，若处理失败，则将延迟消费Ack消息；
    /// 
    /// 为了让严格模式支持多线程消费，确认队列AckKey构造为 Key:Ack:Rand16 的格式，每一个消费者都将有自己完全独一无二的确认队列。
    /// 消费者每30秒（RetryInterval）清理一次确认队列的死信（未确认消息），重新投入主队列。
    /// 应用异常退出时，可能产生一些死信，在应用启动首次消费时通过TakeAllAck消费清理所有Ack队列。
    /// 由于引入状态队列，清理不活跃消费者时，不会影响正常消费者。
    /// 
    /// 设计要点：
    /// 1，消费时，RPOPLPUSH从Key弹出并备份到AckKey，消息处理完成后，再从AckKey删除
    /// 2，AckKey设计为Key:Ack:ukey，ukey=Rand16，让每个实例都有专属的Ack确认队列
    /// 3，消费时，每60秒更新一次状态到Key:Status:ukey，表明ukey还在消费
    /// 4，全局定期扫描Key:Status:ukey，若不活跃，回滚它的Ack消息
    /// 
    /// 消费者要慎重处理错误消息，有可能某条消息一直处理失败，如果未确认，队列会反复把消息送回主队列。
    /// 建议用户自己处理并确认消费，通过消息体或者redisKey计数。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class RedisReliableQueue<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>用于确认的列表</summary>
        public String AckKey { get; set; }

        /// <summary>重新处理确认队列中死信的间隔。默认60s</summary>
        public Int32 RetryInterval { get; set; } = 60;

        /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
        public Int32 MinPipeline { get; set; } = 3;

        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("LLEN", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        private readonly String _Key;
        private readonly String _StatusKey;
        private readonly Status _Status;
        #endregion

        #region 构造
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisReliableQueue(Redis redis, String key) : base(redis, key)
        {
            _Key = key;
            _Status = CreateStatus();
            AckKey = $"{key}:Ack:{_Status.UKey}";
            _StatusKey = $"{key}:Status:{_Status.UKey}";
        }
        #endregion

        #region 核心方法
        ///// <summary>生产添加</summary>
        ///// <param name="value">消息</param>
        ///// <returns></returns>
        //public Int32 Add(T value) => Execute(rc => rc.Execute<Int32>("LPUSH", Key, value), true);

        /// <summary>批量生产添加</summary>
        /// <param name="values">消息集合</param>
        /// <returns></returns>
        public Int32 Add(params T[] values)
        {
            var args = new List<Object> { Key };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("LPUSH", args.ToArray()), true);
        }

        /// <summary>消费获取，从Key弹出并备份到AckKey，支持阻塞</summary>
        /// <remarks>假定前面获取的消息已经确认，因该方法内部可能回滚确认队列，避免误杀</remarks>
        /// <param name="timeout">超时，0秒永远阻塞；负数表示直接返回，不阻塞。</param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0)
        {
            RetryDeadAck();

            return timeout >= 0 ?
                Execute(rc => rc.Execute<T>("BRPOPLPUSH", Key, AckKey, timeout), true) :
                Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);
        }

        /// <summary>异步消费获取</summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<T> TakeOneAsync(Int32 timeout = 0)
        {
#if NET4
            throw new NotSupportedException();
#else
            RetryDeadAck();

            if (timeout < 0) return await ExecuteAsync(rc => rc.ExecuteAsync<T>("RPOPLPUSH", Key, AckKey), true);

            return await ExecuteAsync(rc => rc.ExecuteAsync<T>("BRPOPLPUSH", Key, AckKey, timeout), true);
#endif
        }

        /// <summary>批量消费获取，从Key弹出并备份到AckKey</summary>
        /// <remarks>假定前面获取的消息已经确认，因该方法内部可能回滚确认队列，避免误杀</remarks>
        /// <param name="count">要消费的消息个数</param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            RetryDeadAck();

            // 借助管道支持批量获取
            if (count >= MinPipeline)
            {
                var rds = Redis;
                rds.StartPipeline();

                for (var i = 0; i < count; i++)
                {
                    Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);
                }

                var rs = rds.StopPipeline(true);
                foreach (var item in rs)
                {
                    if (!Equals(item, default(T))) yield return (T)item;
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var value = Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, AckKey), true);
                    if (Equals(value, default(T))) break;

                    yield return value;
                }
            }
        }

        ///// <summary>确认消费，从AckKey中删除</summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public Int32 Acknowledge(T value) => Execute(r => r.Execute<Int32>("LREM", AckKey, 1, value), true);

        /// <summary>确认消费，从AckKey中删除</summary>
        /// <param name="keys"></param>
        public Int32 Acknowledge(params String[] keys)
        {
            var rs = 0;

            // 管道支持
            if (keys.Count() >= MinPipeline)
            {
                var rds = Redis;
                rds.StartPipeline();

                foreach (var item in keys)
                {
                    Execute(r => r.Execute<Int32>("LREM", AckKey, 1, item), true);
                }

                var rs2 = rds.StopPipeline(true);
                foreach (var item in rs2)
                {
                    rs += (Int32)item;
                }
            }
            else
            {
                foreach (var item in keys)
                {
                    rs += Execute(r => r.Execute<Int32>("LREM", AckKey, 1, item), true);
                }
            }

            return rs;
        }
        #endregion

        #region 死信处理
        /// <summary>从确认列表弹出消息，用于消费中断后，重新恢复现场时获取</summary>
        /// <remarks>理论上Ack队列只存储极少数数据</remarks>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> TakeAck(Int32 count = 1)
        {
            if (count <= 0) yield break;

            for (var i = 0; i < count; i++)
            {
                var value = Execute(rc => rc.Execute<T>("RPOP", AckKey), true);
                if (Equals(value, default(T))) break;

                yield return value;
            }
        }

        /// <summary>消费所有确认队列中的遗留数据，一般在应用启动时由单一线程执行</summary>
        /// <returns></returns>
        public IEnumerable<T> TakeAllAck()
        {
            var rds = Redis as FullRedis;

            // 先找到所有Key
            foreach (var key in rds.Search($"{_Key}:Ack:*", 100))
            {
                // 消费所有数据
                while (true)
                {
                    var value = Execute(rc => rc.Execute<T>("RPOP", key), true);
                    if (Equals(value, default(T))) break;

                    yield return value;
                }
            }
        }

        /// <summary>清空所有Ack队列。危险操作！！！</summary>
        /// <returns></returns>
        public Int32 ClearAllAck() => TakeAllAck().Count();

        /// <summary>回滚指定AckKey内的消息到Key</summary>
        /// <param name="key"></param>
        /// <param name="ackKey"></param>
        /// <returns></returns>
        private List<T> RollbackAck(String key, String ackKey)
        {
            // 消费所有数据
            var list = new List<T>();
            while (true)
            {
                var value = Execute(rc => rc.Execute<T>("RPOPLPUSH", ackKey, key), true);
                if (Equals(value, default(T))) break;

                list.Add(value);
            }

            return list;
        }

        /// <summary>全局回滚死信，一般由单一线程执行，避免干扰处理中数据</summary>
        /// <returns></returns>
        public Int32 RollbackAllAck()
        {
            var rds = Redis as FullRedis;

            // 先找到所有Key
            var count = 0;
            foreach (var key in rds.Search($"{_Key}:Status:*", 100))
            {
                var st = rds.Get<Status>(key);
                if (st != null && st.LastActive.AddSeconds(RetryInterval * 10) < DateTime.Now)
                {
                    var ackKey = $"{_Key}:Ack:{st.UKey}";
                    if (rds.ContainsKey(ackKey))
                    {
                        XTrace.WriteLine("发现死信队列：{0}", ackKey);

                        var list = RollbackAck(_Key, ackKey);
                        foreach (var item in list)
                        {
                            XTrace.WriteLine("全局回滚死信：{0}", item);
                        }

                        count += list.Count;
                    }

                    // 删除状态
                    rds.Remove(key);
                    XTrace.WriteLine("删除队列状态：{0} {1}", key, st.ToJson());
                }
            }

            return count;
        }

        private DateTime _nextRetry;
        /// <summary>处理未确认的死信，重新放入队列</summary>
        private void RetryDeadAck()
        {
            var now = DateTime.Now;
            // 一定间隔处理当前ukey死信
            if (_nextRetry < now)
            {
                _nextRetry = now.AddSeconds(RetryInterval);

                // 拿到死信，重新放入队列
                var list = RollbackAck(_Key, AckKey);
                foreach (var item in list)
                {
                    XTrace.WriteLine("定时回滚死信：{0}", item);
                }

                // 更新状态
                UpdateStatus();

                // 处理其它消费者遗留下来的死信
                RollbackAllAck();
            }
        }
        #endregion

        #region 状态
        private static readonly Status _def = new Status
        {
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            ProcessId = Process.GetCurrentProcess().Id,
            Ip = NetHelper.MyIP() + "",
        };
        private Status CreateStatus()
        {
            return new Status
            {
                UKey = Rand.NextString(8),
                MachineName = _def.MachineName,
                UserName = _def.UserName,
                ProcessId = _def.ProcessId,
                Ip = _def.Ip,
                CreateTime = DateTime.Now,
                LastActive = DateTime.Now,
            };
        }

        private void UpdateStatus()
        {
            // 更新状态，永不过期
            _Status.LastActive = DateTime.Now;
            Redis.Set(_StatusKey, _Status);
        }

        private class Status
        {
            /// <summary>标识消费者的唯一Key</summary>
            public String UKey { get; set; }

            /// <summary>机器名</summary>
            public String MachineName { get; set; }

            /// <summary>用户名</summary>
            public String UserName { get; set; }

            /// <summary>进程</summary>
            public Int32 ProcessId { get; set; }

            /// <summary>IP地址</summary>
            public String Ip { get; set; }

            /// <summary>开始时间</summary>
            public DateTime CreateTime { get; set; }

            /// <summary>最后活跃时间</summary>
            public DateTime LastActive { get; set; }

            ///// <summary>消费消息数</summary>
            //public Int64 Consumes { get; set; }

            ///// <summary>确认消息数</summary>
            //public Int64 Acks { get; set; }
        }
        #endregion
    }
}