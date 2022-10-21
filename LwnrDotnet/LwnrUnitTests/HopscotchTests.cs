using NUnit.Framework;
using HopscotchForth;
// ReSharper disable InconsistentNaming

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

}