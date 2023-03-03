﻿using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching.Models;

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

        // 特殊类型处理
        var type = typeof(T);
        if (type == typeof(Object)) return (T)(Object)vs;
        if (type == typeof(String[])) return (T)(Object)vs;

        if (vs.Length == 2 && vs[0] == "__data") return vs[1].ChangeType<T>();
        if (type == typeof(String)) return (T)(Object)vs.Join();

        var properties = typeof(T).GetProperties(true).ToDictionary(e => e.Name, e => e);

        // 字节数组转实体对象
        var entry = Activator.CreateInstance<T>();
        for (var i = 0; i < vs.Length - 1; i += 2)
        {
            if (vs[i] != null && properties.TryGetValue(vs[i], out var pi))
            {
                // 复杂类型序列化为json字符串
                var val = vs[i + 1];
                object v;
                if (pi.PropertyType.GetTypeCode() == TypeCode.Object)
                {
                    try
                    {
                        //ToJsonEntity,如果对像是中文字串，以object转对像会异常，TODO:修改ToJsonEntity的具体实现
                        //"内容".ToJsonEntity(typeof(object)) 出错
                        //"888".ToJsonEntity(typeof(object)) 正常
                        v = val.ToJsonEntity(pi.PropertyType);

                    }
                    catch (NewLife.XException err)
                    {
                        v = val.ChangeType(pi.PropertyType);
                    }
                }
                else
                {
                    v = val.ChangeType(pi.PropertyType);
                }

                pi.SetValue(entry, v, null);
            }
        }
        return entry;
    }
}
