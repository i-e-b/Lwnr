namespace LwnrCore.Types;

/// <summary>
/// Pointer to a chunk of an arena
/// </summary>
public class ArenaPointer
{
    /// <summary> Which arena the memory is in </summary>
    public readonly Arena Arena;

    /// <summary> First byte of the data </summary>
    public readonly uint Start;

    /// <summary> Last byte of the data </summary>
    public readonly uint End;

    /// <summary>
    /// New pointer
    /// </summary>
    public ArenaPointer(Arena arena, uint start, uint end)
    {
        Arena = arena;
        Start = start;
        End = end;
    }

    private static readonly Arena _nothing = new() { IsNull = true };

    /// <summary>
    /// A pointer to nothing.
    /// </summary>
    public static ArenaPointer Null => new(_nothing, 0, 0);

    /// <summary>
    /// Returns true if this pointer is invalid. TODO: try to remove
    /// </summary>
    public bool IsNull => Start == End;

    /// <summary>
    /// Treat the memory as an array of 32-bit ints,
    /// and set the given value at the given index
    /// </summary>
    public void SetUInt32Idx(uint index, uint value)
    {
        var ptr = index + Start;
        if (ptr > End) throw new Exception($"Index out of range: {ptr} of {Start}..{End}");
        Arena.Data[(int)ptr] = value;
    }

    /// <summary>
    /// Return a new pointer that is a subset of this one
    /// </summary>
    public ArenaPointer Subset(uint startOffset)
    {
        return new ArenaPointer(Arena, Start + startOffset, End);
    }

    /// <summary>
    /// Get the value at given index
    /// </summary>
    public uint GetUInt32Idx(uint index)
    {
        var ptr = index + Start;
        if (ptr > End) throw new Exception($"Index out of range: {ptr} of {Start}..{End}");
        return Arena.Data[(int)ptr];
    }

    /// <summary>
    /// Get the number of bytes in this pointer
    /// </summary>
    public uint Size()
    {
        return (End - Start) + 1;
    }
}