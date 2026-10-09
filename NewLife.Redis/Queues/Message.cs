using System.Diagnostics.CodeAnalysis;
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

    /// <summary>获取指定字段的值</summary>
    /// <param name="key">字段名</param>
    /// <returns>字段值；不存在时返回null</returns>
    public String? GetValue(String key)
    {
        var vs = Body;
        if (vs == null || vs.Length == 0) return null;

        for (var i = 0; i < vs.Length - 1; i += 2)
        {
            if (vs[i] == key) return vs[i + 1];
        }

        return null;
    }

    /// <summary>解码消息体为具体类型</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="primitiveKey">基元类型的数据字段名。默认__data，对应RedisStream.PrimitiveKey</param>
    /// <returns></returns>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "属性类型来自运行期反射，无法静态表达保留要求；主链路已通过泛型T的保留注解保根，未保根时由序列化层抛异常降级")]
    public T? GetBody<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] T>(String? primitiveKey = null)
    {
        var vs = Body;
        if (vs == null || vs.Length == 0) return default;

        // 特殊类型处理
        var type = typeof(T);
        if (type == typeof(Object)) return (T)(Object)vs;
        if (type == typeof(String[])) return (T)(Object)vs;

        // 数据字段名。字段增强器可能追加额外字段，因此不能固定字段位置
        var key = primitiveKey.IsNullOrEmpty() ? "__data" : primitiveKey;
        var data = GetValue(key);

        if (!data.IsNullOrEmpty() && type == typeof(String)) return (T)(Object)data;

        // 基元类型：按字段名查找数据值，兼容没有数据字段的历史数据
        if (type.IsBaseType())
        {
            if (data != null) return data.ChangeType<T>();
            if (type == typeof(String)) return (T)(Object)vs.Join();

            return default;
        }

        // 复杂类型：历史数据把Json字符串放在两个字段中
        if (vs.Length == 2 && vs[0] == key) return vs[1].ChangeType<T>();

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
                    v = val.ToJsonEntity(pi.PropertyType, null);
                else
                    v = val.ChangeType(pi.PropertyType);

                pi.SetValue(entry, v, null);
            }
        }

        return entry;
    }
}