namespace LwnrCore.Containers;

/// <summary>
/// `Vector` is a generic, auto-sizing, double-ended queue,
/// and attempts to replicate JavaScript array semantics.
/// <ul>
/// <li>Insertion: Items can be inserted at either end, but not at arbitrary indexes</li>
/// <li>Removal: Items can be removed from either end, and at arbitrary indexes. Deleting from the middle is a relatively slow operation</li>
/// <li>Read: Items can be read from either end, and from any index</li>
/// <li>Update: Items can be modified at either end, and from any index</li>
/// </ul>
/// </summary>
public class Vector<T>
{
    
    /**
     * The array in which the elements of the deque are stored.
     * The capacity of the deque is the length of this array, which is
     * always a power of two. The array is never allowed to become
     * full, except transiently within an addX method where it is
     * resized (see doubleCapacity) immediately upon becoming full,
     * thus avoiding head and tail wrapping around to equal each
     * other.  We also guarantee that all array cells not holding
     * deque elements are always null.
     */
    private T[] _elements;

    /**
     * The index of the element at the head of the deque (which is the
     * element that would be removed by remove() or pop()); or an
     * arbitrary number equal to tail if the deque is empty.
     */
    private volatile int _head;

    /**
     * The index at which the next element would be added to the tail
     * of the deque (via addLast(E), add(E), or push(E)).
     */
    private volatile  int _tail;

    /**
     * The minimum capacity that we'll use for a newly created deque.
     * Must be a power of 2.
     */
    private const uint MinInitialCapacity = 8;
    
    /// <summary>
    /// Maximum capacity for a queue. Must be a power of 2.
    /// </summary>
    private const int MaxCapacity = 0x4000_0000;

    // ******  Array allocation and resizing utilities ******

    /**
     * Allocates empty array to hold the given number of elements.
     *
     * @param numElements the number of elements to hold
     */
    private void AllocateElements(int numElements) {
        if (numElements <= 0) throw new Exception("Invalid element count");
        if (numElements > MaxCapacity) throw new Exception("Invalid element count");
        
        var initialCapacity = MinInitialCapacity;
       
        // Find the best power of two to hold elements.
        // Tests "<=" because arrays aren't kept full.
        if (numElements >= initialCapacity) {
            initialCapacity = (uint)numElements;
            initialCapacity |= (initialCapacity >> 1);
            initialCapacity |= (initialCapacity >> 2);
            initialCapacity |= (initialCapacity >> 4);
            initialCapacity |= (initialCapacity >> 8);
            initialCapacity |= (initialCapacity >> 16);
            initialCapacity++;
        }
        _elements = new T[initialCapacity];
    }

    /**
     * Doubles the capacity of this deque.  Call only when full, i.e.,
     * when head and tail have wrapped around to become equal.
     */
    private void DoubleCapacity() {
        if (_head != _tail) throw new Exception("Unexpected state (internal)");
        var p = _head;
        var elementsLength = _elements.Length;
        var r = elementsLength - p; // number of elements to the right of p
        var newCapacity = elementsLength << 1;
        if (newCapacity < 0)
            throw new Exception("Vector capacity exceeded");
        
        // Create new array, and copy elements over
        var newArray = new T[newCapacity];
        
        Array.Copy(_elements, p, newArray, 0, r);
        Array.Copy(_elements, 0, newArray, r, p);
        
        // null out old array (helps with GC)
        Array.Fill(_elements, default);
        
        _elements = newArray;
        _head = 0;
        _tail = elementsLength;
    }

    /// <summary>
    /// Return a new vector containing a single element
    /// </summary>
    public static Vector<TV> FromValue<TV>(TV v){
        var result = new Vector<TV>();
        result.AddLast(v);
        return result;
    }

    /**
     * Constructs an empty array deque
     */
    public Vector() {
        _elements = new T[8];
    }

    /**
     * Constructs an empty array deque with an initial capacity
     * sufficient to hold the specified number of elements.
     *
     * @param numElements lower bound on initial capacity of the deque
     */
    public Vector(int numElements) {
        _elements = Array.Empty<T>();
        AllocateElements(numElements);
    }

    /**
     * Constructs a deque containing the elements of the specified
     * collection, in the order they are returned by the collection's
     * iterator.  (The first element returned by the collection's
     * iterator becomes the first element, or <i>front</i> of the
     * deque.)
     *
     * @param c the collection whose elements are to be placed into the deque
     * @throws NullPointerException if the specified collection is null
     */
    public Vector(T[] c) {
        _elements = Array.Empty<T>();
        AllocateElements(c.Length);
        foreach (var v in c) AddLast(v);
    }

