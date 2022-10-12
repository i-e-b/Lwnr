using LwnrCore.Compiler;
using LwnrCore.Parser;
using NUnit.Framework;

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
            var _ = new Compiler(tree);
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
        var subject = new Compiler(tree);
        var ir = subject.Compile();
        
        Assert.That(ir, Is.Not.Null);
        Assert.That(ir.Instructions.Count, Is.Not.Zero);
        
        Console.Write(ir.Describe());
        Assert.Inconclusive("needs more test");
    }
    
    [Test]
    public void positional_arguments()
    {
        const string basicProgram = @"
(def test (one two three))
(def main ()
    (test three:`three` two:`two` one:(reverse `eno`))
)
";

        var tree = Parser.Parse(basicProgram);
        var subject = new Compiler(tree);
        var ir = subject.Compile();
        
        Console.Write(ir.Describe());
        
        Assert.That(ir, Is.Not.Null);
        Assert.That(ir.Instructions.Count, Is.Not.Zero);
        
        // TODO: the call to 'test' should have arguments indexed correctly
        // TODO: define what happens with "(test `one` three:`three` `two`)"?
        Assert.Inconclusive("needs more test");
    }
}