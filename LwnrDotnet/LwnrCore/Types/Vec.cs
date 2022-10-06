namespace LwnrCore.Types;

/// <summary>
/// A do-it-all list/stack/queue.
/// This grows in fixed-size chunks, and will re-use any blocks it doesn't
/// need anymore (so it can do rolling queue/dequeue in fixed memory)
/// </summary>
public class Vec
{
    /*
     
     We have '_memory', which is all available space.
     
     Out of this, we allocate chunks (which we will always know the size of)
     These chunks we keep in a '_chunkList'. When indexing, we can guess the
     chunk we should be in, and query the chunk list to find it.
     
     Chunk list [ 0 1 2 3  |  4 5 6 7  |  ... ]
                  ↓
     Chunks:      { slot 0, slot 1 ... slot _elemsPerBlock-1 }
                      ↓
     Slots:           { byte 0 ... byte _elementSize-1}
     
     */
    // Fixed stuff
    private readonly Arena _memory;
    private readonly SpanList _chunkList; // list of active chunks. These MUST be kept in sequence order
    private readonly SpanList _deadList; // list of spare chunks
    
    /// <summary> how many bytes per item </summary>
    private readonly uint _elementSize;
    /// <summary> how many bytes per block </summary>
    private readonly uint _blockSize;
    /// <summary> how many items per block </summary>
    private readonly uint _elemsPerBlock;
    
    // state
    private uint _elementCount; // how many elements are stored
    private uint _baseOffset; // how many elements to skip from first block (for dequeuing)
    
    private uint _chunkCount; // how many blocks are in the chunkList
    private uint _deadCount; // how many blocks are in the dead list

    /// <summary>
    /// Create a new vector for elements of a fixed size (in bytes)
    /// </summary>
    public Vec(Arena memory, uint elementSize)
    {
        _memory = memory;
        _elementSize = elementSize;
        _elemsPerBlock = GuessBlockSlotCount(elementSize);
        _blockSize = _elemsPerBlock * _elementSize;
        _chunkList = new SpanList(memory);
        _deadList = new SpanList(memory);
        
        _elementCount = 0;
        _baseOffset = 0;
        _deadCount = 0;
    }

    /// <summary>
    /// Add a new element to the end of the vector
    /// </summary>
    public void Push(byte[] value)
    {
        if (value.Length != _elementSize) throw new Exception("Incorrect size of element given to Vec.Push");
        
        // - If we have no chunks, create the first
        // - If there are no non-full slots in the last chunk, create a new one
        // - Copy the data to the first free slot in the last chunk
        
        
        var free = FirstFreeSlot();
        if (free.IsZero)
        {
            AppendBlock(); // could write to idx=0 here rather than searching again.
            free = FirstFreeSlot();
        }
        
        if (free.IsZero) throw new Exception("Failed to find a free slot");

        free.Write(value, 0);
        _elementCount++;

    }

    /// <summary>
    /// Remove the last element in the vector, returning its value.
    /// Returns false if the vector is empty
    /// </summary>
    public bool Pop(out byte[] value)
    {
        value = Array.Empty<byte>();
        return false;
    }

    /// <summary>
    /// Return a span from the start to end of the first free slot at the end of the vector.
    /// Returns an empty span if no space is available
    /// </summary>
    private Span FirstFreeSlot()
    {
        if (_chunkCount < 1) return Span.Zero; // nothing allocated yet
        if (_chunkCount >= _chunkList.Count()) return Span.Zero; // all chunks full
        
        var nextElementIndex = _baseOffset + _elementCount;    // global index of where the next lot should be
        var lastAllocatedIndex = _chunkCount * _elemsPerBlock; // global index of the end of allocated memory
        
        if (nextElementIndex > lastAllocatedIndex) return Span.Zero; // next slot is off the end of memory
        
        var whichChunk = nextElementIndex / _elemsPerBlock; // Where in the chunk list?
        var slotInChunk = nextElementIndex % _elemsPerBlock;
        var ok = _chunkList.Seek(whichChunk);
        if (!ok) throw new Exception($"Failed to seek chunk {whichChunk} in list {_chunkList}");
        
        ok = _chunkList.Read(out var theChunk); // this is a span which is _elementSize
        if (!ok) throw new Exception($"Failed to read chunk {whichChunk} in list {_chunkList}");
        
        if (theChunk.IsZero) // need to allocate the data for its slots
        {
            theChunk = _memory.Allocate(_elementSize);
            _chunkList.Seek(whichChunk);
            _chunkList.Write(theChunk);
        }

        var offset = slotInChunk * _elementSize;
        return theChunk.Subset(offset, _elementSize);
    }

    /// <summary>
    /// Add a new block to our vector
    /// </summary>
    /// <returns></returns>
    private Span AppendBlock()
    {
        // If we have any dead blocks, reclaim one
        if (_deadCount > 0)
        {
            throw new NotImplementedException("Append block recovery");
        }
        
        // add new block, add to chain.
        _chunkList.AddChunk();
        _chunkCount++;
        
        return Span.Zero;
    }

    /// <summary>
    /// Remove the first block in the vector
    /// </summary>
    private void DropBlock()
    {
        // TODO: move onto dead list
        _deadCount++;
        
        // TODO: shuffle all the values down one to keep in order
    }

    /// <summary>
    /// For some mix of efficiency and speed, we store
    /// elements in blocks. The smaller the elements,
    /// the more will be in each block.
    /// </summary>
    private uint GuessBlockSlotCount(uint elementSize)
    {
        if (elementSize == 0) throw new Exception("Invalid element size for vector");
        
        if (elementSize > 512) return elementSize; // one element per block for large things
        var count = 1024 / elementSize;
        return count;
    }

    /// <summary>
    /// Return number of elements currently stored in this vector
    /// </summary>
    public uint Count() => _elementCount;
}