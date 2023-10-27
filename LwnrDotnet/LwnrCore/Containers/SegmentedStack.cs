using System.Diagnostics.CodeAnalysis;

namespace LwnrCore.Containers;

/// <summary>
/// A stack of tagged types.
/// This exposes a stack of sub-segments which can be crossed
/// by references, but not by direct access.
/// </summary>
public class SegmentedStack
{
    /// <summary>
    /// Index into container of the first item pushed into the current segment
    /// </summary>
    private int _startOfCurrentSegment;
    
    /// <summary>
    /// Container for stack segments and data
    /// </summary>
    private readonly Vector<TaggedStackItem> _container;
    
    /// <summary>
    /// Create a new stack
    /// </summary>
    public SegmentedStack()
    {
        _container = new();
        _container.AddLast(new TaggedStackItem{Type = StackItemType.Bottom});
        _startOfCurrentSegment = _container.Length();
    }

    #region inside method call
    /// <summary>
    /// Try to access data by index from the start of the current segment.
    /// Returns true if the item was found, false if out of range or invalid.
    /// References will be resolved if possible.
    /// <p/>
    /// No items will be removed from the stack
    /// </summary>
    public bool PeekIndex(int index, [NotNullWhen(true)]out byte[]? data)
    {
        data = null;
        var actualIdx = _startOfCurrentSegment + index;
        if (actualIdx >= _container.Length()) return false;
        
        
        var item = _container[actualIdx];
        var ok = TryDeference(item, out var finalTarget);
        if (!ok || finalTarget is null) return false;
        
        data = finalTarget.Data;
        return true;
    }

    /// <summary>
    /// Push new data into the current segment.
    /// Returns the new entry's offset as an absolute stack position.
    /// </summary>
    public int PushData(byte[] data)
    {
        var idx = _container.Length();
        _container.AddLast(new TaggedStackItem{Type = StackItemType.Data, Data = data});
        return idx;
    }
    
    /// <summary>
    /// Remove a data element from the current segment.
    /// Returns true if an element was removed, false
    /// if there are no more elements in this segment.
    /// </summary>
    public bool PopData(out byte[]? data)
    {
        data = null;
        if (!_container.TryGetLast(out var item)) return false;

        switch (item.Type)
        {
            case StackItemType.Data:
            case StackItemType.Reference:
            {
                var ok = TryDeference(item, out var finalTarget);
                if (!ok || finalTarget is null) return false;
                data = finalTarget.Data;
                _container.RemoveLast();
                return true;
            }

            case StackItemType.Invalid:
            case StackItemType.StartOfSegment:
            case StackItemType.Bottom:
                return false;
            
            default: throw new Exception("Invalid state!");
        }
    }

    private bool TryDeference(TaggedStackItem? item, [NotNullWhen(true)]out TaggedStackItem? found)
    {
        found = null;

        while (item != null)
        {
            switch (item.Type)
            {
                case StackItemType.Data:
                {
                    found = item;
                    return true;
                }
                case StackItemType.Reference:
                {
                    var idx = item.ReferenceIdx;
                    if (idx < 0 || idx >= _container.Length()) return false; // invalid reference
                    item = _container[idx];
                    continue;
                }
                case StackItemType.Invalid:
                case StackItemType.StartOfSegment:
                case StackItemType.Bottom:
                    return false; // not something that should be in a reference chain
            
                default: throw new Exception("Invalid state!");
            }
        }
        
        return false;
    }
    #endregion inside method call

    #region between method calls
    
    /// <summary>
    /// Start a new stack segment.
    /// </summary>
    public void Call()
    {
        _container.AddLast(new TaggedStackItem{Type = StackItemType.StartOfSegment, ReferenceIdx = _startOfCurrentSegment});
        _startOfCurrentSegment = _container.Length();
    }


    /// <summary>
    /// Push a reference to existing data into the stack.
    /// Returns the new entry's offset as an absolute stack position.
    /// </summary>
    public int PushReference(int refIdx)
    {
        var idx = _container.Length();
        _container.AddLast(new TaggedStackItem{Type = StackItemType.Reference, ReferenceIdx = refIdx});
        return idx;
    }
    
    /// <summary>
    /// Push a deferred call into the stack.
    /// Returns the new entry's offset as an absolute stack position.
    /// </summary>
    public int PushDeferredCall(Action call)
    {
        var idx = _container.Length();
        _container.AddLast(new TaggedStackItem{Type = StackItemType.DeferredCall, DeferredCall = call});
        return idx;
    }
    
    /// <summary>
    /// Pop all items from the current segment, and the segment marker.
    /// Returns true if a segment was removed, false if we hit the stack bottom.
    /// <p/>
    /// Provides a list of deferred calls that might have been added during the function.
    /// </summary>
    public bool Return(out List<Action> deferredCalls)
    {
        deferredCalls = new List<Action>();
        while (_container.TryGetLast(out var item))
        {
            switch (item.Type)
            {
                case StackItemType.Data:
                case StackItemType.Reference:
                {
                    _container.RemoveLast();
                    break;
                }

                case StackItemType.StartOfSegment:
                {
                    _container.RemoveLast(); // pop the start-of-segment
                    
                    // re-establish start-of-segment index
                    _startOfCurrentSegment = item.ReferenceIdx;
                    return true;
                }

                case StackItemType.DeferredCall:
                {
                    if (item.DeferredCall is not null) deferredCalls.Add(item.DeferredCall);
                    break;
                }

                case StackItemType.Invalid: // stack segment is invalid
                case StackItemType.Bottom: // this is not a call segment
                    return false;
                
                default: throw new Exception("Invalid state!");
            }
        }
        return false;
    }
    #endregion between method calls

    /// <summary>
    /// Item in the stack
    /// </summary>
    private class TaggedStackItem
    {
        public StackItemType Type { get; set; } = StackItemType.Invalid;

        /// <summary>
        /// <ul>
        /// <li>If type is <see cref="StackItemType.Reference"/>, this is the index of the target</li>
        /// <li>If type is <see cref="StackItemType.StartOfSegment"/> this is the <see cref="_startOfCurrentSegment"/> value when the segment was started</li>
        /// <li>Otherwise, this is unused</li>
        /// </ul>
        /// </summary>
        public int ReferenceIdx { get; set; } = -1;
        
        /// <summary>
        /// If type is <see cref="StackItemType.Data"/>, this is the element
        /// data (implicit with size). Otherwise, unused.
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// If type is <see cref="StackItemType.DeferredCall"/>, this is the action to run
        /// </summary>
        public Action? DeferredCall { get; set; }
    }

    /// <summary>
    /// Entry type of a stack item
    /// </summary>
    private enum StackItemType
    {
        /// <summary>
        /// An invalid stack item.
        /// </summary>
        Invalid = 0,
        
        /// <summary>
        /// Start of a segment. Must 'return' to pass this
        /// </summary>
        StartOfSegment = 1,
        
        /// <summary>
        /// Root entry of the stack. Must 'exit' to pass this
        /// </summary>
        Bottom = 2,
        
        /// <summary>
        /// Normal stack item storing data
        /// </summary>
        Data = 3,
        
        /// <summary>
        /// Reference to another stack item. This can cross 'StartOfStack' entries.
        /// </summary>
        Reference = 4,
        
        /// <summary>
        /// A call that should be made when the current segment returns
        /// </summary>
        DeferredCall = 5,
    }
}
