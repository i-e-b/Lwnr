using System.Text;
using LwnrCore.Containers;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace LwnrUnitTests;

[TestFixture]
public class SegmentedStackTests
{
    [Test]
    public void basic_multi_call_example()
    {
        var lastDeferred = "none";
        var subject = new SegmentedStack();
        
        // Base data
        var rOne = subject.PushData(str("one"));
        
        // Prep 1st call
        subject.Call();
        subject.PushReference(rOne);
        var rTwo = subject.PushData(str("static param"));
        
        // Run function - get params
        Assert.That(subject.PeekIndex(0, out var refP1), Is.True);
        Assert.That(str(refP1), Is.EqualTo("one"));
        Assert.That(subject.PeekIndex(1, out var refP2), Is.True);
        Assert.That(str(refP2), Is.EqualTo("static param"));
        
        // push a defer action
        subject.PushDeferredCall(() => { lastDeferred = "one"; });
        
        // Prep 2nd call
        subject.Call();
        subject.PushReference(rOne);
        subject.PushReference(rTwo);
        
        // Run function - get params
        Assert.That(subject.PeekIndex(0, out var refP3), Is.True); // two calls down
        Assert.That(str(refP3), Is.EqualTo("one"));
        Assert.That(subject.PeekIndex(1, out var refP4), Is.True); // one call down
        Assert.That(str(refP4), Is.EqualTo("static param"));
        
        // push a defer action
        subject.PushDeferredCall(() => { lastDeferred = "two"; });

        // Unwind 2nd call
        Assert.That(subject.Return(out var def1), Is.True);
        
        // check deferred calls
        Assert.That(def1.Count, Is.EqualTo(1));
        def1[0]();
        Assert.That(lastDeferred, Is.EqualTo("two"));
        
        
        // Unwind 1st call
        Assert.That(subject.Return(out var def2), Is.True);
        
        // check deferred calls
        Assert.That(def2.Count, Is.EqualTo(1));
        def2[0]();
        Assert.That(lastDeferred, Is.EqualTo("one"));
        
        // Unwind process
        Assert.That(subject.Return(out var def3), Is.False);
        Assert.That(def3.Count, Is.EqualTo(0));
    }

    private static byte[] str(string? s) => s is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(s);
    private static string str(byte[]? b) => b is null ? "<null>" : Encoding.UTF8.GetString(b);
}