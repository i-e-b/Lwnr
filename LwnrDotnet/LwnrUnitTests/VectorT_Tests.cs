using LwnrCore.Containers;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace LwnrUnitTests;

[TestFixture]
public class VectorT_Tests
{
    private const double epsilon = 0.0001;
    
    [Test]
    public void can_create_empty_vec() {
        Vector<double> v = new Vector<double>();

        Assert.That(v.length(), Is.EqualTo(0), "initial size");
        Assert.That(v.isEmpty(), Is.True, "empty flag");
    }

    [Test]
    public void can_create_vec_by_adding_to_start() {
        Vector<double> v = new Vector<double>();
        v.addFirst(1.0);
        v.addFirst(2.0);
        v.addFirst(3.0);

        // Conceptually, [3,2,1]

        Assert.That(v.length(), Is.EqualTo(3), "size");
        Assert.That(v.isEmpty(), Is.False, "empty flag");

        // Check get by index
        Assert.That(v.get(0), Is.EqualTo(3.0).Within(epsilon), "idx 0");
        Assert.That(v.get(2), Is.EqualTo(1.0).Within(epsilon), "idx 2");
    }

    [Test]
    public void can_create_vec_by_adding_to_end() {
        Vector<double> v = new Vector<double>();
        v.addLast(1.0);
        v.addLast(2.0);
        v.addLast(3.0);

        // Conceptually, [1,2,3]

        Assert.That(v.length(), Is.EqualTo(3), "size");
        Assert.That(v.isEmpty(), Is.False, "empty flag");

        // Check get by index
        Assert.That(v.get(0), Is.EqualTo(1.0).Within(epsilon), "idx 0");
        Assert.That(v.get(2), Is.EqualTo(3.0).Within(epsilon), "idx 2");
    }

    [Test]
    public void can_create_vec_by_adding_to_both_sides() {
        Vector<double> v = new Vector<double>();
        v.addLast(1.0);
        v.addLast(2.0);
        v.addFirst(3.0);
        v.addFirst(4.0);

        // Conceptually, [4,3,1,2]

        Assert.That(v.length(), Is.EqualTo(4), "size");
        Assert.That(v.isEmpty(), Is.False, "empty flag");

        // Check get by index
        Assert.That(v.get(0), Is.EqualTo(4.0).Within(epsilon), "idx 0");
        Assert.That(v.get(1), Is.EqualTo(3.0).Within(epsilon), "idx 1");
        Assert.That(v.get(2), Is.EqualTo(1.0).Within(epsilon), "idx 2");
        Assert.That(v.get(3), Is.EqualTo(2.0).Within(epsilon), "idx 3");
    }

    [Test]
    public void can_peek_at_vector_ends_without_removing_items() {
        Vector<double> v = new Vector<double>();
        v.addLast(1.0);
        v.addLast(2.0);
        v.addFirst(3.0);
        v.addFirst(4.0);

        // Conceptually, [4,3,1,2]

        Assert.That(v.getFirst(), Is.EqualTo(4.0).Within(epsilon), "peek start 1");
        Assert.That(v.getFirst(), Is.EqualTo(4.0).Within(epsilon), "peek start 2");
        Assert.That(v.getLast(), Is.EqualTo(2.0).Within(epsilon), "peek end 1");
        Assert.That(v.getLast(), Is.EqualTo(2.0).Within(epsilon), "peek end 2");

        Assert.That(v.length(), Is.EqualTo(4), "size");
        Assert.That(v.isEmpty(), Is.False, "empty flag");

        // Check get by index
        Assert.That(v.get(0), Is.EqualTo(4.0).Within(epsilon), "idx 0");
        Assert.That(v.get(1), Is.EqualTo(3.0).Within(epsilon), "idx 1");
        Assert.That(v.get(2), Is.EqualTo(1.0).Within(epsilon), "idx 2");
        Assert.That(v.get(3), Is.EqualTo(2.0).Within(epsilon), "idx 3");
    }