    /**
     * Create a copy of 'other'. No data is shared.
     */
    public Vector(Vector<T> other){
        _elements = new T[other._elements.Length];
        this._head = other._head;
        this._tail = other._tail;
        Array.Copy(other._elements, 0, this._elements, 0, _elements.Length);
    }

    // The main insertion and extraction methods are addFirst,
    // addLast, pollFirst, pollLast. The other methods are defined in
    // terms of these.

    /**
     * Inserts the specified element at the front of this deque.
     *
     * @param e the element to add
     * @throws NullPointerException if the specified element is null
     */
    public void AddFirst(T e) {
        _elements[_head = (_head - 1) & (_elements.Length - 1)] = e;
        if (_head == _tail) DoubleCapacity();
    }

    /**
     * Inserts the specified element at the end of this deque.
     */
    public void AddLast(T e) {
        _elements[_tail] = e;
        if ( (_tail = (_tail + 1) & (_elements.Length - 1)) == _head)
            DoubleCapacity();
    }

    /**
     * @throws NoSuchElementException {@inheritDoc}
     */
    public T RemoveFirst() {
        if (_head == _tail) throw new Exception("The vector is empty");
        return PollFirst();
    }

    /**
     * @throws NoSuchElementException {@inheritDoc}
     */
    public T RemoveLast() {
        if (_head == _tail) throw new Exception("The vector is empty");
        return PollLast();
    }

    private T PollFirst() {
        var h = _head;
        var result = _elements[h];
        // Element is null if deque empty
        if (result != null) {
            _elements[h] = default!;
            _head = (h + 1) & (_elements.Length - 1);
        }
        return result;
    }

    private T PollLast() {
        var t = (_tail - 1) & (_elements.Length - 1);
        var result = _elements[t];
        if (result != null) {
            _elements[t] = default!;
            _tail = t;
        }
        return result;
    }

    /**
     * Read but don't remove first item
     * @throws NoSuchElementException {@inheritDoc}
     */
    public T GetFirst() {
        if (_head == _tail) throw new Exception("The vector is empty");
        return _elements[_head];
    }

    /**
     * Read but don't remove last item
     * @throws NoSuchElementException {@inheritDoc}
     */
    public T GetLast() {
        if (_head == _tail) throw new Exception("The vector is empty");
        return _elements[(_tail - 1) & (_elements.Length - 1)];
    }


    /**
     * Removes the element at the specified position in the elements array,
     * adjusting head and tail as necessary.  This can result in motion of
     * elements backwards or forwards in the array.
     */
    public void Delete(int i) {
        var mask = _elements.Length - 1;
        var h = _head;
        var t = _tail;
        var front = (i - h) & mask;
        var back = (t - i) & mask;

        // Invariant: head <= i < tail mod circularity
        if (front >= ((t - h) & mask))
            throw new Exception("Possible concurrent modification");

        // Optimize for least element motion
        if (front < back) {
            if (h <= i) {
                Array.Copy(_elements, h, _elements, h + 1, front);
            } else { // Wrap around
                Array.Copy(_elements, 0, _elements, 1, i);
                _elements[0] = _elements[mask];
                Array.Copy(_elements, h, _elements, h + 1, mask - h);
            }
            _elements[h] = default!;
            _head = (h + 1) & mask;
        } else {
            if (i < t) { // Copy the null tail as well
                Array.Copy(_elements, i + 1, _elements, i, back);
                _tail = t - 1;
            } else { // Wrap around
                Array.Copy(_elements, i + 1, _elements, i, mask - i);
                _elements[mask] = _elements[0];
                Array.Copy(_elements, 1, _elements, 0, t);
                _tail = (t - 1) & mask;
            }
        }
    }

    // *** Collection Methods ***

    /**
     * Returns {@code true} if this deque contains no elements.
     *
     * @return {@code true} if this deque contains no elements
     */
    public bool IsEmpty() {
        return _head == _tail;
    }

    /**
     * Returns {@code false} if this deque contains no elements.
     */
    public bool NotEmpty() {
        return _head != _tail;
    }

    /**
     * Removes all of the elements from this deque.
     * The deque will be empty after this call returns.
     */
    public void Clear() {
        var h = _head;
        var t = _tail;
        if (h != t) { // clear all cells
            _head = _tail = 0;
            var i = h;
            var mask = _elements.Length - 1;
            do {
                _elements[i] = default!;
                i = (i + 1) & mask;
            } while (i != t);
        }
    }

