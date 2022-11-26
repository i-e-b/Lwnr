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
(def log ())
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
        
        Assert.That(ir.FunctionOffsets.ContainsKey("main"));
        Assert.That(ir.Instructions.Single(i=>i.Command==IrCmdType.RefArg).Arguments[0], Is.EqualTo("stdout"));
        Assert.That(ir.Instructions.Single(i=>i.Command==IrCmdType.ValArg).Arguments[0], Is.EqualTo("hello, world"));
    }
    
    [Test]
    public void positional_arguments()
    {
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
    
    [Test]
    public void partial_positional_arguments()
    {
        // Any unlabeled arguments go in order from pos 0 up,
        // and skip any positions taken by a labeled argument.
        
        const string basicProgram = @"
(def test (one two three))
(def main ()
    (test three:`three` `one` `two`)
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
        
        var callOne = ir.Instructions.SingleOrDefault(i=>i.Command == IrCmdType.ValArg && i.Arguments[0]=="one");
        Assert.That(callOne.Arguments[1], Is.EqualTo("0"));
    }
    
    [Test]
    public void quotation_arguments()
    {
        // As we can't return from a function,
        // there is no reason to evaluate a function to
        // get an argument value. Instead, we treat them
        // as quotations, which can either be edited with
        // macros (at compile time) or run as lambdas.
        
        // Have a special quotation for an RPN syntax, and pass a stack container into it.
        // See the 'HopscotchForth' for a base.
        // so math quote would be like
        // `(new stack s)(rpn s [a 2 * b 3 * +])(set c s@0)`
        // IEB: Would also be good to have a big macro to do this: https://github.com/glathoud/flatorize
        
        const string basicProgram = @"
(def sort (list comparer start end)
    // do a sort...
)
(def main (stdin stdout)
    (new list myList 8 5 4 6 9 7 1 3 2)
    (
        sort myList (\ (a b c) (< a b c) )
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
        
        // -------------------
        // The 'sort' function
        // -------------------
        var sortOffset = ir.FunctionOffsets["sort"];
        Assert.That(sortOffset, Is.GreaterThanOrEqualTo(0));
        
        // No 'new' in this function, so we should go straight to args
        Assert.That(ir.Instructions[sortOffset+0].Command, Is.EqualTo(IrCmdType.AliasParam));
        Assert.That(ir.Instructions[sortOffset+0].Arguments[0], Is.EqualTo("list"));
        
        Assert.That(ir.Instructions[sortOffset+1].Command, Is.EqualTo(IrCmdType.AliasParam));
        Assert.That(ir.Instructions[sortOffset+1].Arguments[0], Is.EqualTo("comparer"));
        
        Assert.That(ir.Instructions[sortOffset+2].Command, Is.EqualTo(IrCmdType.AliasParam));
        Assert.That(ir.Instructions[sortOffset+2].Arguments[0], Is.EqualTo("start"));
        
        Assert.That(ir.Instructions[sortOffset+3].Command, Is.EqualTo(IrCmdType.AliasParam));
        Assert.That(ir.Instructions[sortOffset+3].Arguments[0], Is.EqualTo("end"));
        
        // No body, so 'end'
        Assert.That(ir.Instructions[sortOffset+4].Command, Is.EqualTo(IrCmdType.EndOfFunction));
        Assert.That(ir.Instructions[sortOffset+4].Arguments, Is.Empty);
        
        
        // -------------------
        // The 'main' function
        // -------------------
        var mainOffset = ir.FunctionOffsets["main"];
        Assert.That(mainOffset, Is.GreaterThanOrEqualTo(0));
        
        // We have at least one 'new', so this function must push a scope
        Assert.That(ir.Instructions[mainOffset+0].Command, Is.EqualTo(IrCmdType.PushScope));
        
        Assert.Inconclusive("needs more test\r\n");
    }
}