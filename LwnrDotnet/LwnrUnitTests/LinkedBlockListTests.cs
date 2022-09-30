using LwnrCore.Types;
using NUnit.Framework;

namespace LwnrUnitTests;

[TestFixture]
public class LinkedBlockListTests
{
    [Test]
    public void can_store_and_retrieve_data_in_one_block()
    {
        var memory = new Arena();
        var subject = new LinkedBlockList32(memory);
        
        // before anything added
        Assert.That(subject.Count(), Is.EqualTo(0));
        
        // can't read or write until we've added a chunk
        var ok = subject.Read(out var value);
        Assert.That(ok, Is.False);
        Assert.That(value, Is.EqualTo(0));
        
        ok = subject.Write(1);
        Assert.That(ok, Is.False);
        
        // Add a chunk
        subject.AddChunk();
        
        // Write some data
        ok = subject.Write(1);
        Assert.That(ok, Is.True);
        ok = subject.Write(2);
        Assert.That(ok, Is.True);
        
        // Seek back
        ok = subject.Seek(0);
        Assert.That(ok, Is.True);
        
        // Read the data
        ok = subject.Read(out value);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(1));
        
        ok = subject.Read(out value);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(2));
        
        Assert.That(memory.Data.Count, Is.EqualTo(LinkedBlockList32.BlockSize + 1));
    }

    [Test]
    public void can_write_and_read_across_multiple_blocks()
    {
        var memory = new Arena();
        var subject = new LinkedBlockList32(memory);

        bool ok;
        var max = LinkedBlockList32.BlockSize * 3;
        for (uint i = 0; i < max; i++)
        {
            if (subject.Count() <= i) subject.AddChunk();
            ok = subject.Write(i+1);
            Assert.That(ok, Is.True, $"write {i}");
        }
        
        ok = subject.Seek(0);
        Assert.That(ok, Is.True, "seek");
        
        for (uint i = 0; i < max; i++)
        {
            ok = subject.Read(out var value);
            Assert.That(ok, Is.True, $"read {i}");
            Assert.That(value, Is.EqualTo(i+1), $"read {i}");
        }

        Assert.That(memory.Data.Count, Is.EqualTo((LinkedBlockList32.BlockSize + 1) * 3));
    }

}