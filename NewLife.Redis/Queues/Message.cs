using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching.Queues;

/// <summary>消息队列中消费得到的消息</summary>
public class Message
{
    /// <summary>消息标识</summary>
    public String? Id { get; set; }

    /// <summary>消息体</summary>
    public String[]? Body { get; set; }

    /// <summary>解码消息体为具体类型</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetBody<T>()
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
            if (vs[i] != null && properties.TryGetValue(vs[i], out var pi) && pi.CanWrite)
            {
                // 复杂类型序列化为json字符串
                var val = vs[i + 1];
                Object? v;
                if (pi.PropertyType == typeof(Object))
                    // 有的模型类属性就是Object类型
                    v = val;
                else if (pi.PropertyType.GetTypeCode() == TypeCode.Object)
                    //TODO:复杂对像的解析目前无法把编码器应用到这里，后续需要改进。
                    //ToJsonEntity,如果对像是中文字串，以object转对像会异常，由ToJsonEntity决定
                    //"内容".ToJsonEntity(typeof(object)) 出错
                    //"888".ToJsonEntity(typeof(object)) 正常
                    v = val.ToJsonEntity(pi.PropertyType);
                else
                    v = val.ChangeType(pi.PropertyType);

                pi.SetValue(entry, v, null);
            }
        }

        return entry;
    }
}