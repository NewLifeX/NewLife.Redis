using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Reflection;

namespace NewLife.Caching.Models
{
    /// <summary>消息队列中消费得到的消息</summary>
    public class Message
    {
        /// <summary>消息标识</summary>
        public String Id { get; set; }

        /// <summary>消息体</summary>
        public String[] Body { get; set; }

        /// <summary>解码消息体为具体类型</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetBody<T>()
        {
            var vs = Body;
            if (vs == null || vs.Length == 0) return default;

            if (vs.Length == 2 && vs[0] == "__data") return vs[1].ChangeType<T>();

            var properties = typeof(T).GetProperties(true).ToDictionary(e => e.Name, e => e);

            // 字节数组转实体对象
            var entry = Activator.CreateInstance<T>();
            for (var i = 0; i < vs.Length - 1; i += 2)
            {
                if (properties.TryGetValue(vs[i], out var pi))
                {
                    pi.SetValue(entry, vs[i + 1].ChangeType(pi.PropertyType), null);
                }
            }
            return entry;
        }
    }
}