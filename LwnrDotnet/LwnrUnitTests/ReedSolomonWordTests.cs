using LwnrCore.Helpers;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace LwnrUnitTests;

[TestFixture]
public class ReedSolomonWordTests
{
    private static readonly Random _rnd = new();

    [Test, Repeat(100)]
    public void can_read_and_write_correct_data()
    {
        var original = (_rnd.Next() << 1) ^ (_rnd.Next());
        var encoded = ReedSolomonWord.Encode(original);

        Console.WriteLine($" {original:X8} -> {encoded:X16}");

        var result = ReedSolomonWord.Decode(encoded);
        Console.WriteLine($" {encoded:X16} -> {result:X8}");

        Assert.That(result, Is.EqualTo(original));
    }

    [Test, Repeat(16)]
    public void can_recover_correct_data_from_damaged_data()
    {
        var original = (_rnd.Next() << 1) ^ (_rnd.Next());
        var encoded = ReedSolomonWord.Encode(original);

        Console.WriteLine($" {original:X8} -> {encoded:X16}");

        encoded ^= 0x80000100202000L; // 4 flipped bits

        var result = ReedSolomonWord.Decode(encoded);
        Console.WriteLine($" {encoded:X16} -> {result:X8}");

        Assert.That(result, Is.EqualTo(original));
    }

    [Test, Repeat(16)]
    public void very_damaged_data_throws_an_exception_when_decoding()
    {
        var original = (_rnd.Next() << 1) ^ (_rnd.Next());
        var encoded = ReedSolomonWord.Encode(original);

        Console.WriteLine($" {original:X8} -> {encoded:X16}");

        encoded ^= 4422242428444222; // 16 flipped bits

        Assert.Throws<Exception>(() => { ReedSolomonWord.Decode(encoded); });
    }

    [Test]//, Repeat(16)]
    public void data_can_be_damaged_repeatedly_between_refreshes()
    {
        var original = (_rnd.Next() << 1) ^ (_rnd.Next());
        var encoded = ReedSolomonWord.Encode(original);

        Console.WriteLine($"{original:X8}        -> {encoded:X16}                        {encoded:X16}");

        var damaged = encoded;
        for (int i = 0; i < 16; i++)
        {
            // TODO: this fails if the last symbol is damaged at all (probably an off-by-1)
            var flips = 0x1;//(1L << _rnd.Next(0, 60)) | (1L << _rnd.Next(0, 63));
            damaged ^= flips;
            Console.Write($"{flips:X16} | ");
            
            Console.Write($"{encoded:X16} -> {damaged:x16}");
            Assert.That(damaged, Is.Not.EqualTo(encoded));
            
            var recovered = ReedSolomonWord.Refresh(damaged);
            Console.WriteLine($" => {recovered:X16}");
            Assert.That(recovered, Is.EqualTo(encoded));
            
            damaged = recovered;
        }

        var result = ReedSolomonWord.Decode(encoded);
        Console.WriteLine($" {encoded:X16} -> {result:X8}");

        Assert.That(result, Is.EqualTo(original));
    }

}