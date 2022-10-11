using LwnrCore.Helpers;

namespace LwnrCore.Types;

/// <summary>
/// A growable zone of memory.
/// This is a very simple bump allocator,
/// and our system has no way to deallocate single
/// items, just ditching the whole scope.
/// <p></p>
/// Pointers in arenas should only point inside the same arena.
/// If memory is to cross arenas, that needs to be through an
/// alias (which holds the target arena)
/// </summary>
public class Arena
{
    /// <summary>
    /// The raw data
    /// </summary>
    public List<byte> Data { get; set; } = new();

    /// <summary>
    /// Returns true if the arena is invalid
    /// </summary>
    public bool IsNull { get; set; } = false;

    /// <summary>
    /// Allocate a number of bytes.
    /// This will go into the <see cref="Data"/> list
    /// as a bunch of zeros
    /// </summary>
    public Span Allocate(uint byteCount)
    {
        if (byteCount > int.MaxValue) throw new Exception("Not supported");
        var start = Data.Count;
        Data.AddRange(Enumerable.Repeat((byte)0, (int)byteCount));
        return new Span(this, (uint)start, byteCount);
    }

    /// <summary>
    /// Human readable summary
    /// </summary>
    public string Describe()
    {
        if (Data.Count <= 1024) return Bit.Describe("Data",Data);
        return $"Total = {Bit.Human(Data.Count)}; Top 512={Bit.Describe("Data",Data.Take(512))}";
    }
}