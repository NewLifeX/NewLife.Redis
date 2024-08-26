using System.Text;

namespace NewLife.Caching;

public ref struct SpanWriter(Span<Byte> buffer)
{
    private Span<Byte> _buffer = buffer;

    public Int32 Position { get; set; }

    public Span<Byte> GetSpan() => _buffer[..Position];

    public Span<Byte> GetRemain() => _buffer[Position..];

    public void Write(Int32 value)
    {
        BitConverter.TryWriteBytes(_buffer.Slice(Position), value);
        Position += sizeof(Int32);
    }

    public Int32 Write(String value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        //var byteCount = Encoding.UTF8.GetByteCount(value);

        var count = Encoding.UTF8.GetBytes(value, _buffer.Slice(Position));
        Position += count;

        return count;
    }

    public void WriteByte(Byte b) => _buffer[Position++] = b;

    public void Write(Byte[] value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        value.CopyTo(_buffer.Slice(Position));
        Position += value.Length;
    }

    public void Write(ReadOnlySpan<Byte> span)
    {
        span.CopyTo(_buffer.Slice(Position));
        Position += span.Length;
    }
}
