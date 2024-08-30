using System.Text;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching;

/// <summary>Redis编码器</summary>
public class RedisJsonEncoder //: IPacketEncoder
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
        if (value is IAccessor acc) value = acc.ToPacket();
        if (value is Packet pk2) value = pk2.ToArray();
        if (value is Byte[] buf) return new MemorySegment(buf);

        var type = value.GetType();
        var str = type.GetTypeCode() switch
        {
            TypeCode.Object => JsonHost.Write(value),
            TypeCode.String => (value as String)!,
            TypeCode.DateTime => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff"),
            _ => value + "",
        };

        return new MemorySegment(str.GetBytes());
    }

    ///// <summary>数据包转对象</summary>
    ///// <param name="pk"></param>
    ///// <param name="type"></param>
    ///// <returns></returns>
    //public virtual Object? Decode(Packet pk, Type type)
    //{
    //    //if (pk == null) return null;

    //    try
    //    {
    //        if (type == typeof(Packet)) return pk;
    //        if (type == typeof(Byte[])) return pk.ReadBytes();
    //        if (type.As<IAccessor>()) return type.AccessorRead(pk);

    //        // 支持可空类型，遇到无数据时返回null
    //        var ntype = Nullable.GetUnderlyingType(type);
    //        if (pk.Total == 0 && ntype != null && ntype != type) return null;
    //        if (ntype != null) type = ntype;

    //        //var str = pk.ToStr().Trim('\"');
    //        var str = pk.ToStr();
    //        if (type.GetTypeCode() == TypeCode.String) return str;

    //        //if (type.GetTypeCode() != TypeCode.Object) return str.ChangeType(type);
    //        if (type.GetTypeCode() != TypeCode.Object)
    //        {
    //            if (type == typeof(Boolean) && str == "OK") return true;

    //            //return Convert.ChangeType(str, type);
    //            return str.ChangeType(type);
    //        }

    //        //return str.ToJsonEntity(type);
    //        return JsonHost.Read(str, type);
    //    }
    //    catch
    //    {
    //        if (ThrowOnError) throw;

    //        return null;
    //    }
    //}

    /// <summary>数值转数据包</summary>
    /// <param name="value"></param>
    /// <param name="span"></param>
    /// <returns></returns>
    public virtual Int32 Encode(Object value, Span<Byte> span)
    {
        if (value == null) return 0;

        if (value is IAccessor acc) value = acc.ToPacket();
        if (value is Packet pk)
        {
            if (span != null) pk.ToArray().CopyTo(span);
            return pk.Total;
        }
        if (value is Byte[] buf)
        {
            if (span != null) buf.CopyTo(span);
            return buf.Length;
        }

        var type = value.GetType();
        var str = (type.GetTypeCode()) switch
        {
            TypeCode.Object => JsonHost.Write(value),
            TypeCode.String => value as String,
            TypeCode.DateTime => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff"),
            _ => value + "",
        };

        if (span == null) return Encoding.UTF8.GetByteCount(str);

        return Encoding.UTF8.GetBytes(str.AsSpan(), span);
    }

    public virtual Object? Decode(IPacket pk, Type type) => Decode(pk.GetSpan(), type);

    /// <summary>数据包转对象</summary>
    /// <param name="span"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public virtual Object? Decode(Span<Byte> span, Type type)
    {
        try
        {
            if (type == typeof(Packet)) return span.ToArray();
            if (type == typeof(Byte[])) return span.ToArray();
            if (type.As<IAccessor>()) return type.AccessorRead(span.ToArray());

            // 支持可空类型，遇到无数据时返回null
            var ntype = Nullable.GetUnderlyingType(type);
            if (span.Length == 0 && ntype != null && ntype != type) return null;
            if (ntype != null) type = ntype;

            var str = Encoding.UTF8.GetString(span);
            if (type.GetTypeCode() == TypeCode.String) return str;

            if (type.GetTypeCode() != TypeCode.Object)
            {
                if (type == typeof(Boolean) && str == "OK") return true;

                return str.ChangeType(type);
            }

            return JsonHost.Read(str, type);
        }
        catch
        {
            if (ThrowOnError) throw;

            return null;
        }
    }
}

public static class RedisJsonEncoderHelper
{
    //public static T Decode<T>(this RedisJsonEncoder encoder, Packet pk) => (T)encoder.Decode(pk, typeof(T))!;

    public static T Decode<T>(this RedisJsonEncoder encoder, IPacket pk) => (T)encoder.Decode(pk, typeof(T))!;
}