    /**
     * Returns an array containing all of the elements in this deque
     * in proper sequence (from first to last element).
     *
     * <p>The returned array will be "safe" in that no references to it are
     * maintained by this deque.  (In other words, this method must allocate
     * a new array).  The caller is thus free to modify the returned array.</p>
     *
     * <p>This method acts as bridge between array-based and collection-based
     * APIs.</p>
     *
     * @return an array containing all of the elements in this deque
     */
    public T[] ToArray() {
        var wrap = _tail < _head;
        var end = wrap ? _tail + _elements.Length : _tail;
        var newLength = end - _head;
        if (newLength < 0) throw new Exception(_head + " > " + end);
        
        var copy = new T[newLength];
        Array.Copy(_elements, _head, copy, 0, Math.Min(_elements.Length - _head, newLength));
        if (wrap) Array.Copy(_elements, 0, copy, _elements.Length - _head, _tail);
        
        return copy;
    }

    // *** Array-like Methods ***

    /**
     * Returns the number of elements in this deque.
     */
    public int Length() {
        return (_tail - _head) & (_elements.Length - 1);
    }

    /** set the value at the given index */
    public void Set(int index, T value) {
        if (index >= Length()) return;
        if (index < 0) return;

        if (_head < _tail) {
            _elements[index + _head] = value;
            return;
        }

        var rIdx = (_elements.Length - 1) - _head; // 'real' index at end of array
        if (index <= rIdx) _elements[index + _head] = value; // it's on the 'right' side of array
        else _elements[index - (rIdx + 1)] = value;// index is wrapped
    }

    /** update value at index to equal v(value) */
    public void Edit(int index, Func<T,T> v) {
        if (index >= Length()) return;
        if (index < 0) return;

        if (_head < _tail) {
            _elements[index + _head] = v(_elements[index + _head]);
            return;
        }

        var rIdx = (_elements.Length - 1) - _head; // 'real' index at end of array
        if (index <= rIdx) _elements[index + _head] = v(_elements[index + _head]); // it's on the 'right' side of array
        else _elements[index - (rIdx + 1)] = v(_elements[index - (rIdx + 1)]);// index is wrapped
    }

    /** return the value at the given index. Throws exception if out of range */
    public T Get(int index) {
        if (index >= Length()) throw new Exception("Index out of range");
        if (index < 0) throw new Exception("Index is invalid");

        // Just addFirst looks like ; addFirst(0),addFirst(1),addFirst(2)
        // conceptually, this is the array [0,1,2]
        // [<tail> _, ... _, <head>3, 2, 1]

        // Just addLast looks like ; addLast(0),addLast(1),addLast(2)
        // conceptually, this is the array [2,1,0]
        // [<tail> 0, 1, 2 _, ... _, <head>_]

        if (_head < _tail) return _elements[index + _head];

        var rIdx = (_elements.Length - 1) - _head; // 'real' index at end of array
        if (index <= rIdx) return _elements[index + _head]; // it's on the 'right' side of array
        return _elements[index - (rIdx + 1)];// index is wrapped
    }

    /** return the value at the given index. Returns defaultValue if out of range */
    public T Get(int index, T defaultValue) {
        if (index >= Length()) return defaultValue;
        if (index < 0) return defaultValue;

        if (_head < _tail) return _elements[index + _head];

        var rIdx = (_elements.Length - 1) - _head; // 'real' index at end of array
        if (index <= rIdx) return _elements[index + _head]; // it's on the 'right' side of array
        return _elements[index - (rIdx + 1)];// index is wrapped
    }


    /** returns true if the index is valid in the vector */
    public bool HasIndex(int idx) {
        return idx >= 0 && idx < Length();
    }

    /** remove items from end until length is less than or equal to newLength*/
    public void TruncateTo(int newLength) {
        if (newLength <=0) {
            Clear();
            return;
        }
        while (Length() > newLength){
            this.PollLast();
        }
    }

    /** remove items from start while they match a comparator function */
    public void TrimLeading(Func<T, bool> comparator){
        while (Length() > 0){
            if (!comparator(GetFirst())) return;
            RemoveFirst();
        }
    }

    /** reverse the order of items in this vector, without moving head or tail pointers */
    public void Reverse() {
        if (Length() < 2) return;

        var h = _head;
        var t = _tail;
        var m = _elements.Length - 1;
        var c = Length() / 2;

        t = (t - 1) & m;
        for (var i = 0; i < c; i++) {
            (_elements[h], _elements[t]) = (_elements[t], _elements[h]);
            h = (h + 1) & m;
            t = (t - 1) & m;
        }

    }

    /**
     * Create a copy from [start..end)
     * @param start inclusive start index
     * @param end exclusive end index
     */
    public Vector<T> Slice(int start, int end) {
        if (start < 0) start += Length();
        if (end < 0) end += Length();
        if (start < 0 || start >= end) return new Vector<T>();

        var result = new Vector<T>(end - start);
        for (var i = start; i < end; i++){
            result.AddLast(Get(i));
        }
        return result;
    }
}