﻿using LwnrCore.Containers;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace LwnrUnitTests;

[TestFixture]
public class FizzleHashMapTests
{
    [Test]
    public void add_and_get_values()
    {
        var subject = new FizzleHashMap<string, string>(512);
        
        var ok1 = subject.TryAdd("one",   "value 1");
        var ok2 = subject.TryAdd("two",   "value 2");
        var ok3 = subject.TryAdd("three", "value 3");
        var ok4 = subject.TryAdd("four",  "value 4");
        
        Assert.That(ok1, Is.True, "Should add value 1");
        Assert.That(ok2, Is.True, "Should add value 2");
        Assert.That(ok3, Is.True, "Should add value 3");
        Assert.That(ok4, Is.True, "Should add value 4");
        
        var found1 = subject.TryGet("one",   out var value1);
        var found2 = subject.TryGet("two",   out var value2);
        var found3 = subject.TryGet("three", out var value3);
        var found4 = subject.TryGet("four",  out var value4);
        
        Assert.That(found1, Is.True, "Should find value 1");
        Assert.That(found2, Is.True, "Should find value 2");
        Assert.That(found3, Is.True, "Should find value 3");
        Assert.That(found4, Is.True, "Should find value 4");
        
        Assert.That(value1, Is.EqualTo("value 1"), "Should find value 1");
        Assert.That(value2, Is.EqualTo("value 2"), "Should find value 2");
        Assert.That(value3, Is.EqualTo("value 3"), "Should find value 3");
        Assert.That(value4, Is.EqualTo("value 4"), "Should find value 4");
    }

    [Test]
    public void small_map_size()
    {
        var subject = new FizzleHashMap<string, string>(8);
        
        var ok1 = subject.TryAdd("one",   "value 1");
        var ok2 = subject.TryAdd("two",   "value 2");
        var ok3 = subject.TryAdd("three", "value 3");
        var ok4 = subject.TryAdd("four",  "value 4");
        
        Assert.That(ok1, Is.True, "Should add value 1");
        Assert.That(ok2, Is.True, "Should add value 2");
        Assert.That(ok3, Is.True, "Should add value 3");
        Assert.That(ok4, Is.True, "Should add value 4");
        
        var found1 = subject.TryGet("one",   out var value1);
        var found2 = subject.TryGet("two",   out var value2);
        var found3 = subject.TryGet("three", out var value3);
        var found4 = subject.TryGet("four",  out var value4);
        
        Assert.That(found1, Is.True, "Should find value 1");
        Assert.That(found2, Is.True, "Should find value 2");
        Assert.That(found3, Is.True, "Should find value 3");
        Assert.That(found4, Is.True, "Should find value 4");
        
        Assert.That(value1, Is.EqualTo("value 1"), "Should find value 1");
        Assert.That(value2, Is.EqualTo("value 2"), "Should find value 2");
        Assert.That(value3, Is.EqualTo("value 3"), "Should find value 3");
        Assert.That(value4, Is.EqualTo("value 4"), "Should find value 4");
    }

    [Test]
    public void fill_a_small_map()
    {
        var subject = new FizzleHashMap<string, string>(8);
        var capacity = subject.Capacity;
        
        Console.WriteLine($"Asked for 8 slots, got {capacity}");

        var added = 0;
        for (int i = 0; i < capacity * 2; i++)
        {
            if (!subject.TryAdd("key"+i,   "value "+i)) break;
            added++;
        }
        
        Assert.That(added, Is.EqualTo(capacity), "Should be able to fill map to capacity");
    }
    
    [Test]
    public void fill_a_larger_map()
    {
        var subject = new FizzleHashMap<string, string>(512);
        var capacity = subject.Capacity;
        
        Console.WriteLine($"Asked for 512 slots, got {capacity}");

        var added = 0;
        for (int i = 0; i < capacity * 2; i++)
        {
            if (!subject.TryAdd("key"+i,   "value "+i)) break;
            added++;
        }
        
        Assert.That(added, Is.EqualTo(capacity), "Should be able to fill map to capacity");
    }

    [Test]
    public void should_fail_to_get_a_key_not_stored_in_a_map()
    {
        var subject = new FizzleHashMap<string, string>(64);
        
        subject.TryAdd("one",   "value 1");
        subject.TryAdd("two",   "value 2");
        subject.TryAdd("three", "value 3");
        subject.TryAdd("four",  "value 4");

        Assert.That(subject.TryGet("one",   out var value1), Is.True, "Should find value 1");
        Assert.That(subject.TryGet("two",   out var value2), Is.True, "Should find value 2");
        Assert.That(subject.TryGet("three", out var value3), Is.True, "Should find value 3");
        Assert.That(subject.TryGet("four",  out var value4), Is.True, "Should find value 4");
        
        Assert.That(value1, Is.EqualTo("value 1"), "Should find value 1");
        Assert.That(value2, Is.EqualTo("value 2"), "Should find value 2");
        Assert.That(value3, Is.EqualTo("value 3"), "Should find value 3");
        Assert.That(value4, Is.EqualTo("value 4"), "Should find value 4");
        
        
        Assert.That(subject.TryGet("un",     out _), Is.False, "Should not find value 1");
        Assert.That(subject.TryGet("dau",    out _), Is.False, "Should not find value 2");
        Assert.That(subject.TryGet("tri",    out _), Is.False, "Should not find value 3");
        Assert.That(subject.TryGet("pedwar", out _), Is.False, "Should not find value 4");
    }

    [Test]
    public void should_be_able_to_delete_a_key()
    {
        
        var subject = new FizzleHashMap<string, string>(512);
        
        var ok1 = subject.TryAdd("one",   "value 1");
        var ok2 = subject.TryAdd("two",   "value 2");
        var ok3 = subject.TryAdd("three", "value 3");
        var ok4 = subject.TryAdd("four",  "value 4");
        
        Assert.That(ok1, Is.True, "Should add value 1");
        Assert.That(ok2, Is.True, "Should add value 2");
        Assert.That(ok3, Is.True, "Should add value 3");
        Assert.That(ok4, Is.True, "Should add value 4");
        
        var del2 = subject.TryDelete("two");
        var del4 = subject.TryDelete("four");
        Assert.That(del2, Is.True, "Should delete value 2");
        Assert.That(del4, Is.True, "Should delete value 4");
        
        var found1 = subject.TryGet("one",   out var value1);
        var found2 = subject.TryGet("two",   out var value2);
        var found3 = subject.TryGet("three", out var value3);
        var found4 = subject.TryGet("four",  out var value4);
        
        Assert.That(found1, Is.True,  "Should find key 1");
        Assert.That(found3, Is.True,  "Should find key 3");
        Assert.That(found2, Is.False, "Should not find key 2");
        Assert.That(found4, Is.False, "Should not find key 4");
        
        Assert.That(value1, Is.EqualTo("value 1"), "Should find value 1");
        Assert.That(value3, Is.EqualTo("value 3"), "Should find value 3");
        Assert.That(value2, Is.Null, "Should not find value 2");
        Assert.That(value4, Is.Null, "Should not find value 4");
    }
}