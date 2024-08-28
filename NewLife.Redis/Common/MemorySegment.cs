using System.Buffers;

namespace NewLife.Caching;

public struct MemorySegment<T> : IDisposable
{
    #region 属性
    private readonly IMemoryOwner<T> _memoryOwner;
    private readonly Int32 _length;

    public Memory<T> Memory => _memoryOwner.Memory[.._length];

    public Int32 Length => _length;
    #endregion

    public MemorySegment(IMemoryOwner<T> memoryOwner, Int32 length)
    {
        if (length < 0 || length > memoryOwner.Memory.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");
        }

        _memoryOwner = memoryOwner;
        _length = length;
    }

    public Span<T> GetSpan() => _memoryOwner.GetSpan()[.._length];

    public void Dispose() => _memoryOwner.Dispose();
}
