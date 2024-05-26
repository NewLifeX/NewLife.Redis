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

    static RedisJsonEncoder()
    {
        // 尝试使用System.Text.Json，不支持时使用FastJson
        IJsonHost? host = null;
        try
        {
            var type = $"{typeof(FastJson).Namespace}.SystemJson".GetTypeEx();
            if (type != null)
            {
                host = type.CreateInstance() as IJsonHost;
            }
        }
        catch { }

        _host = host ?? JsonHelper.Default;
    }

    /// <summary>数值转数据包</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Packet Encode(Object value)
    {
        if (value == null) return new Byte[0];

        if (value is Packet pk) return pk;
        if (value is Byte[] buf) return buf;
        if (value is IAccessor acc) return acc.ToPacket();

        var type = value.GetType();
        return (type.GetTypeCode()) switch
        {
            TypeCode.Object => JsonHost.Write(value).GetBytes(),
            TypeCode.String => (value as String).GetBytes(),
            TypeCode.DateTime => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff").GetBytes(),
            _ => (value + "").GetBytes(),
        };
    }

    /// <summary>数据包转对象</summary>
    /// <param name="pk"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public virtual Object? Decode(Packet pk, Type type)
    {
        //if (pk == null) return null;

        try
        {
            if (type == typeof(Packet)) return pk;
            if (type == typeof(Byte[])) return pk.ReadBytes();
            if (type.As<IAccessor>()) return type.AccessorRead(pk);

            // 支持可空类型，遇到无数据时返回null
            var ntype = Nullable.GetUnderlyingType(type);
            if (pk.Total == 0 && ntype != null && ntype != type) return null;
            if (ntype != null) type = ntype;

            //var str = pk.ToStr().Trim('\"');
            var str = pk.ToStr();
            if (type.GetTypeCode() == TypeCode.String) return str;

            //if (type.GetTypeCode() != TypeCode.Object) return str.ChangeType(type);
            if (type.GetTypeCode() != TypeCode.Object)
            {
                if (type == typeof(Boolean) && str == "OK") return true;

                //return Convert.ChangeType(str, type);
                return str.ChangeType(type);
            }

            //return str.ToJsonEntity(type);
            return JsonHost.Read(str, type);
        }
        catch
        {
            if (ThrowOnError) throw;

            return null;
        }
    }
}