namespace LwnrCore.Types;

/// <summary>
/// A super-basic container class, used to back more advanced interfaces.
/// <p></p>
/// This is a forward-only linked list, each item being a fixed size block.
/// The block size is a multiple of the size of a span, and access to data
/// is as span values. Items cannot be removed.
/// </summary>
public class SpanList
{
    private const uint BytesForData = BlockSize * Span.ByteSize;
    private const uint BytesForPointer = sizeof(uint);
    private const uint ChunkBytes = BytesForData + BytesForPointer;
    
    /// <summary>
    /// Number of spans in each block.
    /// Larger is more compute efficient, smaller is less memory use.
    /// </summary>
    public const int BlockSize = 16;
    
    private readonly Arena _memorySpace;
    /// <summary>
    /// Root chunk. Once allocated, this pointer should stay the same
    /// </summary>
    private Span _first;
    
    /// <summary>
    /// Cache of last chunk. This will start the same as _first,
    /// then move forward as chunks are allocated
    /// </summary>
    private Span _last;
    
    /// <summary>
    /// Chunk where the read/write head is
    /// </summary>
    private Span _headBlock;
    
    /// <summary>
    /// The global index of the read/write head
    /// </summary>
    private uint _headIndex;
    
    /// <summary>
    /// The offset within _headBlock where
    /// the _headIndex is, counted from zero
    /// in increments of the size of a span.
    /// </summary>
    private uint _headOffset;
    
    /// <summary>
    /// The block index of where
    /// the _headIndex is.
    /// </summary>
    private uint _headBlockNumber;
    
    /// <summary>
    /// Total number of index points in the list
    /// </summary>
    private uint _count;

    /// <summary>
    /// Create a linked list in an arena.
    /// The list will initially have no chunks allocated
    /// </summary>
    public SpanList(Arena memorySpace)
    {
        _memorySpace = memorySpace;
        _first = Span.Zero;
        _last = Span.Zero;
        _headBlock = Span.Zero;
        _headIndex = 0;
        _headOffset = 0;
        _headBlockNumber = 0;
        _count = 0;
    }

    /// <summary>
    /// Describe the span list
    /// </summary>
    public override string ToString()
    {
        return $"{_first}..{_last}; count={_count}; Head: i={_headIndex}, b={_headBlock}, o={_headOffset};";
    }

    /// <summary>
    /// Allocate a new chunk
    /// </summary>
    public void AddChunk()
    {
        // each chunk is one uint larger, to hold a 'next' header
        if (_first.IsZero) // initial chunk
        {
            _first = _memorySpace.Allocate(ChunkBytes);
            _last = _first;
            _first.ZeroAll();
            
            _count += BlockSize;
            
            _headBlock = _first;

            return;
        }
        
        // subsequent chunks
        if (_last.IsZero || _count < BlockSize) throw new Exception("Precondition fail");
        
        var newChunk = _memorySpace.Allocate(ChunkBytes);
        newChunk.ZeroAll();
        _last.SetUInt32Idx(0, newChunk.Start); // set 'next' pointer of old last to new last
        _count += BlockSize;
        _last = newChunk; // update last pointer
    }

    /// <summary>
    /// move read/write head to the offset requested.
    /// If the offset is not allocated, the function
    /// will return false and not accept reads or writes
    /// until a valid seek is made.
    /// </summary>
    public bool Seek(uint index)
    {
        if (index >= _count) return false;
        var expectedBlock = index / BlockSize;
        var expectedOffset = index % BlockSize;

        // Try a few optimised cases, then walk through the list
        if (expectedBlock == _headBlockNumber) // staying within the current block?
        {
            _headOffset = expectedOffset;
            _headIndex = index;
            return true;
        }
        
        if (index >= (_count - BlockSize)) // last block?
        {
            _headBlock = _last;
            _headBlockNumber = expectedBlock;
            _headOffset = expectedOffset;
            _headIndex = index;
            return true;
        }

        // walk from last head, or from start
        if (_headBlockNumber > expectedBlock)
        {
            _headBlock = _first;
            _headBlockNumber = 0;
        }

        // walk until we find it
        while (_headBlockNumber < expectedBlock)
        {
            var nextPtr = _headBlock.GetUInt32Idx(0);
            if (nextPtr == 0) break;
            _headBlock = new Span(_memorySpace, nextPtr, nextPtr + BytesForData);
            _headBlockNumber++;
        }

        if (_headBlockNumber != expectedBlock) return false; // not found
        
        _headOffset = expectedOffset;
        _headIndex = index;
        return true;
    }

    /// <summary>
    /// Try to read a value at the current read/write head,
    /// then advance the read/write head one position
    /// Return false if the head is not in a chunk
    /// </summary>
    public bool Read(out Span value)
    {
        value = Span.Zero;
        if (_headIndex >= _count) return false;
        if (! Seek(_headIndex)) return false;
        
        var location = (_headOffset * 2) + 1;
        var start = _headBlock.GetUInt32Idx(location);
        var end = _headBlock.GetUInt32Idx(location+1);
        value = new Span(_memorySpace, start, end);
        _headIndex++;
        return true;
    }

    /// <summary>
    /// Try to write a value at the current read/write head,
    /// then advance the read/write head one position
    /// Return false if the head is not in a chunk
    /// </summary>
    public bool Write(Span value)
    {
        if (_headIndex >= _count) return false;
        if (! Seek(_headIndex)) return false;
        
        var location = (_headOffset * 2) + 1;
        _headBlock.SetUInt32Idx(location, value.Start);
        _headBlock.SetUInt32Idx(location+1, value.End);
        _headIndex++;
        return true;
    }

    /// <summary>
    /// Return total number of data slots in the list.
    /// This is NOT a count of the number written.
    /// </summary>
    public uint Count()
    {
        return _count;
    }

    /// <summary>
    /// Return a new span list, removing the
    /// first 'count' blocks. The new list
    /// points to the same memory as the original.
    /// </summary>
    public SpanList Sublist(int count)
    {
        var start = 0;
        var newFirst = _first;

        while (start < count)
        {
            var nextPtr = newFirst.GetUInt32Idx(0);
            if (nextPtr == 0) // can't find it
            {
                throw new Exception("Sublist excluded entire list");
            }

            newFirst = new Span(_memorySpace, nextPtr, nextPtr + BytesForData);
            start++;
        }
        
        return new SpanList(_memorySpace){
            _count = (uint)(_count - (BlockSize * count)),
            _first = newFirst,
            _headBlock = newFirst,
            _headBlockNumber = 0,
            _headIndex = 0,
            _headOffset = 0,
            _last = _last
        };
    }
}