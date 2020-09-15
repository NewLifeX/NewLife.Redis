using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NewLife.Caching.Models;
using NewLife.Data;
using NewLife.Reflection;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Caching
{
    /// <summary>Redis5.0的Stream数据结果，完整态消息队列，支持多消费组</summary>
    /// <typeparam name="T"></typeparam>
    public class RedisStream<T> : RedisBase, IProducerConsumer<T>
    {
        #region 属性
        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("XLEN", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        /// <summary>基元类型数据添加该key构成集合。默认__data</summary>
        public String PrimitiveKey { get; set; } = "__data";

        /// <summary>最大队列长度。默认10万</summary>
        public Int32 MaxLenngth { get; set; } = 100_000;

        /// <summary>开始编号。默认0-0</summary>
        public String StartId { get; set; } = "0-0";

        /// <summary>消费者组</summary>
        public String Group { get; set; }

        private IDictionary<String, PropertyInfo> _properties;
        #endregion

        #region 构造
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisStream(Redis redis, String key) : base(redis, key) { }
        #endregion

        #region 核心生产消费
        /// <summary>生产添加</summary>
        /// <param name="value">消息体</param>
        /// <param name="msgId">消息ID</param>
        /// <returns>返回消息ID</returns>
        public String Add(T value, String msgId = null)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var args = new List<Object> { Key };
            if (MaxLenngth > 0)
            {
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
                foreach (var item in value.ToDictionary())
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
            var rs = !Group.IsNullOrEmpty() ? ReadGroup(Group, null, count) : Read(StartId, count);
            if (rs == null || rs.Count == 0) yield break;

            foreach (var item in rs)
            {
                SetNextId(item.Key);

                var vs = item.Value;
                if (vs != null) yield return Convert(vs);
            }
        }

        private T Convert(String[] vs)
        {
            if (vs.Length == 2 && vs[0] == PrimitiveKey)
                return vs[1].ChangeType<T>();

            if (_properties == null) _properties = typeof(T).GetProperties(true).ToDictionary(e => e.Name, e => e);

            // 字节数组转实体对象
            var entry = Activator.CreateInstance<T>();
            for (var i = 0; i < vs.Length - 1; i += 2)
            {
                if (_properties.TryGetValue(vs[i], out var pi))
                {
                    pi.SetValue(entry, vs[i + 1].ChangeType(pi.PropertyType), null);
                }
            }
            return entry;
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
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<T> TakeOneAsync(Int32 timeout = 0)
        {
            var rs = !Group.IsNullOrEmpty() ?
                await ReadGroupAsync(Group, null, 1, timeout * 1000) :
                await ReadAsync(StartId, 1, timeout * 1000);
            if (rs == null || rs.Count == 0) return default;

            var kv = rs.First();

            // 全局消费（非消费组）时，更新编号
            if (Group.IsNullOrEmpty()) SetNextId(kv.Key);

            return Convert(kv.Value);
        }

        /// <summary>消费确认</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Int32 Acknowledge(params String[] keys)
        {
            var rs = 0;
            foreach (var item in keys)
            {
                rs += Ack(null, item);
            }
            return rs;
        }
        #endregion

        #region 内部命令
        /// <summary>删除指定消息</summary>
        /// <param name="id">消息Id</param>
        /// <returns></returns>
        public Int32 Delete(String id) => Execute(rc => rc.Execute<Int32>("XDEL", Key, id), true);

        /// <summary>确认消息</summary>
        /// <param name="group">消费组名称</param>
        /// <param name="id">消息Id</param>
        /// <returns></returns>
        public Int32 Ack(String group, String id) => Execute(rc => rc.Execute<Int32>("XACK", Key, group, id), true);

        /// <summary>获取区间消息</summary>
        /// <param name="startId"></param>
        /// <param name="endId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IDictionary<String, String[]> Range(String startId, String endId, Int32 count = -1)
        {
            if (startId.IsNullOrEmpty()) startId = "-";
            if (endId.IsNullOrEmpty()) endId = "+";

            var rs = count > 0 ?
                Execute(rc => rc.Execute<Object[]>("XRANGE", Key, startId, endId, "COUNT", count), false) :
                Execute(rc => rc.Execute<Object[]>("XRANGE", Key, startId, endId), false);

            return Parse(rs);
        }

        /// <summary>获取区间消息</summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IDictionary<String, String[]> Range(DateTime start, DateTime end, Int32 count = -1) => Range(start.ToLong() + "-0", end.ToLong() + "-0", count);

        /// <summary>原始独立消费</summary>
        /// <param name="startId">开始编号</param>
        /// <param name="count">消息个数</param>
        /// <returns></returns>
        public IDictionary<String, String[]> Read(String startId, Int32 count)
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
#if DEBUG
                System.Diagnostics.Debug.Assert(vs[0] is Packet pk && pk.ToStr() == Key);
#endif
                if (vs[1] is Object[] vs2) return Parse(vs2);
            }

            return null;
        }

        /// <summary>异步原始独立消费</summary>
        /// <param name="startId">开始编号</param>
        /// <param name="count">消息个数</param>
        /// <param name="block">阻塞毫秒数，0表示永远</param>
        /// <returns></returns>
        public async Task<IDictionary<String, String[]>> ReadAsync(String startId, Int32 count, Int32 block = -1)
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

            var rs = await ExecuteAsync(rc => rc.ExecuteAsync<Object[]>("XREAD", args.ToArray()), true);
            if (rs != null && rs.Length == 1 && rs[0] is Object[] vs && vs.Length == 2)
            {
                if (vs[1] is Object[] vs2) return Parse(vs2);
            }

            return null;
        }

        private IDictionary<String, String[]> Parse(Object[] vs2)
        {
            var dic = new Dictionary<String, String[]>();
            foreach (var item in vs2)
            {
                if (item is Object[] vs3 && vs3.Length == 2 && vs3[0] is Packet pkId && vs3[1] is Object[] vs4)
                {
                    var id = pkId.ToStr();
                    dic[id] = vs4.Select(e => (e as Packet).ToStr()).ToArray();
                }
            }
            return dic;
        }
        #endregion

        #region 消费组
        /// <summary>创建消费组</summary>
        /// <param name="group">消费组名称</param>
        /// <param name="startId">开始编号。0表示从开头，$表示从末尾，收到下一条生产消息才开始消费</param>
        /// <returns></returns>
        public Boolean GroupCreate(String group, String startId = null)
        {
            if (startId.IsNullOrEmpty()) startId = "0";

            return Execute(rc => rc.Execute<Boolean>("XGROUP", "CREATE", Key, group, startId), true);
        }

        /// <summary>销毁消费组</summary>
        /// <param name="group">消费组名称</param>
        /// <returns></returns>
        public Boolean GroupDestroy(String group) => Execute(rc => rc.Execute<Boolean>("XGROUP", "DESTROY", Key, group), true);

        /// <summary>销毁消费组</summary>
        /// <param name="group">消费组名称</param>
        /// <param name="consumer">消费者</param>
        /// <returns>返回消费者在被删除之前所拥有的待处理消息数量</returns>
        public Int32 GroupDeleteConsumer(String group, String consumer) => Execute(rc => rc.Execute<Int32>("XGROUP", "DELCONSUMER", Key, group, consumer), true);

        /// <summary>设置消费组Id</summary>
        /// <param name="group">消费组名称</param>
        /// <param name="startId">开始编号</param>
        /// <returns></returns>
        public Boolean GroupSetId(String group, String startId)
        {
            if (startId.IsNullOrEmpty()) startId = "$";

            return Execute(rc => rc.Execute<Boolean>("XGROUP", "SETID", Key, group, startId), true);
        }

        /// <summary>消费组消费</summary>
        /// <param name="group"></param>
        /// <param name="consumer"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IDictionary<String, String[]> ReadGroup(String group, String consumer, Int32 count)
        {
            if (consumer.IsNullOrEmpty()) consumer = $"{Environment.MachineName}@{Process.GetCurrentProcess().Id}";

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
        /// <param name="group"></param>
        /// <param name="consumer"></param>
        /// <param name="count"></param>
        /// <param name="block">阻塞毫秒数，0表示永远</param>
        /// <returns></returns>
        public async Task<IDictionary<String, String[]>> ReadGroupAsync(String group, String consumer, Int32 count, Int32 block = -1)
        {
            if (consumer.IsNullOrEmpty()) consumer = $"{Environment.MachineName}@{Process.GetCurrentProcess().Id}";

            var rs = count > 0 ?
                await ExecuteAsync(rc => rc.ExecuteAsync<Object[]>("XREADGROUP", new Object[] { "GROUP", group, consumer, "BLOCK", block, "COUNT", count, "STREAMS", Key, ">" }), true) :
                await ExecuteAsync(rc => rc.ExecuteAsync<Object[]>("XREADGROUP", new Object[] { "GROUP", group, consumer, "BLOCK", block, "STREAMS", Key, ">" }), true);
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