﻿namespace LwnrCore.Types;

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
    public List<UInt32> Data { get; set; } = new();

    /// <summary>
    /// Returns true if the arena is invalid
    /// </summary>
    public bool IsNull { get; set; } = false;

    /// <summary>
    /// Allocate a number of bytes.
    /// This will go into the <see cref="Data"/> list
    /// as a bunch of zeros
    /// </summary>
    public ArenaPointer Allocate(uint byteCount)
    {
        if (byteCount > int.MaxValue) throw new Exception("Not supported");
        var start = Data.Count;
        byteCount += byteCount % 4;
        Data.AddRange(Enumerable.Repeat((UInt32)0, (int)(byteCount / 4)));
        var end = Data.Count - 1;
        return new ArenaPointer(this, (uint)start, (uint)end);
    }
}