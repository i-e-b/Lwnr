namespace ClockHands;

/// <summary>
/// Buffer that pushes back all values when a new one is written.
/// Values can be queried by offset.
/// </summary>
/// <remarks>
/// This does a lot of data copying to simulate a 'bucket-brigade'
/// circuit, which would do the copying within a single clock cycle.
/// </remarks>
public class CircularOffsetBuffer<T>
{
    private readonly object _lock = new();
    private readonly T[] _data;

    /// <summary>
    /// Create a new buffer with the given fixed capacity.
    /// </summary>
    public CircularOffsetBuffer(int capacity)
    {
        _data = new T[capacity];
    }

    /// <summary>
    /// Push a new value into the buffer.
    /// The supplied value is immediately available at index zero
    /// All existing values increase index by one.
    /// </summary>
    public void Push(T value)
    {
        lock (_lock)
        {
            // Push other values back
            for (int i = _data.Length - 1; i > 0; i--) _data[i] = _data[i - 1];
            
            // Write new value
            _data[0] = value;
        }
    }

    /// <summary>
    /// Get a stored value by index
    /// </summary>
    public T this[int index] {
        get {
            lock (_lock)
            {
                return _data[index];
            }
        }
    }
}