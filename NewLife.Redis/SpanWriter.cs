using System.Text;

namespace NewLife.Caching;

public ref struct SpanWriter(Span<Byte> buffer)
{
    private Span<Byte> _buffer = buffer;

    public Int32 Position { get; set; }

    public void Write(Int32 value)
    {
        BitConverter.TryWriteBytes(_buffer.Slice(Position), value);
        Position += sizeof(Int32);
    }

    public void Write(String value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var byteCount = Encoding.UTF8.GetByteCount(value);

        Encoding.UTF8.GetBytes(value, _buffer.Slice(Position));
        Position += byteCount;
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
