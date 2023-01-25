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
    private T[] elements;

    /**
     * The index of the element at the head of the deque (which is the
     * element that would be removed by remove() or pop()); or an
     * arbitrary number equal to tail if the deque is empty.
     */
    private volatile int head;

    /**
     * The index at which the next element would be added to the tail
     * of the deque (via addLast(E), add(E), or push(E)).
     */
    private volatile  int tail;

    /**
     * The minimum capacity that we'll use for a newly created deque.
     * Must be a power of 2.
     */
    private const uint MIN_INITIAL_CAPACITY = 8;
    
    /// <summary>
    /// Maximum capacity for a queue. Must be a power of 2.
    /// </summary>
    private const int MAX_CAPACITY = 0x4000_0000;

    // ******  Array allocation and resizing utilities ******

    /**
     * Allocates empty array to hold the given number of elements.
     *
     * @param numElements the number of elements to hold
     */
    private void allocateElements(int numElements) {
        if (numElements <= 0) throw new Exception("Invalid element count");
        if (numElements > MAX_CAPACITY) throw new Exception("Invalid element count");
        
        uint initialCapacity = MIN_INITIAL_CAPACITY;
       
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
        elements = new T[initialCapacity];
    }

    /**
     * Doubles the capacity of this deque.  Call only when full, i.e.,
     * when head and tail have wrapped around to become equal.
     */
    private void doubleCapacity() {
        if (head != tail) throw new Exception("Unexpected state (internal)");
        int p = head;
        int elementsLength = elements.Length;
        int r = elementsLength - p; // number of elements to the right of p
        int newCapacity = elementsLength << 1;
        if (newCapacity < 0)
            throw new Exception("Vector capacity exceeded");
        
        // Create new array, and copy elements over
        T[] newArray = new T[newCapacity];
        
        Array.Copy(elements, p, newArray, 0, r);
        Array.Copy(elements, 0, newArray, r, p);
        
        // null out old array (helps with GC)
        Array.Fill(elements, default);
        
        elements = newArray;
        head = 0;
        tail = elementsLength;
    }

    /// <summary>
    /// Return a new vector containing a single element
    /// </summary>
    public static Vector<TV> FromValue<TV>(TV v){
        var result = new Vector<TV>();
        result.addLast(v);
        return result;
    }

    /**
     * Constructs an empty array deque
     */
    public Vector() {
        elements = new T[8];
    }

    /**
     * Constructs an empty array deque with an initial capacity
     * sufficient to hold the specified number of elements.
     *
     * @param numElements lower bound on initial capacity of the deque
     */
    public Vector(int numElements) {
        elements = Array.Empty<T>();
        allocateElements(numElements);
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
        elements = Array.Empty<T>();
        allocateElements(c.Length);
        foreach (var v in c) addLast(v);
    }

    /**
     * Create a copy of 'other'. No data is shared.
     */
    public Vector(Vector<T> other){
        elements = new T[other.elements.Length];
        this.head = other.head;
        this.tail = other.tail;
        Array.Copy(other.elements, 0, this.elements, 0, elements.Length);
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
    public void addFirst(T e) {
        elements[head = (head - 1) & (elements.Length - 1)] = e;
        if (head == tail) doubleCapacity();
    }

    /**
     * Inserts the specified element at the end of this deque.
     */
    public void addLast(T e) {
        elements[tail] = e;
        if ( (tail = (tail + 1) & (elements.Length - 1)) == head)
            doubleCapacity();
    }

    /**
     * @throws NoSuchElementException {@inheritDoc}
     */
    public T removeFirst() {
        if (head == tail) throw new Exception("The vector is empty");
        return pollFirst();
    }

    /**
     * @throws NoSuchElementException {@inheritDoc}
     */
    public T removeLast() {
        if (head == tail) throw new Exception("The vector is empty");
        return pollLast();
    }

    private T pollFirst() {
        int h = head;
        T result = elements[h];
        // Element is null if deque empty
        if (result != null) {
            elements[h] = default!;
            head = (h + 1) & (elements.Length - 1);
        }
        return result;
    }

    private T pollLast() {
        int t = (tail - 1) & (elements.Length - 1);
        T result = elements[t];
        if (result != null) {
            elements[t] = default!;
            tail = t;
        }
        return result;
    }

    /**
     * Read but don't remove first item
     * @throws NoSuchElementException {@inheritDoc}
     */
    public T getFirst() {
        if (head == tail) throw new Exception("The vector is empty");
        return elements[head];
    }

    /**
     * Read but don't remove last item
     * @throws NoSuchElementException {@inheritDoc}
     */
    public T getLast() {
        if (head == tail) throw new Exception("The vector is empty");
        return elements[(tail - 1) & (elements.Length - 1)];
    }


    /**
     * Removes the element at the specified position in the elements array,
     * adjusting head and tail as necessary.  This can result in motion of
     * elements backwards or forwards in the array.
     */
    public void delete(int i) {
        int mask = elements.Length - 1;
        int h = head;
        int t = tail;
        int front = (i - h) & mask;
        int back = (t - i) & mask;

        // Invariant: head <= i < tail mod circularity
        if (front >= ((t - h) & mask))
            throw new Exception("Possible concurrent modification");

        // Optimize for least element motion
        if (front < back) {
            if (h <= i) {
                Array.Copy(elements, h, elements, h + 1, front);
            } else { // Wrap around
                Array.Copy(elements, 0, elements, 1, i);
                elements[0] = elements[mask];
                Array.Copy(elements, h, elements, h + 1, mask - h);
            }
            elements[h] = default!;
            head = (h + 1) & mask;
        } else {
            if (i < t) { // Copy the null tail as well
                Array.Copy(elements, i + 1, elements, i, back);
                tail = t - 1;
            } else { // Wrap around
                Array.Copy(elements, i + 1, elements, i, mask - i);
                elements[mask] = elements[0];
                Array.Copy(elements, 1, elements, 0, t);
                tail = (t - 1) & mask;
            }
        }
    }

    // *** Collection Methods ***

    /**
     * Returns the number of elements in this deque.
     *
     * @return the number of elements in this deque
     */
    public int size() {
        return (tail - head) & (elements.Length - 1);
    }

    /**
     * Returns {@code true} if this deque contains no elements.
     *
     * @return {@code true} if this deque contains no elements
     */
    public bool isEmpty() {
        return head == tail;
    }

    /**
     * Returns {@code false} if this deque contains no elements.
     */
    public bool notEmpty() {
        return head != tail;
    }

    /**
     * Removes all of the elements from this deque.
     * The deque will be empty after this call returns.
     */
    public void clear() {
        int h = head;
        int t = tail;
        if (h != t) { // clear all cells
            head = tail = 0;
            int i = h;
            int mask = elements.Length - 1;
            do {
                elements[i] = default!;
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
    public T[] toArray() {
        bool wrap = (tail < head);
        int end = wrap ? tail + elements.Length : tail;
        int newLength = end - head;
        if (newLength < 0) throw new Exception(head + " > " + end);
        
        T[] copy = new T[newLength];
        Array.Copy(elements, head, copy, 0, Math.Min(elements.Length - head, newLength));
        var result = copy;
        if (wrap) Array.Copy(elements, 0, result, elements.Length - head, tail);
        
        return result;
    }

    // *** Array-like Methods ***

    /**
     * Returns the number of elements in this deque.
     */
    public int length() {
        return (tail - head) & (elements.Length - 1);
    }

    /** set the value at the given index */
    public void set(int index, T value) {
        if (index >= length()) return;
        if (index < 0) return;

        if (head < tail) {
            elements[index + head] = value;
            return;
        }

        int rIdx = (elements.Length - 1) - head; // 'real' index at end of array
        if (index <= rIdx) elements[index + head] = value; // it's on the 'right' side of array
        else elements[index - (rIdx + 1)] = value;// index is wrapped
    }

    /** update value at index to equal v(value) */
    public void edit(int index, Func<T,T> v) {
        if (index >= length()) return;
        if (index < 0) return;

        if (head < tail) {
            elements[index + head] = v(elements[index + head]);
            return;
        }

        int rIdx = (elements.Length - 1) - head; // 'real' index at end of array
        if (index <= rIdx) elements[index + head] = v(elements[index + head]); // it's on the 'right' side of array
        else elements[index - (rIdx + 1)] = v(elements[index - (rIdx + 1)]);// index is wrapped
    }

    /** return the value at the given index. Throws exception if out of range */
    public T get(int index) {
        if (index >= length()) throw new Exception("Index out of range");
        if (index < 0) throw new Exception("Index is invalid");

        // Just addFirst looks like ; addFirst(0),addFirst(1),addFirst(2)
        // conceptually, this is the array [0,1,2]
        // [<tail> _, ... _, <head>3, 2, 1]

        // Just addLast looks like ; addLast(0),addLast(1),addLast(2)
        // conceptually, this is the array [2,1,0]
        // [<tail> 0, 1, 2 _, ... _, <head>_]

        if (head < tail) return elements[index + head];

        int rIdx = (elements.Length - 1) - head; // 'real' index at end of array
        if (index <= rIdx) return elements[index + head]; // it's on the 'right' side of array
        return elements[index - (rIdx + 1)];// index is wrapped
    }

    /** return the value at the given index. Returns defaultValue if out of range */
    public T get(int index, T defaultValue) {
        if (index >= length()) return defaultValue;
        if (index < 0) return defaultValue;

        if (head < tail) return elements[index + head];

        int rIdx = (elements.Length - 1) - head; // 'real' index at end of array
        if (index <= rIdx) return elements[index + head]; // it's on the 'right' side of array
        return elements[index - (rIdx + 1)];// index is wrapped
    }


    /** returns true if the index is valid in the vector */
    public bool hasIndex(int idx) {
        return idx >= 0 && idx < length();
    }

    /** remove items from end until length is less than or equal to newLength*/
    public void truncateTo(int newLength) {
        if (newLength <=0) {
            clear();
            return;
        }
        while (length() > newLength){
            this.pollLast();
        }
    }

    /** remove items from start while they match a comparator function */
    public void trimLeading(Func<T, bool> comparator){
        while (length() > 0){
            if (!comparator(getFirst())) return;
            removeFirst();
        }
    }

    /** reverse the order of items in this vector, without moving head or tail pointers */
    public void reverse() {
        if (length() < 2) return;

        int h = head;
        int t = tail;
        int m = elements.Length - 1;
        int c = length() / 2;

        t = (t - 1) & m;
        for (int i = 0; i < c; i++) {
            (elements[h], elements[t]) = (elements[t], elements[h]);
            h = (h + 1) & m;
            t = (t - 1) & m;
        }

    }

    /**
     * Create a copy from [start..end)
     * @param start inclusive start index
     * @param end exclusive end index
     */
    public Vector<T> slice(int start, int end) {
        if (start < 0) start += length();
        if (end < 0) end += length();
        if (start < 0 || start >= end) return new Vector<T>();

        Vector<T> result = new Vector<T>(end - start);
        for (int i = start; i < end; i++){
            result.addLast(get(i));
        }
        return result;
    }
}