    [Test]
    public void can_create_vec_from_array() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> v = new Vector<double>(src);

        Assert.That(v.length(), Is.EqualTo(6), "size");
        Assert.That(v.isEmpty(), Is.False, "empty flag");

        // Check get by index
        Assert.That(v.get(0), Is.EqualTo(0.1).Within(epsilon), "idx 0");
        Assert.That(v.get(5), Is.EqualTo(5.6).Within(epsilon), "idx 5");
    }

    [Test]
    public void can_restore_array_from_vec() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> v = new Vector<double>(src);

        double[] result = v.toArray();

        Assert.That(v.length(), Is.EqualTo(6), "vector length");
        Assert.That(result.Length, Is.EqualTo(6), "result length");

        for (int i = 0; i < src.Length; i++) {
            Assert.That(result[i], Is.EqualTo(src[i]).Within(epsilon), "index "+i);
        }
    }

    [Test]
    public void can_check_if_indexes_are_in_bounds_of_vec() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> v = new Vector<double>(src);

        Assert.False(v.hasIndex(-1), "too low");
        Assert.True(v.hasIndex(0), "first item");
        Assert.True(v.hasIndex(3), "middle item");
        Assert.True(v.hasIndex(5), "last item");
        Assert.False(v.hasIndex(6), "too high");
        Assert.False(v.hasIndex(6000), "way too high");
    }

    [Test]
    public void can_restore_array_from_vec_after_removing_items() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> v = new Vector<double>(src);

        Assert.That(v.removeFirst(), Is.EqualTo(0.1).Within(epsilon), "removed first");
        Assert.That(v.removeLast(), Is.EqualTo(5.6).Within(epsilon), "removed last");

        double[] result = v.toArray();

        Assert.That(v.length(), Is.EqualTo(4), "vector length");
        Assert.That(result.Length, Is.EqualTo(4), "result length");

        for (int i = 0; i < result.Length; i++) {
            Assert.That(result[i], Is.EqualTo(src[i+1]).Within(epsilon), "index "+i);
        }
    }

    [Test]
    public void can_restore_array_from_vec_after_adding_items() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> v = new Vector<double>(src);

        v.addFirst(-1);
        v.addLast(-2);

        double[] expected = {-1.0, 0.1, 1.2, 2.3, 3.4, 4.5, 5.6, -2.0};
        double[] result = v.toArray();

        Assert.That(v.length(), Is.EqualTo(8), "vector length");
        Assert.That(result.Length, Is.EqualTo(8), "result length");

        for (int i = 0; i < expected.Length; i++) {
            Assert.That(result[i], Is.EqualTo(expected[i]).Within(epsilon), "index "+i);
        }
    }

    [Test]
    public void can_remove_vector_items_by_index() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> v = new Vector<double>(src);

        v.delete(1);
        v.delete(4); // index 5 in src

        double[] expected = {0.1, 2.3, 3.4, 5.6};
        double[] result = v.toArray();

        Assert.That(v.length(), Is.EqualTo(4), "vector length");
        Assert.That(result.Length, Is.EqualTo(4), "result length");

        for (int i = 0; i < expected.Length; i++) {
            Assert.That(result[i], Is.EqualTo(expected[i]).Within(epsilon), "index "+i);
        }
    }

    [Test]
    public void can_clear_all_items_from_vector() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> v = new Vector<double>(src);

        Assert.That(v.length(), Is.EqualTo(6), "vector length before clear");

        v.clear();
        Assert.That(v.length(), Is.EqualTo(0), "vector length after clear");

        double[] result = v.toArray();
        Assert.That(result.Length, Is.EqualTo(0), "array length after clear");

        // can start adding things again
        v.addLast(1);
        v.addLast(2);
        v.addLast(3);

        double[] expected = {1,2,3};
        result = v.toArray();

        Assert.That(v.length(), Is.EqualTo(3), "vector length");
        Assert.That(result.Length, Is.EqualTo(3), "array length");

        for (int i = 0; i < expected.Length; i++) {
            Assert.That(result[i], Is.EqualTo(expected[i]).Within(epsilon), "index "+i);
        }
    }

    [Test]
    public void can_modify_items_in_vec_in_place_by_index() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 123};
        Vector<double> v = new Vector<double>(src);

        v.edit(1, x => x + 10.0);
        v.edit(2, x => x * 10.0);
        v.set(4, -4.4);
        v.edit(5, x => x % 10);

        double[] expected = {0.1, 11.2, 23.0, 3.4, -4.4, 3};
        double[] result = v.toArray();

        Assert.That(v.length(), Is.EqualTo(6), "vector length");
        Assert.That(result.Length, Is.EqualTo(6), "array length");

        for (int i = 0; i < expected.Length; i++) {
            Assert.That(result[i], Is.EqualTo(expected[i]).Within(epsilon), "index "+i);
        }
    }

    [Test]
    public void vectors_can_scale_beyond_initial_bounds() {
        double[] initial = {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20};
        Vector<double> src = new Vector<double>(initial);
        Vector<double> dst = new Vector<double>(8);

        while (src.notEmpty()){
            Assert.False(src.isEmpty());

            dst.addFirst(src.removeLast());
            dst.addLast(src.removeFirst());
        }

        double[] expected = {11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
        double[] result = dst.toArray();

        Assert.That(dst.length(), Is.EqualTo(20), "dest length");
        Assert.That(result.Length, Is.EqualTo(20), "result length");
        Assert.That(src.length(), Is.EqualTo(0), "source length");

        for (int i = 0; i < expected.Length; i++) {
            Assert.That(result[i], Is.EqualTo(expected[i]).Within(epsilon), "index "+i);
        }
    }

    [Test]
    public void vectors_can_be_truncated_to_a_given_length(){
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> v = new Vector<double>(src);

        v.truncateTo(3);

        double[] expected = {0.1, 1.2, 2.3};
        double[] result = v.toArray();

        Assert.That(v.length(), Is.EqualTo(3), "vector length");
        Assert.That(result.Length, Is.EqualTo(3), "result length");

        for (int i = 0; i < expected.Length; i++) {
            Assert.That(result[i], Is.EqualTo(expected[i]).Within(epsilon), "index "+i);
        }
    }

    [Test]
    public void vectors_can_have_leading_zeros_truncated(){
        double[] src = {0.0, 0.0, 0.0000001, 1, 2, 0, 0, 0};
        Vector<double> v = new Vector<double>(src);

        Assert.That(v.length(), Is.EqualTo(8), "vector length");
        v.trimLeading(x => x == 0.0);
        Assert.That(v.length(), Is.EqualTo(6), "length after");
        v.trimLeading(x => x == 0.0); // no-op if no leading zeros
        Assert.That(v.length(), Is.EqualTo(6), "length after");

        Assert.That(v.toArray(), Is.EqualTo(new[]{0.0000001, 1, 2, 0, 0, 0}).Within(epsilon).AsCollection, "values");
    }

    [Test]
    public void copied_vectors_do_not_share_data(){
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> a = new Vector<double>(src);
        Vector<double> b = new Vector<double>(a);

        a.set(1, 1000.2);

        Assert.That(a.get(1), Is.EqualTo(1000.2).Within(epsilon), "A1");
        Assert.That(b.get(1), Is.EqualTo(1.2).Within(epsilon), "B1");

        b.reverse();
        Assert.That(a.toArray(), Is.EqualTo(new[]{0.1, 1000.2, 2.3, 3.4, 4.5, 5.6}).Within(epsilon).AsCollection, "A2");
        Assert.That(b.toArray(), Is.EqualTo(new[]{5.6, 4.5, 3.4, 2.3, 1.2, 0.1}).Within(epsilon).AsCollection, "B2");
    }

    [Test]
    public void can_create_vec_as_subset_of_another() {
        double[] src = {0.1, 1.2, 2.3, 3.4, 4.5, 5.6};
        Vector<double> a = new Vector<double>(src);
        Vector<double> b = a.slice(1,3);

        Assert.That(a.length(), Is.EqualTo(6), "a length");
        Assert.That(b.length(), Is.EqualTo(2), "b length");

        // values not shared
        a.set(1, 100);
        a.set(2, 200);

        // Check get by index
        Assert.That(a.toArray(), Is.EqualTo(new[]{0.1, 100, 200, 3.4, 4.5, 5.6}).Within(epsilon).AsCollection, "A");
        Assert.That(b.toArray(), Is.EqualTo(new[]{1.2, 2.3}).Within(epsilon).AsCollection, "B");
    }

    [Test]
    public void can_reverse_vector_in_place_after_various_operations_1(){
        Vector<double> v = new Vector<double>();

        v.reverse(); // should be a no-op, cause no errors
        Assert.That(v.length(), Is.EqualTo(0), "vector length");

        v.addLast(5);  // 5
        v.addFirst(4); // 4 5

        Console.WriteLine(string.Join(", ", v.toArray()));
        Assert.That(v.toArray(), Is.EqualTo(new[]{4,5}).Within(epsilon).AsCollection, "a");
        v.reverse();
        Assert.That(v.toArray(), Is.EqualTo(new[]{5,4}).Within(epsilon).AsCollection, "b");
        v.reverse();
        Assert.That(v.toArray(), Is.EqualTo(new[]{4,5}).Within(epsilon).AsCollection, "c");

        v.addLast(6);  // 4 5 6
        v.addFirst(3); // 3 4 5 6
        v.addLast(7);  // 3 4 5 6 7
        v.addFirst(2); // 2 3 4 5 6 7

        Assert.That(v.toArray(), Is.EqualTo(new[]{2,3,4,5,6,7}).Within(epsilon).AsCollection, "d");
        v.reverse();
        Assert.That(v.toArray(), Is.EqualTo(new[]{7,6,5,4,3,2}).Within(epsilon).AsCollection, "e");
        v.reverse();
        Assert.That(v.toArray(), Is.EqualTo(new[]{2,3,4,5,6,7}).Within(epsilon).AsCollection, "f");

        v.addLast(8);  // 2 3 4 5 6 7 8
        v.addFirst(1); // 1 2 3 4 5 6 7 8
        v.addLast(9);  // 1 2 3 4 5 6 7 8 9
        v.addFirst(0); // 0 1 2 3 4 5 6 7 8 9

        Assert.That(v.toArray(), Is.EqualTo(new[]{0,1,2,3,4,5,6,7,8,9}).Within(epsilon).AsCollection, "g");
        v.reverse();
        Assert.That(v.toArray(), Is.EqualTo(new[]{9,8,7,6,5,4,3,2,1,0}).Within(epsilon).AsCollection, "h");
        v.reverse();
        Assert.That(v.toArray(), Is.EqualTo(new[]{0,1,2,3,4,5,6,7,8,9}).Within(epsilon).AsCollection, "i");
    }

    [Test]
    public void can_reverse_vector_in_place_after_various_operations_2(){
        Vector<double> v = new Vector<double>();

        v.addLast(-1);
        v.addLast(-1);
        v.addLast(0);
        v.addLast(1);
        v.addLast(2);
        v.addLast(3);
        v.removeFirst();
        v.removeFirst();

        Assert.That(v.toArray(), Is.EqualTo(new[]{0,1,2,3}).Within(epsilon).AsCollection, "a");
        v.reverse();
        Assert.That(v.toArray(), Is.EqualTo(new[]{3,2,1,0}).Within(epsilon).AsCollection, "b");
        v.reverse();
        Assert.That(v.toArray(), Is.EqualTo(new[]{0,1,2,3}).Within(epsilon).AsCollection, "c");
    }
}