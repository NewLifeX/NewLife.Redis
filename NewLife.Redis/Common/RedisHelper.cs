using System.Buffers;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Caching;

/// <summary>Redis助手</summary>
public static class RedisHelper
{
    #region 注入TraceId
    /// <summary>在消息队列发布消息前</summary>
    /// <param name="redis"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    internal static Object AttachTraceId(this Redis redis, Object msg)
    {
        // 消息为空或者特殊类型，不接受注入
        if (msg == null || msg is Byte[] || msg is Packet) return msg;

        // 字符串或复杂类型以外的消息，不接受注入
        var code = Type.GetTypeCode(msg.GetType());
        if (code != TypeCode.String && code != TypeCode.Object) return msg;

        // 注入参数名
        var name = redis?.Tracer?.AttachParameter;
        if (name.IsNullOrEmpty()) return msg;

        // 当前埋点追踪片段，正在准备采样
        var span = DefaultSpan.Current as DefaultSpan;
        if (span == null || span.TraceFlag == 0) return msg;

        // 注入Json尾部
        if (msg is String str)
        {
            if (str[0] == '{' && str[str.Length - 1] == '}')
                if (str.IndexOf($"\"{name}\":", StringComparison.OrdinalIgnoreCase) < 0)
                    return str.Substring(0, str.Length - 1) + $",\"{name}\":\"{span}\"}}";
                else if (str.EndsWithIgnoreCase($",\"{name}\":null}}"))
                    return str.Substring(0, str.Length - $",\"{name}\":null}}".Length) + $",\"{name}\":\"{span}\"}}";

            return msg;
        }
        // 注入字典
        else if (msg is IDictionary<String, Object> dic)
        {
            if (!dic.TryGetValue(name, out var val) || val == null) dic[name] = span.ToString();
            return dic;
        }
        // 注入复合对象
        else if (code == TypeCode.Object)
        {
            dic = msg.ToDictionary();
            if (!dic.TryGetValue(name, out var val) || val == null) dic[name] = span.ToString();
            return dic;
        }

        return msg;
    }
    #endregion

    /// <summary>获取Span</summary>
    /// <param name="owner"></param>
    /// <returns></returns>
    public static Span<T> GetSpan<T>(this IMemoryOwner<T> owner)
    {
        if (owner is MemoryManager<T> manager)
            return manager.GetSpan();

        return owner.Memory.Span;
    }

    internal static Int32 Read(this Stream stream, Span<Byte> buffer)
    {
        var array = ArrayPool<Byte>.Shared.Rent(buffer.Length);
        try
        {
            var num = stream.Read(array, 0, buffer.Length);
            if ((UInt32)num > (UInt32)buffer.Length)
                throw new IOException("IO_StreamTooLong");

            new ReadOnlySpan<Byte>(array, 0, num).CopyTo(buffer);
            return num;
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(array);
        }
    }
}