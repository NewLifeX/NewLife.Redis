using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching;

/// <summary>Redis编码器</summary>
public class RedisJsonEncoder : IPacketEncoder
{
    #region 属性
    /// <summary>解码出错时抛出异常。默认false不抛出异常，仅返回默认值</summary>
    public Boolean ThrowOnError { get; set; }

    /// <summary>用于对复杂对象进行Json序列化的主机。优先SystemJson，内部FastJson兜底</summary>
    public IJsonHost JsonHost { get; set; } = _host;

    private static IJsonHost _host;
    #endregion

    static RedisJsonEncoder() => _host = GetJsonHost();

    internal static IJsonHost GetJsonHost()
    {
        // 尝试使用System.Text.Json，不支持时使用FastJson
        var host = JsonHelper.Default;
        if (host == null || host.GetType().Name == "FastJson")
        {
            // 当前组件输出net45和netstandard2.0，而SystemJson要求net5以上，因此通过反射加载
            try
            {
                var type = $"{typeof(FastJson).Namespace}.SystemJson".GetTypeEx();
                if (type != null)
                {
                    host = type.CreateInstance() as IJsonHost;
                }
            }
            catch { }
        }

        return host ?? JsonHelper.Default;
    }

    /// <summary>数值转数据包</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual IPacket? Encode(Object value)
    {
        if (value == null) return null;

        if (value is IPacket pk) return pk;
        if (value is Byte[] buf) return (ArrayPacket)buf;
        if (value is IAccessor acc) return acc.ToPacket();

        var type = value.GetType();
        var str = type.GetTypeCode() switch
        {
            TypeCode.Object => JsonHost.Write(value),
            TypeCode.String => (value as String)!,
            TypeCode.DateTime => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff"),
            _ => value + "",
        };

        return (ArrayPacket)str.GetBytes();
    }

    /// <summary>解码数据包为目标类型</summary>
    /// <param name="pk"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public virtual Object? Decode(IPacket pk, Type type)
    {
        try
        {
            if (type == typeof(IPacket)) return pk;
            if (type == typeof(Byte[])) return pk.ReadBytes();
            if (type.As<IAccessor>()) return type.AccessorRead(pk);

            // 支持可空类型，遇到无数据时返回null
            var ntype = Nullable.GetUnderlyingType(type);
            if (pk.Length == 0 && ntype != null && ntype != type) return null;
            if (ntype != null) type = ntype;

            var str = pk.ToStr();
            if (type.GetTypeCode() == TypeCode.String) return str;

            if (type.GetTypeCode() != TypeCode.Object)
            {
                if (type == typeof(Boolean) && str == "OK") return true;

                return str.ChangeType(type);
            }

            // 判断是否Json字符串
            if (str[0] == '{' && str[^1] == '}')
                return JsonHost.Read(str, type);

            return null;
        }
        catch
        {
            if (ThrowOnError) throw;

            return null;
        }
    }
}

/// <summary>编解码助手</summary>
public static class RedisJsonEncoderHelper
{
    //public static T Decode<T>(this RedisJsonEncoder encoder, Packet pk) => (T)encoder.Decode(pk, typeof(T))!;

    /// <summary>解码数据包</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="encoder"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static T Decode<T>(this IPacketEncoder encoder, IPacket pk) => (T)encoder.Decode(pk, typeof(T))!;
}