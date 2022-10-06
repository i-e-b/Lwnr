using LwnrCore.Types;
using NUnit.Framework;

namespace LwnrUnitTests;

[TestFixture]
public class ArenaAndPointerTests
{
    [Test]
    public void can_create_pointers_to_blocks_of_memory_and_manipulate_them()
    {
        var memory = new Arena();

        var block = memory.Allocate(1024);
        Assert.That(block.Size(), Is.EqualTo(1024));

        // WRITING WORDS
        block.SetUInt32Idx(0, 0xff805501);
        block.SetUInt32Idx(1, 0x22334455);
        block.SetUInt32Idx(2, 0x66778899);

        // READING BYTES
        // inside first word
        var bytes = block.ReadBytes(1, 4);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x80, 0x55, 0x01 }).AsCollection);

        bytes = block.ReadBytes(0, 4);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xFF, 0x80, 0x55, 0x01 }).AsCollection);

        bytes = block.ReadBytes(0, 3);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xFF, 0x80, 0x55 }).AsCollection);
        
        // inside second word
        bytes = block.ReadBytes(4, 7);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x22, 0x33, 0x44 }).AsCollection);

        bytes = block.ReadBytes(4, 8);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x22, 0x33, 0x44, 0x55 }).AsCollection);

        bytes = block.ReadBytes(5, 8);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x33, 0x44, 0x55 }).AsCollection);
        
        // across two words
        bytes = block.ReadBytes(2, 6);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x55, 0x01, 0x22, 0x33 }).AsCollection);
        
        // across multiple words
        bytes = block.ReadBytes(2, 10);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x55, 0x01, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 }).AsCollection);
        
        
        // WRITING BYTES
        bytes = new byte[]{0x80,0x90,0xA0};
        
        // single word
        block.Write(bytes, 0);
        Assert.That(block.GetUInt32Idx(0), Is.EqualTo(0x8090A001));
        
        // crossing 2 words
        block.Write(bytes, 2);
        Assert.That(block.GetUInt32Idx(0), Is.EqualTo(0x80908090));
        Assert.That(block.GetUInt32Idx(1), Is.EqualTo(0xA0334455));
        
        // across multiple words
        bytes = new byte[]{0x01,0x02,0x03,0x04,0x05,0x06,0x07,0x08};
        block.Write(bytes, 2);
        Assert.That(block.GetUInt32Idx(0), Is.EqualTo(0x80900102));
        Assert.That(block.GetUInt32Idx(1), Is.EqualTo(0x03040506));
        Assert.That(block.GetUInt32Idx(2), Is.EqualTo(0x07088899));
    }
}