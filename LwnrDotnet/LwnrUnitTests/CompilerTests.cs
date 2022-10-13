using LwnrCore.Compiler;
using LwnrCore.Parser;
using NUnit.Framework;
#pragma warning disable CS8602

namespace LwnrUnitTests;

[TestFixture]
public class CompilerTests
{
    [Test]
    public void program_must_have_main()
    {
        const string basicProgram = @"(nothing here)";

        var tree = Parser.Parse(basicProgram);
        var err = Assert.Throws<Exception>(() =>
        {
            var _ = new CompilerPass1(tree);
        });
        
        Assert.That(err?.Message, Contains.Substring("must have 'main' function"));
    }

    [Test]
    public void basic_program()
    {
        const string basicProgram = @"
(def main (stdin stdout)
    (log stdout `hello, world`)
)
";

        var tree = Parser.Parse(basicProgram);
        var subject = new CompilerPass1(tree);
        var ir = subject.Compile();
        
        Assert.That(ir, Is.Not.Null);
        Assert.That(ir.Instructions.Count, Is.Not.Zero);
        
        Console.Write(ir.Describe());
        Assert.Inconclusive("needs more test");
    }
    
    [Test]
    public void quotation_arguments()
    {
        // As we can't return from a function,
        // there is no reason to evaluate a function to
        // get an argument value. Instead, we treat them
        // as quotations, which can either be edited with
        // macros (at compile time) or run as lambdas.
        
        // IEB Note: We need *something* to get values out of expressions
        // Like `(set c (< a b))` below.
        // `(< c a b)` would be fine, but then all kinds of math
        // become really gnarly- `c=(a*2)+(b*3)` -> `(int t1)(int t2)(* t1 a 2)(* t2 b 3)(+ c t1 t2)`
        // Maybe a math-quote would work?
        // `(set c [a*2 + b*3])`
        
        const string basicProgram = @"
(def sort (list comparer start end)
    // do a sort...
)
(def main (stdin stdout)
    (new list myList 8 5 4 6 9 7 1 3 2)
    (
        sort myList (\ (a b c) (set c (< a b)) )
    )
    (log stdout `hello, world`)
)
";

        var tree = Parser.Parse(basicProgram);
        var subject = new CompilerPass1(tree);
        var ir = subject.Compile();
        
        Assert.That(ir, Is.Not.Null);
        Assert.That(ir.Instructions.Count, Is.Not.Zero);
        
        Console.Write(ir.Describe());
        Assert.Inconclusive("needs more test");
    }
    
    [Test]
    public void positional_arguments()
    {
        // TODO: define what happens with "(test `one` three:`three` `two`)"?
        
        const string basicProgram = @"
(def test (one two three))
(def main ()
    (test three:`three` two:`two` one:`eno`)
)
";

        var tree = Parser.Parse(basicProgram);
        var subject = new CompilerPass1(tree);
        var ir = subject.Compile();
        
        Console.Write(ir.Describe());
        
        Assert.That(ir, Is.Not.Null);
        Assert.That(ir.Instructions.Count, Is.Not.Zero);
        
        // The call to 'test' should have arguments indexed correctly
        var callThree = ir.Instructions.SingleOrDefault(i=>i.Command == IrCmdType.ValArg && i.Arguments[0]=="three");
        Assert.That(callThree.Arguments[1], Is.EqualTo("2"));
        
        var callTwo = ir.Instructions.SingleOrDefault(i=>i.Command == IrCmdType.ValArg && i.Arguments[0]=="two");
        Assert.That(callTwo.Arguments[1], Is.EqualTo("1"));
        
        var callOne = ir.Instructions.SingleOrDefault(i=>i.Command == IrCmdType.ValArg && i.Arguments[0]=="eno");
        Assert.That(callOne.Arguments[1], Is.EqualTo("0"));
    }
}