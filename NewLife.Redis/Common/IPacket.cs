
using System.Text;

namespace NewLife.Caching;

public interface IPacket : IDisposable
{
    Int32 Length { get; }

    Memory<Byte> Memory { get; }

    Span<Byte> GetSpan();
}

public static class SpanHelper
{
    public static String ToStr(this ReadOnlySpan<Byte> span, Encoding? encoding = null) => (encoding ?? Encoding.UTF8).GetString(span);

    public static String ToStr(this Span<Byte> span, Encoding? encoding = null) => (encoding ?? Encoding.UTF8).GetString(span);

    /// <summary>转字符串并释放</summary>
    /// <param name="pk"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static String ToStr(this IPacket pk, Encoding? encoding = null)
    {
        var rs = pk.GetSpan().ToStr(encoding);
        pk.Dispose();
        return rs;
    }
}