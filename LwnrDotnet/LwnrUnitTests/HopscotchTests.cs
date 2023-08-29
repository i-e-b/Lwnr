using NUnit.Framework;
using HopscotchForth;
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute

namespace LwnrUnitTests;

/// <summary>
/// A quick experiment based on
/// https://gist.github.com/i-e-b/e91e28a61b55ad88aed0e4944a7f1b80
/// </summary>
[TestFixture]
public class HopscotchTests
{
    [Test]
    public void hopscotch_reorders_correctly()
    {
        var result = Lang.ParseAndReorder(@"
    if (x >=(4)) then (x /(2)) else (x *(2))
");
        
        Console.WriteLine(string.Join("; ", result));
        
        Assert.That(result, Is.EqualTo(new []{
            "x", "4", ">=", "if", "x", "2", "/", "then", "x", "2", "*", "else"
        }).AsCollection);
    }
    
    [Test]
    public void run_a_simple_program()
    {
        var program = Lang.ParseAndReorder(@"
    if (x >=(4)) then (x /(2)) else (x *(2))
");
        
        Console.WriteLine(string.Join("; ", program));
        
        var vars = new Dictionary<string, double> { { "x", 0.0 } };
        
        Console.WriteLine("========== run 1 ==========");
        vars["x"] = 12.0;
        var result = Lang.Run(program, vars);
        Assert.That(result.Pop(), Is.EqualTo(6.0));
        
        
        Console.WriteLine("========== run 2 ==========");
        vars["x"] = 2.5;
        result = Lang.Run(program, vars);
        Assert.That(result.Pop(), Is.EqualTo(5.0));
    }

    [Test]
    [TestCase("10.1 if (z >(x *(y))) then (1.01) endif ", 1.01)]
    [TestCase("10.1 if (x >(z *(y))) then (1.01) endif ", 10.1)]
    [TestCase("if (x < (y)) then (5) else (6)", 5)]
    [TestCase("if (x <= (y)) then (5) else (6)", 5)]
    [TestCase("if (x >= (y)) then (5) else (6)", 6)]
    [TestCase("x y >= y if 5 then 6 else", 6)]
    public void program_examples(string programStr, double expected)
    {
        var program = Lang.ParseAndReorder(programStr);
        
        Console.WriteLine(string.Join("; ", program));
        
        var vars = new Dictionary<string, double> {
            { "x",  1.0 },
            { "y",  5.0 },
            { "z", 12.3 }
        };
        
        var result = Lang.Run(program, vars);
        Assert.That(result.Pop(), Is.EqualTo(expected));
    }

}