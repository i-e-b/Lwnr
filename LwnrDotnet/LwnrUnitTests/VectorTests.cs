using LwnrCore.Types;
using NUnit.Framework;

namespace LwnrUnitTests;

[TestFixture]
public class VectorTests
{
    [Test]
    public void create_push_and_get_length()
    {
        var memory = new Arena();
        var subject = new Vec(memory, 2);
        
        Assert.That(subject.Count(), Is.EqualTo(0));
        
        subject.Push(new byte[]{0x80,0x7f});
        
        Assert.That(subject.Count(), Is.EqualTo(1));
        
        var found = subject.Pop(out var result);
        Assert.That(found, Is.True);
        Assert.That(result, Is.EqualTo(new byte[]{0x80,0x7f}).AsCollection);
        
        Assert.That(subject.Count(), Is.EqualTo(0));
    }
}