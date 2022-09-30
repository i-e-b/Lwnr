namespace LwnrCore.Types;

/// <summary>
/// A do-it-all list/stack/queue.
/// This grows in fixed-size chunks, and does not need to re-allocate
/// </summary>
public class Vec
{
    private readonly Arena _memory;
    private readonly uint _elementSize;
    private readonly LinkedBlockList32 _chunkList;

    /// <summary>
    /// Create a new vector for elements of a fixed size
    /// </summary>
    public Vec(Arena memory, uint elementSize)
    {
        _memory = memory;
        _elementSize = elementSize;
        _chunkList = new LinkedBlockList32(memory);
    }
}