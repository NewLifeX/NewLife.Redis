using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace NewLife.Caching;

/// <summary>Span写入器</summary>
/// <param name="buffer"></param>
public ref struct SpanWriter(Span<Byte> buffer)
{
    #region 属性
    private readonly Span<Byte> _buffer = buffer;

    private Int32 _index;
    /// <summary>已写入字节数</summary>
    public Int32 WrittenCount => _index;

    /// <summary>总容量</summary>
    public Int32 Capacity => _buffer.Length;

    /// <summary>空闲容量</summary>
    public Int32 FreeCapacity => _buffer.Length - _index;

    /// <summary>是否小端字节序。默认true</summary>
    public Boolean IsLittleEndian { get; set; } = true;
    #endregion

    #region 基础方法
    /// <summary>告知有多少数据已写入缓冲区</summary>
    /// <param name="count"></param>
    public void Advance(Int32 count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (_index > _buffer.Length - count) throw new ArgumentOutOfRangeException(nameof(count));

        _index += count;
    }

    //public Memory<Byte> GetMemory(Int32 sizeHint = 0)
    //{
    //    if (sizeHint > FreeCapacity) throw new ArgumentOutOfRangeException(nameof(sizeHint));

    //    return _buffer[.._index];
    //}

    /// <summary>返回要写入到的Span，其大小按 sizeHint 参数指定至少为所请求的大小</summary>
    /// <param name="sizeHint"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Span<Byte> GetSpan(Int32 sizeHint = 0)
    {
        if (sizeHint > FreeCapacity) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        return _buffer[.._index];
    }

    //public Span<Byte> GetRemain() => _buffer[_index..];
    #endregion

    #region 写入方法
    /// <summary>确保缓冲区中有足够的空间。</summary>
    /// <param name="size">需要的字节数。</param>
    private void EnsureSpace(Int32 size)
    {
        if (_index + size > _buffer.Length)
        {
            throw new InvalidOperationException("缓冲区空间不足。");
        }
    }

    /// <summary>写入字节。</summary>
    /// <param name="value">要写入的字节值。</param>
    public Int32 Write(Byte value)
    {
        var size = sizeof(Byte);
        EnsureSpace(size);
        _buffer[_index] = value;
        _index += size;
        return size;
    }

    /// <summary>写入 32 位整数。</summary>
    /// <param name="value">要写入的整数值。</param>
    public Int32 Write(Int32 value)
    {
        var size = sizeof(Int32);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(_index), value);
        else
            BinaryPrimitives.WriteInt32BigEndian(_buffer.Slice(_index), value);
        _index += size;
        return size;
    }

    /// <summary>写入无符号 32 位整数。</summary>
    /// <param name="value">要写入的无符号整数值。</param>
    public Int32 Write(UInt32 value)
    {
        var size = sizeof(UInt32);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(_index), value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(_buffer.Slice(_index), value);
        _index += size;
        return size;
    }

    /// <summary>写入 64 位整数。</summary>
    /// <param name="value">要写入的整数值。</param>
    public Int32 Write(Int64 value)
    {
        var size = sizeof(Int64);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.Slice(_index), value);
        else
            BinaryPrimitives.WriteInt64BigEndian(_buffer.Slice(_index), value);
        _index += size;
        return size;
    }

    /// <summary>写入无符号 64 位整数。</summary>
    /// <param name="value">要写入的无符号整数值。</param>
    public Int32 Write(UInt64 value)
    {
        var size = sizeof(UInt64);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.Slice(_index), value);
        else
            BinaryPrimitives.WriteUInt64BigEndian(_buffer.Slice(_index), value);
        _index += size;
        return size;
    }

    /// <summary>写入单精度浮点数。</summary>
    /// <param name="value">要写入的浮点值。</param>
    public unsafe Int32 Write(Single value)
    {
#if NETSTANDARD2_1_OR_GREATER
        return Write(BitConverter.SingleToInt32Bits(value));
#else
        return Write(*(Int32*)(&value));
#endif
    }

    /// <summary>写入双精度浮点数。</summary>
    /// <param name="value">要写入的浮点值。</param>
    public unsafe Int32 Write(Double value)
    {
#if NETSTANDARD2_1_OR_GREATER
        return Write(BitConverter.DoubleToInt64Bits(value));
#else
        return Write(*(Int64*)(&value));
#endif
    }

    /// <summary>写入字符串</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Int32 Write(String value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        var count = Encoding.UTF8.GetBytes(value.AsSpan(), _buffer.Slice(_index));
        _index += count;

        return count;
    }


    /// <summary>写入字节数组</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Int32 Write(Byte[] value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        value.CopyTo(_buffer.Slice(_index));
        _index += value.Length;

        return value.Length;
    }

    /// <summary>写入Span</summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public Int32 Write(ReadOnlySpan<Byte> span)
    {
        span.CopyTo(_buffer.Slice(_index));
        _index += span.Length;

        return span.Length;
    }
    #endregion
}

static class SpanHelper
{
    public static unsafe Int32 GetBytes(this Encoding encoding, ReadOnlySpan<Char> chars, Span<Byte> bytes)
    {
        fixed (Char* chars2 = &MemoryMarshal.GetReference(chars))
        {
            fixed (Byte* bytes2 = &MemoryMarshal.GetReference(bytes))
            {
                return encoding.GetBytes(chars2, chars.Length, bytes2, bytes.Length);
            }
        }
    }

    public static unsafe String GetString(this Encoding encoding, ReadOnlySpan<Byte> bytes)
    {
        if (bytes.IsEmpty) return String.Empty;

#if NET45
        return encoding.GetString(bytes.ToArray());
#else
        fixed (Byte* bytes2 = &MemoryMarshal.GetReference(bytes))
        {
            return encoding.GetString(bytes2, bytes.Length);
        }
#endif
    }

    public static Task WriteAsync(this Stream stream, ReadOnlyMemory<Byte> buffer, CancellationToken cancellationToken = default)
    {
        if (MemoryMarshal.TryGetArray(buffer, out var segment))
            return stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);

        var array = ArrayPool<Byte>.Shared.Rent(buffer.Length);
        buffer.Span.CopyTo(array);

        var writeTask = stream.WriteAsync(array, 0, buffer.Length, cancellationToken);
        return Task.Run(async () =>
        {
            try
            {
                await writeTask.ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<Byte>.Shared.Return(array);
            }
        });
    }
}