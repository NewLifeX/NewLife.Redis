using System.Buffers;

namespace NewLife.Caching;

public struct MemorySegment : IDisposable, IPacket
{
    #region 属性
    private readonly IMemoryOwner<Byte> _memoryOwner;
    private readonly Memory<Byte> _memory;
    private readonly Int32 _length;

    public Memory<Byte> Memory => _memoryOwner.Memory[.._length];

    public Int32 Length => _length;
    #endregion

    public MemorySegment(IMemoryOwner<Byte> memoryOwner, Int32 length)
    {
        if (length < 0 || length > memoryOwner.Memory.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");
        }

        _memoryOwner = memoryOwner;
        _length = length;
    }

    public MemorySegment(Byte[] buf, Int32 offset = 0, Int32 count = -1)
    {
        if (count < 0) count = buf.Length - offset;

        _memory = new Memory<Byte>(buf, offset, count);
        _length = count;
    }

    public Span<Byte> GetSpan()
    {
        if (_memoryOwner != null) return _memoryOwner.GetSpan()[.._length];

        return _memory.Span;
    }

    public void Dispose() => _memoryOwner?.Dispose();
}
