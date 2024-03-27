using System.Diagnostics.CodeAnalysis;
using LwnrCore.Helpers;

namespace LwnrCore.Containers;

/// <summary>
/// Robin-hood hash-map, that uses a "Fizzle Fade" algorithm for placement.
/// This means that every slot can be used for every value, but there
/// is still a lot of scattering.
/// <p/>
/// Each slot contains key,value,depth; where depth is how many steps
/// away from the first choice slot this value is.
/// <p/>
/// When we are storing a value, if we come to a slot where the value
/// has a depth lower than (ours - 1), we store our value here, and start
/// pushing the existing value down. We repeat this until the value goes
/// into an empty slot.
/// <p/>
/// This is experimental, and doesn't auto-scale yet.
/// </summary>
public class FizzleHashMap<TK, TV>
{
    private readonly Slot[] _slots;
    private readonly uint _sizeMask;
    private readonly uint _fizzMask;

    /// <summary>
    /// Total number of key/value pairs that can be stored
    /// </summary>
    public int Capacity { get; set; }
    
    /// <summary>
    /// Create a new empty FizzleHashMap with a fixed number of slots
    /// </summary>
    public FizzleHashMap(int fixedSize)
    {
        Capacity = (int)Bit.NextPowerOf2(fixedSize - 1);
        _sizeMask = (uint)(Capacity - 1);
        _slots = new Slot[Capacity];
        _fizzMask = GetMask(_sizeMask);
    }

    /// <summary>
    /// Try to add a key/value pair into the map.
    /// Keys in a map must be unique, so this will fail if trying to add an exising key
    /// </summary>
    public bool TryAdd(TK key, TV value)
    {
        if (key is null) return false;
        
        var initial = (uint)(key.GetHashCode() & _sizeMask);
        var idx = initial;
        var slot = _slots[idx];
        var depth = 0;

        while (slot.Key is not null && !slot.Key.Equals(key)) // until we find the key, or an empty slot
        {
            depth++;
            
            idx = NextIndex(idx);
            
            if (idx == initial) return false; // map is full
            if (depth > _slots.Length) return false; // safety check
            
            slot = _slots[idx];
            
            // TODO: check for depth and do robin-hood here
        }
        
        if (slot.Key?.Equals(key) == true) return false; // already in the map
        
        // Insert
        slot.Key = key;
        slot.Value = value;
        slot.Depth = depth;
        _slots[idx] = slot;
        Console.WriteLine($"Added {key}->{value}; depth={depth}; idx={idx}");
        return true;
    }

    /// <summary>
    /// Try to find a value for the given key.
    /// Returns false if key is not stored
    /// </summary>
    public bool TryGet(TK key, out TV? value)
    {
        value = default;
        if (key is null) return false;
        
        var initial = (uint)(key.GetHashCode() & _sizeMask);
        var idx = initial;
        var slot = _slots[idx];
        var depth = 0;

        while (slot.Key is not null && !slot.Key.Equals(key)) // until we find the key, or an empty slot
        {
            depth++;
            idx = NextIndex(idx);
            
            if (idx == initial) return false; // have cycled around the whole map
            if (depth > _slots.Length) return false; // safety check
            
            slot = _slots[idx];
            
            // TODO: we should be able to short-circuit based on depth.
            //if (depth < slot.Depth) return false; // check this
        }

        if (slot.Key?.Equals(key) != true) return false; // not found

        // Found it
        value = slot.Value;
        return true;
    }

    /// <summary>
    /// Try to remove a key/value pair
    /// </summary>
    public bool TryDelete(TK key)
    {
        throw new NotImplementedException(); // TODO: need to think about how the robin-hood affects this
    }

    /// <summary>
    /// Get next slot index based on current index
    /// </summary>
    private uint NextIndex(uint idx)
    {
        var next = NextMaskedValue(idx + 1, _fizzMask);
        while (next > _sizeMask) next = NextMaskedValue(next, _fizzMask);
        return next - 1;
    }
    
    /// <summary>
    /// Do the fizzle fade LSR
    /// </summary>
    /// <param name="v">Value. Must be 1 or greater</param>
    /// <param name="m">Mask, from <see cref="GetMask"/></param>
    private static uint NextMaskedValue(uint v, uint m) {
        v = ((v & 1) != 0) ? ((v >> 1) ^ m) : (v >> 1);

        // if (v == 1) ...
        // This will always end at 1, and then cycle through values again.
        // 'v' should never be zero, either input or output.
        return v;
    }

    /// <summary>
    /// Table of 'magic number' masks that drive the 'fizzle fade'.
    /// See https://jsfiddle.net/i_e_b/py6e08n7/  for a graphic demo of how this works.
    /// </summary>
    private static uint GetMask(long r) {
        if (r < 0x00000004) return 0x00000003;
        if (r < 0x00000008) return 0x00000006;
        if (r < 0x00000010) return 0x0000000C;
        if (r < 0x00000020) return 0x00000014;
        if (r < 0x00000040) return 0x00000030;
        if (r < 0x00000080) return 0x00000060;
        if (r < 0x00000100) return 0x000000B8;
        if (r < 0x00000200) return 0x00000110;
        if (r < 0x00000400) return 0x00000240;
        if (r < 0x00000800) return 0x00000500;
        if (r < 0x00001000) return 0x00000CA0;
        if (r < 0x00002000) return 0x00001B00;
        if (r < 0x00004000) return 0x00003500;
        if (r < 0x00008000) return 0x00006000;
        if (r < 0x00010000) return 0x0000B400;
        if (r < 0x00020000) return 0x00012000;
        if (r < 0x00040000) return 0x00020400;
        if (r < 0x00080000) return 0x00072000;
        if (r < 0x00100000) return 0x00090000;
        if (r < 0x00200000) return 0x00140000;
        if (r < 0x00400000) return 0x00300000;
        if (r < 0x00800000) return 0x00400000;
        if (r < 0x01000000) return 0x00D80000;
        if (r < 0x02000000) return 0x01200000;
        if (r < 0x04000000) return 0x03880000;
        if (r < 0x08000000) return 0x07200000;
        if (r < 0x10000000) return 0x09000000;
        if (r < 0x20000000) return 0x14000000;
        if (r < 0x40000000) return 0x32800000;
        if (r < 0x80000000) return 0x48000000;
        return 0xA3000000;
    }

    /// <summary>
    /// Layout of slot
    /// </summary>
    private struct Slot
    {
        public TK? Key;
        public TV? Value;
        public int Depth;
    }
}