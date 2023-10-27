using System.Diagnostics.CodeAnalysis;

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
    /// <summary>
    /// Return a new vector containing a single element
    /// </summary>
    public static Vector<TV> FromValue<TV>(TV v){
        var result = new Vector<TV>();
        result.AddLast(v);
        return result;
    }

    ///<summary>
    /// Constructs an empty vector
    ///</summary>
    public Vector() {
        _elements = new T[8];
    }

    /// <summary>
    /// Constructs an empty array vector with an initial capacity sufficient to hold the specified number of elements.
    /// </summary>
    /// <param name="numElements">lower bound on initial capacity</param>
    public Vector(int numElements) {
        _elements = Array.Empty<T>();
        AllocateElements(numElements);
    }

    /// <summary>
    /// Constructs a vector containing the elements of the specified
    /// collection, in the order they are returned by the collection's
    /// iterator.  (The first element returned by the collection's
    /// iterator becomes the first element, or <i>front</i> of the
    /// vector)
    /// </summary>
    /// <param name="c">the collection whose elements are to be placed into the vector</param>
    public Vector(T[] c) {
        _elements = Array.Empty<T>();
        AllocateElements(c.Length);
        foreach (var v in c) AddLast(v);
    }

    /// <summary>
    /// Create a shallow copy of 'other'.
    /// No data is shared between vectors, but reference elements will not be copied.
    /// </summary>
    /// <param name="other">Vector to copy</param>
    public Vector(Vector<T> other){
        _elements = new T[other._elements.Length];
        _head = other._head;
        _tail = other._tail;
        Array.Copy(other._elements, 0, _elements, 0, _elements.Length);
    }

    /// <summary>
    /// Get or set a value at the given index.
    /// An exception will be thrown if the index is out of bounds.
    /// </summary>
    /// <param name="index">zero-based index of item to access</param>
    public T this[int index] {
        get => Get(index);
        set => Set(index, value);
    }


    /// <summary>
    /// Inserts the specified element at the front of this vector.
    /// </summary>
    /// <param name="e">element to add</param>
    public void AddFirst(T e) {
        _elements[_head = (_head - 1) & (_elements.Length - 1)] = e;
        if (_head == _tail) DoubleCapacity();
    }

    /// <summary>
    /// Inserts the specified element at the end of this vector.
    /// </summary>
    /// <param name="e">element to add</param>
    public void AddLast(T e) {
        _elements[_tail] = e;
        if ( (_tail = (_tail + 1) & (_elements.Length - 1)) == _head)
            DoubleCapacity();
    }

    /// <summary>
    /// Remove the element at the front of the vector, returning its value
    /// </summary>
    /// <returns>Value of element removed</returns>
    /// <exception cref="Exception">Throws if the vector is empty</exception>
    public T RemoveFirst() {
        if (_head == _tail) throw new Exception("The vector is empty");
        return PollFirst();
    }

    /// <summary>
    /// Remove the element at the front of the vector, returning its value if the vector is not empty.
    /// Returns true if an item was removed, false if the vector was empty before calling.
    /// </summary>
    public bool TryRemoveFirst(out T item)
    {
        item = default!;
        if (_head == _tail) return false;
        item = PollFirst();
        return true;
    }

    /// <summary>
    /// Remove the element at the back of the vector, returning its value
    /// </summary>
    /// <returns>Value of element removed</returns>
    /// <exception cref="Exception">Throws if the vector is empty</exception>
    public T RemoveLast()
    {
        if (_head == _tail) throw new Exception("The vector is empty");
        return PollLast();
    }

    /// <summary>
    /// Remove the element at the back of the vector, returning its value if the vector is not empty.
    /// Returns true if an item was removed, false if the vector was empty before calling.
    /// </summary>
    public bool TryRemoveLast(out T item) {
        item = default!;
        if (_head == _tail) return false;
        item = PollLast();
        return true;
    }

    /// <summary>
    /// Read but don't remove first item in the vector
    /// </summary>
    /// <returns>Value of first item</returns>
    /// <exception cref="Exception">Thrown if the vector is empty</exception>
    public T GetFirst() {
        if (_head == _tail) throw new Exception("The vector is empty");
        return _elements[_head];
    }
    
    /// <summary>
    /// Read but don't remove first item in the vector.
    /// Returns false if the vector is empty.
    /// </summary>
    public bool TryGetFirst(out T item) {
        item = default!;
        if (_head == _tail) return false;
        item = _elements[_head];
        return true;
    }

    /// <summary>
    /// Read but don't remove last item in the vector
    /// </summary>
    /// <returns>Value of last item</returns>
    /// <exception cref="Exception">Thrown if the vector is empty</exception>
    public T GetLast() {
        if (_head == _tail) throw new Exception("The vector is empty");
        return _elements[(_tail - 1) & (_elements.Length - 1)];
    }
    
    /// <summary>
    /// Read but don't remove last item in the vector.
    /// Returns false if the vector is empty.
    /// </summary>
    public bool TryGetLast(out T item) {
        item = default!;
        if (_head == _tail) return false;
        item = _elements[(_tail - 1) & (_elements.Length - 1)];
        return true;
    }

    ///<summary>
    /// Removes the element at the specified position in the elements array,
    /// adjusting head and tail as necessary.  This can result in motion of
    /// elements backwards or forwards in the array.
    ///</summary>
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

    /// <summary>
    /// Returns <c>true</c> if this vector contains no elements.
    /// </summary>
    public bool IsEmpty() {
        return _head == _tail;
    }

    /// <summary>
    /// Returns <c>false</c> if this vector contains no elements.
    /// </summary>
    public bool NotEmpty() {
        return _head != _tail;
    }

    ///<summary>
    /// Removes all of the elements from this vector.
    /// The vector will be empty after this call returns.
    /// <p></p>
    /// The vector remains valid, and new items can be added.
    ///</summary>
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

    /// <summary>
    /// Returns an array containing all of the elements in this vector
    /// in proper sequence (from first to last element).
    /// <p></p>
    /// The returned array will be "safe" in that no references to it are
    /// maintained by the parent vector. (In other words, this method must allocate
    /// a new array).  The caller is thus free to modify the returned array.
    /// </summary>
    /// <returns>an array containing all of the elements in this vector</returns>
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

    /// <summary>
    /// Returns the number of elements in this vector.
    /// </summary>
    public int Length() {
        return (_tail - _head) & (_elements.Length - 1);
    }

    /// <summary>
    /// set the value at the given index
    /// </summary>
    /// <param name="index">zero-based index into the logical vector</param>
    /// <param name="value">new value at this position</param>
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

    /// <summary>
    /// update the value at the given index, so new value is <c>v(old_value)</c>
    /// </summary>
    /// <param name="index">zero-based index into the logical vector</param>
    /// <param name="v">function that takes old value and returns new value</param>
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

    /// <summary>
    /// Return the value at the given index.
    /// See also <see cref="Get(int,T)"/>
    /// </summary>
    /// <param name="index">zero-based index into the logical vector</param>
    /// <returns>Value at logical index</returns>
    /// <exception cref="Exception">Thrown if index is out of range</exception>
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

    /// <summary>
    /// Return the value at the given index. Gives a default value if index is out of range.
    /// See also <see cref="Get(int)"/>
    /// </summary>
    /// <param name="index">zero-based index into the logical vector</param>
    /// <param name="defaultValue">value to return if index is out of bounds</param>
    /// <returns>Value at logical index, or default value</returns>
    public T Get(int index, T defaultValue) {
        if (index >= Length()) return defaultValue;
        if (index < 0) return defaultValue;

        if (_head < _tail) return _elements[index + _head];

        var rIdx = (_elements.Length - 1) - _head; // 'real' index at end of array
        if (index <= rIdx) return _elements[index + _head]; // it's on the 'right' side of array
        return _elements[index - (rIdx + 1)];// index is wrapped
    }

    /// <summary>
    /// returns true if the index is valid in the vector
    /// </summary>
    /// <param name="idx">zero-based index to check</param>
    /// <returns><c>true</c> if the index is valid, <c>false</c> if the index is out of range.</returns>
    public bool HasIndex(int idx) {
        return idx >= 0 && idx < Length();
    }

    /// <summary>
    /// Remove items from end until length is less than or equal to newLength.
    /// If the vector is already less-than-or-equal-to the given length, no changes are made.
    /// </summary>
    /// <param name="newLength">new maximum length of the vector</param>
    public void TruncateTo(int newLength) {
        if (newLength <= 0) {
            Clear();
            return;
        }
        while (Length() > newLength){
            PollLast();
        }
    }

    /// <summary>
    /// Remove items from start of the vector while they match a comparator function.
    /// As soon as one value does not match, no more values are removed.
    /// </summary>
    /// <param name="comparator">function that returns <c>true</c> for any values to be trimmed</param>
    public void TrimLeading(Func<T, bool> comparator){
        while (Length() > 0){
            if (!comparator(GetFirst())) return;
            RemoveFirst();
        }
    }

    /// <summary>
    /// Reverse the order of items in this vector in place
    /// </summary>
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

    /// <summary>
    ///  Creates a shallow copy from index <c>start</c> (inclusive) to index <c>end</c> (exclusive)
    /// </summary>
    /// <param name="start">Inclusive start index</param>
    /// <param name="end">Exclusive end index</param>
    /// <returns>A new vector with a subset of values from the parent.</returns>
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
    
    #region Internals
    
    /// <summary>
    /// The array in which the elements of the vector are stored.
    /// The capacity of the vector is the length of this array, which is
    /// always a power of two. The array is never allowed to become
    /// full, except transiently within an <c>Add...</c> method where it is
    /// resized (see doubleCapacity) immediately upon becoming full,
    /// thus avoiding head and tail wrapping around to equal each
    /// other.  We also guarantee that all array cells not holding
    /// vector elements are always null.
    /// </summary>
    private T[] _elements;

    /// <summary>
    /// The index of the element at the head of the vector (which is the
    /// element that would be removed by remove() or pop()); or an
    /// arbitrary number equal to tail if the vector is empty.
    /// </summary>
    private volatile int _head;

    /// <summary>
    /// The index at which the next element would be added to the tail
    /// of the vector (via addLast(E), add(E), or push(E)).
    /// </summary>
    private volatile  int _tail;

    /// <summary>
    /// The minimum capacity that we'll use for a newly created vector.
    /// Must be a power of 2.
    /// </summary>
    public const uint MinInitialCapacity = 8;
    
    /// <summary>
    /// Maximum capacity for a queue. Must be a power of 2.
    /// </summary>
    public const int MaxCapacity = 0x4000_0000;
    
    /// <summary>
    /// Allocates empty array to hold the given number of elements.
    /// </summary>
    /// <param name="numElements">the number of elements to hold</param>
    /// <exception cref="Exception">If numElements is zero or less, or greater than max capacity</exception>
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

    ///<summary>
    /// Doubles the capacity of this vector.  Call only when full:
    /// when head and tail have wrapped around to become equal.
    ///</summary>
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
    /// Remove the element at the front of the vector, returning its value
    /// </summary>
    private T PollFirst() {
        var h = _head;
        var result = _elements[h];
        // Element might be null if vector empty
        if (result != null) {
            _elements[h] = default!;
            _head = (h + 1) & (_elements.Length - 1);
        }
        return result;
    }

    /// <summary>
    /// Remove the element at the back of the vector, returning its value
    /// </summary>
    private T PollLast() {
        var t = (_tail - 1) & (_elements.Length - 1);
        var result = _elements[t];
        if (result != null) {
            _elements[t] = default!;
            _tail = t;
        }
        return result;
    }
    #endregion

}