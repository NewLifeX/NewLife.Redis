using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Collections;

namespace NewLife.Caching
{
    /// <summary>Redis5.0的Stream数据结果，完整态消息队列，支持多消费组</summary>
    public class RedisStream : RedisBase, IProducerConsumer<String>
    {
        #region 属性
        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("XLEN", Key));

        /// <summary>是否为空</summary>
        public Boolean IsEmpty => Count == 0;

        /// <summary>最小管道阈值，达到该值时使用管道，默认3</summary>
        public Int32 MinPipeline { get; set; } = 3;
        #endregion

        #region 构造
        /// <summary>实例化队列</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisStream(Redis redis, String key) : base(redis, key) { }
        #endregion

        /// <summary>生产添加</summary>
        /// <param name="value"></param>
        /// <param name="maxlen"></param>
        /// <returns></returns>
        public String Add(String value, Int32 maxlen = -1)
        {
            var cmd = maxlen > 0 ?
                $"{Key} maxlen {maxlen} * {value}" :
                $"{Key} * {value}";

            return Execute(rc => rc.Execute<String>("XADD", $"{Key} * {value}"), true);
        }

        /// <summary>批量生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(IEnumerable<String> values)
        {
            var count = 0;
            foreach (var item in values)
            {
                Add(item);
                count++;
            }
            return count;
        }

        /// <summary>删除指定消息</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Int32 Delete(String id) => Execute(rc => rc.Execute<Int32>("XDEL", $"{Key} {id}"), true);

        /// <summary>获取区间消息</summary>
        /// <param name="startId"></param>
        /// <param name="endId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public String[] Range(String startId, String endId, Int32 count = -1)
        {
            if (startId.IsNullOrEmpty()) startId = "-";
            if (endId.IsNullOrEmpty()) endId = "+";

            var cmd = $"{Key} {startId} {endId}";
            if (count > 0) cmd += $" COUNT {count}";

            return Execute(rc => rc.Execute<String[]>("XRANGE", cmd), false);
        }

        /// <summary>获取区间消息</summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public String[] Range(DateTime start, DateTime end, Int32 count = -1)
        {
            return Range(start.ToLong() + "-0", end.ToLong() + "-0", count);
        }

        /// <summary>独立消费</summary>
        /// <param name="startId"></param>
        /// <param name="count"></param>
        /// <param name="block">阻塞描述，0表示永远</param>
        /// <returns></returns>
        public String[] Read(String startId, Int32 count, Int32 block = -1)
        {
            if (startId.IsNullOrEmpty()) startId = "$";

            var sb = Pool.StringBuilder.Get();
            if (block >= 0) sb.AppendFormat("block {0} ", block);
            if (count > 0) sb.AppendFormat("count {0} ", count);

            sb.AppendFormat("streams {0} {1}", Key, startId);

            var cmd = sb.Put(true);

            return Execute(rc => rc.Execute<String[]>("XREAD", cmd), true);
        }

        /// <summary>批量消费获取</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<String> Take(Int32 count = 1) => Read(null, count);

        /// <summary>创建消费组</summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public Boolean CreateGroup(String group)
        {
            return Execute(rc => rc.Execute<Boolean>("XGROUP", "create " + group), true);
        }

        /// <summary>消费组消费</summary>
        /// <param name="group"></param>
        /// <param name="concumer"></param>
        /// <param name="count"></param>
        /// <param name="block">阻塞描述，0表示永远</param>
        /// <returns></returns>
        public String[] ReadGroup(String group, String concumer, Int32 count, Int32 block = -1)
        {
            if (concumer.IsNullOrEmpty()) concumer = $"{Environment.MachineName}@{Process.GetCurrentProcess().Id}";

            var sb = Pool.StringBuilder.Get();
            sb.AppendFormat("group {0} ", group);

            if (block >= 0) sb.AppendFormat("block {0} ", block);
            if (count > 0) sb.AppendFormat("count {0} ", count);

            sb.AppendFormat("streams {0} >", Key);

            var cmd = sb.Put(true);

            return Execute(rc => rc.Execute<String[]>("XREADGROUP", cmd), true);
        }
    }
}