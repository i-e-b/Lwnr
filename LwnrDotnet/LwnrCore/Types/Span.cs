namespace LwnrCore.Types;

/// <summary>
/// Pointer to a chunk of an arena.
/// This is the primary access to memory.
/// </summary>
public class Span
{
    /// <summary> Which arena the memory is in </summary>
    public readonly Arena Arena;

    /// <summary> First byte of the data </summary>
    public readonly uint Start;

    /// <summary> Last byte of the data </summary>
    public readonly uint End;

    /// <summary>
    /// String describing the range of this span
    /// </summary>
    public override string ToString() => $"[{Start}-{End}| max={End-Start}]";

    /// <summary>
    /// New pointer
    /// </summary>
    public Span(Arena arena, uint start, uint end)
    {
        Arena = arena;
        Start = start;
        End = end;
    }

    private static readonly Arena _nothing = new() { IsNull = true };

    /// <summary>
    /// A pointer to nothing.
    /// </summary>
    public static Span Zero => new(_nothing, 0, 0);

    /// <summary>
    /// Returns true if this pointer has a range of zero.
    /// </summary>
    public bool IsZero => Start == 0 && End == 0;

    /// <summary>
    /// Number of bytes to store the span.
    /// Note that serialising the span loses
    /// its connection to the parent Arena
    /// </summary>
    public const uint ByteSize = sizeof(uint /* Start */)+sizeof(uint /* End */);

    /// <summary>
    /// Treat the memory as an array of 32-bit ints,
    /// and set the given value at the given index
    /// </summary>
    public void SetUInt32Idx(uint index, uint value)
    {
        var ptr = (int)(index*sizeof(uint) + Start);
        if (ptr > End) throw new Exception($"Index out of range: {ptr} of {Start}..{End}");
        Arena.Data[ptr++] = (byte)((value >> 24) & 0xff);
        Arena.Data[ptr++] = (byte)((value >> 16) & 0xff);
        Arena.Data[ptr++] = (byte)((value >>  8) & 0xff);
        Arena.Data[ptr  ] = (byte)((value >>  0) & 0xff);
    }

    /// <summary>
    /// Get the value at given index
    /// </summary>
    public uint GetUInt32Idx(uint index)
    {
        var ptr = (int)(index*sizeof(uint) + Start);
        if (ptr > End) throw new Exception($"Index out of range: {ptr} of {Start}..{End}");
        
        uint result = 0;
        result |= (uint)Arena.Data[ptr++] << 24;
        result |= (uint)Arena.Data[ptr++] << 16;
        result |= (uint)Arena.Data[ptr++] <<  8;
        result |= (uint)Arena.Data[ptr  ] <<  0;
        return result;
    }

    /// <summary>
    /// Get the number of bytes in this pointer
    /// </summary>
    public uint Size()
    {
        return End - Start + 1;
    }

    /// <summary>
    /// Return a new pointer that is a subset of this one
    /// </summary>
    public Span Subset(uint startOffset, uint length = 0)
    {
        var newStart = Start + startOffset;
        var newEnd = length < 1 ? End : (newStart+length-1);
        
        if (newStart > End || newEnd > End) throw new Exception($"Subset out of bounds. Requested {newStart}..{newEnd} of {Start}..{End}");
        
        return new Span(Arena, newStart, newEnd);
    }

    /// <summary>
    /// Read a range of bytes into an array
    /// </summary>
    /// <param name="start">byte index of start (inclusive)</param>
    /// <param name="end">byte index of end (exclusive)</param>
    /// <remarks>In the real system this would just be a cast with a range check</remarks>
    public byte[] ReadBytes(uint start, uint end)
    {
        var last = end - 1;
        if (start > last) throw new Exception($"Invalid range {start}..{end}");
        
        var realStart = (int)(start+Start);
        var realEnd = (int)(end+Start);
        
        if (realStart > End || realEnd > End) throw new Exception("start and end result in a range outside this span");

        var length = end - start;
        var output = new byte[length];
        
        for (int i = 0; i < length; i++)
        {
            output[i] = Arena.Data[realStart++];
        }

        return output;
    }

    /// <summary>
    /// Write a list of bytes over the memory at the given byte index
    /// </summary>
    public void Write(IEnumerable<byte> bytes, int targetIndex)
    {
        var input = bytes.ToArray();
        var start = targetIndex;
        var last = input.Length + start - 1;
        if (start > last) throw new Exception($"Invalid range {start}..{last}");

        var realStart = (int)(start+Start);
        var realEnd = (int)(last+Start);
        
        if (realStart > End || realEnd > End) throw new Exception("targetIndex and array length result in a range outside this span");
        
        var length = input.Length;
        for (int i = 0; i < length; i++)
        {
            Arena.Data[realStart++] = input[i];
        }
    }

    /// <summary>
    /// Set all memory values to zero
    /// </summary>
    public void ZeroAll()
    {
        for (uint i = Start; i <= End; i++)
        {
            Arena.Data[(int)i] = 0;
        }
    }
}