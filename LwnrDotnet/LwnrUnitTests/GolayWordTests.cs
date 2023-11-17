using System.Text;
using LwnrCore.Helpers;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace LwnrUnitTests;

[TestFixture]
public class GolayWordTests
{
    
    private static readonly Random _rnd = new();

    [Test]
    public void can_read_and_write_correct_data()
    {
        var original = Encoding.UTF8.GetBytes("Hello, world");
        var encoded = GolayCodec.Encode(original, out var length);

        Console.WriteLine($"({length}) {Encoding.UTF8.GetString(original)} -> {Convert.ToHexString(encoded)}");

        var result = GolayCodec.Decode(encoded, out var errs);
        Console.WriteLine($"({errs}) {Convert.ToHexString(encoded)} -> {Encoding.UTF8.GetString(result)}");

        Assert.That(result, Is.EqualTo(original).AsCollection);
    }

    [Test, Repeat(16)]
    public void can_recover_correct_data_from_damaged_data()
    {
        var original = Encoding.UTF8.GetBytes("Hello, world");
        var encoded = GolayCodec.Encode(original, out var length);

        for (int i = 0; i < 3; i++)
        {
            var b = _rnd.Next(0, length);
            encoded[b] = (byte)(encoded[b] ^ (1 << _rnd.Next(0,8)));
        }
        Console.WriteLine($"({length}) {Encoding.UTF8.GetString(original)} -> {Convert.ToHexString(encoded)}");

        var result = GolayCodec.Decode(encoded, out var errs);
        Console.WriteLine($"({errs}) {Convert.ToHexString(encoded)} -> {Encoding.UTF8.GetString(result)}");

        Assert.That(result, Is.EqualTo(original).AsCollection);
    }
}