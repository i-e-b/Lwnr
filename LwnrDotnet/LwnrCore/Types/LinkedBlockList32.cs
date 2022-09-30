namespace LwnrCore.Types;

/// <summary>
/// A super-basic container class, used to back more advanced interfaces.
/// <p></p>
/// This is a forward-only linked list, each item being a fixed size block.
/// The block size is a multiple of 4 bytes, and access to the data is as
/// int32 values. Items cannot be removed.
/// </summary>
public class LinkedBlockList32
{
    /// <summary>
    /// number of uints in each block
    /// </summary>
    public const int BlockSize = 4;
    
    private readonly Arena _memorySpace;
    /// <summary>
    /// Root chunk. Once allocated, this pointer should stay the same
    /// </summary>
    private ArenaPointer _first;
    
    /// <summary>
    /// Cache of last chunk. This will start the same as _first,
    /// then move forward as chunks are allocated
    /// </summary>
    private ArenaPointer _last;
    
    /// <summary>
    /// Chunk where the read/write head is
    /// </summary>
    private ArenaPointer _headBlock;
    
    /// <summary>
    /// The global index of the read/write head
    /// </summary>
    private uint _headIndex;
    
    /// <summary>
    /// The offset within _headBlock where
    /// the _headIndex is.
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
    public LinkedBlockList32(Arena memorySpace)
    {
        _memorySpace = memorySpace;
        _first = ArenaPointer.Null;
        _last = ArenaPointer.Null;
        _headBlock = ArenaPointer.Null;
        _headIndex = 0;
        _headOffset = 0;
        _headBlockNumber = 0;
        _count = 0;
    }

    /// <summary>
    /// Allocate a new chunk, returning a pointer to it
    /// </summary>
    public void AddChunk()
    {
        // each chunk is one uint larger, to hold a 'next' header

        if (_first.IsNull) // initial chunk
        {
            _first = _memorySpace.Allocate((BlockSize + 1) * sizeof(uint));
            _last = _first;
            _first.SetUInt32Idx(0, 0);
            
            _count += BlockSize;
            
            _headBlock = _first;

            return;
        }
        
        // subsequent chunks
        if (_last.IsNull || _count < BlockSize) throw new Exception("Precondition fail");
        
        var newChunk = _memorySpace.Allocate((BlockSize + 1) * sizeof(uint));
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
            _headBlock = new ArenaPointer(_memorySpace, nextPtr, nextPtr+BlockSize);
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
    public bool Read(out uint value)
    {
        value = 0;
        if (_headIndex >= _count) return false;
        if (! Seek(_headIndex)) return false;
        
        value = _headBlock.GetUInt32Idx(_headOffset+1);
        _headIndex++;
        return true;
    }

    /// <summary>
    /// Try to write a value at the current read/write head,
    /// then advance the read/write head one position
    /// Return false if the head is not in a chunk
    /// </summary>
    public bool Write(uint value)
    {
        if (_headIndex >= _count) return false;
        if (! Seek(_headIndex)) return false;
        
        _headBlock.SetUInt32Idx(_headOffset+1, value);
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
}