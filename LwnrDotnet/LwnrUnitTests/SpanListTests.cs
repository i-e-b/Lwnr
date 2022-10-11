using LwnrCore.Types;
using NUnit.Framework;

namespace LwnrUnitTests;

[TestFixture]
public class SpanListTests
{
    [Test]
    public void can_store_and_retrieve_data_in_one_block()
    {
        var memory = new Arena();
        var subject = new SpanList(memory);
        
        // before anything added
        Assert.That(subject.Count(), Is.EqualTo(0));
        
        // can't read or write until we've added a chunk
        var ok = subject.Read(out var value);
        Assert.That(ok, Is.False);
        Assert.That(value.IsZero, Is.True);
        
        ok = subject.Write(new Span(memory, 1, 2));
        Assert.That(ok, Is.False);
        
        // Add a chunk
        subject.AddChunk();
        
        // Write some data
        ok = subject.Write(new Span(memory, 1, 2));
        Assert.That(ok, Is.True);
        ok = subject.Write(new Span(memory, 2, 2));
        Assert.That(ok, Is.True);
        
        // Seek back
        ok = subject.Seek(0);
        Assert.That(ok, Is.True);
        
        // Read the data
        ok = subject.Read(out value);
        Assert.That(ok, Is.True);
        Assert.That(value.Start, Is.EqualTo(1));
        Assert.That(value.End, Is.EqualTo(2));
        
        ok = subject.Read(out value);
        Assert.That(ok, Is.True);
        Assert.That(value.Start, Is.EqualTo(2));
        Assert.That(value.End, Is.EqualTo(3));
        
        Assert.That(memory.Data.Count, Is.EqualTo((SpanList.BlockSize * sizeof(uint) * 2)+ sizeof(uint)));
    }

    [Test]
    public void can_write_and_read_across_multiple_blocks()
    {
        var memory = new Arena();
        var subject = new SpanList(memory);

        bool ok;
        var max = SpanList.BlockSize * 3;
        for (uint i = 0; i < max; i++)
        {
            if (subject.Count() <= i) subject.AddChunk();
            ok = subject.Write(new Span(memory, i+1, 1));
            Assert.That(ok, Is.True, $"write {i}");
        }
        
        ok = subject.Seek(0);
        Assert.That(ok, Is.True, "seek");
        
        for (uint i = 0; i < max; i++)
        {
            try
            {
                ok = subject.Read(out var value);
                Assert.That(ok, Is.True, $"read index {i}");
                Assert.That(value.Start, Is.EqualTo(i + 1), $"read index {i}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"did not read index {i}; {subject} {ex}");
            }
        }

        Assert.That(memory.Data.Count, Is.EqualTo((SpanList.BlockSize * sizeof(uint)*2 * 3) + (sizeof(uint)*3)));
    }

    [Test]
    public void can_shuffle_back_entries()
    {
        var memory = new Arena();
        var subject = new SpanList(memory);

        var max = SpanList.BlockSize * 3;
        for (uint i = 0; i < max; i++)
        {
            if (subject.Count() <= i) subject.AddChunk();
            subject.Write(new Span(memory, i+1, 2));
        }
        
        subject = subject.Sublist(1); // chop off first chunk
        subject.Seek(0); // which should be the same as BlockSize in original list 
        
        for (uint i = SpanList.BlockSize; i < max; i++)
        {
            var ok = subject.Read(out var value);
            Assert.That(ok, Is.True, $"read index {i}");
            Assert.That(value.Start, Is.EqualTo(i + 1), $"read index {i} start");
            Assert.That(value.End, Is.EqualTo(i + 2), $"read index {i} end");
        }
    }
}