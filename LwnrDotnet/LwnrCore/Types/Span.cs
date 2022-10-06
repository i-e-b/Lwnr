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
    /// Returns true if this pointer is invalid. TODO: try to remove
    /// </summary>
    public bool IsZero => Start == End;

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
        var ptr = index + Start;
        if (ptr > End) throw new Exception($"Index out of range: {ptr} of {Start}..{End}");
        Arena.Data[(int)ptr] = value;
    }

    /// <summary>
    /// Return a new pointer that is a subset of this one
    /// </summary>
    public Span Subset(uint startOffset)
    {
        return new Span(Arena, Start + startOffset, End);
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
        return (End - Start + 1) * 4;
    }

    /// <summary>
    /// Read a range of bytes into an array
    /// </summary>
    /// <param name="start">byte index of start (inclusive)</param>
    /// <param name="end">byte index of end (exclusive)</param>
    /// <remarks>In the real system this would just be a cast with a range check</remarks>
    public byte[] ReadBytes(int start, int end)
    {
        var last = end - 1;
        if (start > last) throw new Exception($"Invalid range {start}..{end}");

        var length = end - start;
        
        var startWord = start / 4;
        var startShift = start % 4;
        
        var endWord = last / 4;
        var endShift = last % 4;
        
        var output = new byte[length];

        var idx = 0;
        var word = Arena.Data[startWord];
        startShift = 3 - startShift;
        endShift = 3 - endShift;
        
        if (startWord == endWord) // special case -- no uint spans
        {
            if (startShift <= endShift) throw new Exception("Expectation failed. Check the logic");
            
            for (int i = startShift; i >= endShift; i--)
            {
                var b = i << 3;
                output[idx++] = (byte)((word >> b)&0xff);
            }
            return output;
        }
        
        
        // head
        for (int i = startShift; i >= 0; i--)
        {
            var b = i << 3;
            output[idx++] = (byte)((word >> b)&0xff);
        }
        
        // main
        for (int i = startWord + 1; i < endWord; i++)
        {
            word = Arena.Data[i];
            output[idx++] = (byte)((word >> 24) & 0xff);
            output[idx++] = (byte)((word >> 16) & 0xff);
            output[idx++] = (byte)((word >>  8) & 0xff);
            output[idx++] = (byte)((word >>  0) & 0xff);
        }
        
        // tail
        word = Arena.Data[endWord];
        for (int i = 3; i >= endShift; i--)
        {
            var b = i << 3;
            output[idx++] = (byte)((word >> b) & 0xff);
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

        var startWord = start / 4;
        var startShift = start % 4;
        
        var endWord = last / 4;
        var endShift = last % 4;
        
        var idx = 0;
        startShift = 3 - startShift;
        endShift = 3 - endShift;
        
        if (startWord == endWord) // special case -- no uint spans
        {
            if (startShift <= endShift) throw new Exception("Expectation failed. Check the logic");
            
            for (int i = startShift; i >= endShift; i--)
            {
                var b = i << 3;
                var val = Arena.Data[startWord];
                var mask = ~(0xffu << b);
                
                Arena.Data[startWord] = (val&mask) | ((uint)input[idx++] << b);
            }
            return;
        }
        
        // head
        for (int i = startShift; i >= 0; i--)
        {
            var b = i << 3;
            var val = Arena.Data[startWord];
            var mask = ~(0xffu << b);
                
            Arena.Data[startWord] = (val&mask) | ((uint)input[idx++] << b);
        }
        
        // main
        for (int i = startWord + 1; i < endWord; i++)
        {
            long word = 0;
            word |= (long)input[idx++] << 24;
            word |= (long)input[idx++] << 16;
            word |= (long)input[idx++] <<  8;
            word |= (long)input[idx++] <<  0;
            Arena.Data[i] = (uint)word;
        }
        
        // tail
        for (int i = 3; i >= endShift; i--)
        {
            var b = i << 3;
            var val = Arena.Data[endWord];
            var mask = ~(0xffu << b);
                
            Arena.Data[endWord] = (val&mask) | ((uint)input[idx++] << b);
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