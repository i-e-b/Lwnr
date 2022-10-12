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
    (stdout log `hello, world`)
)
";

        var tree = Parser.Parse(basicProgram);
        var subject = new Compiler(tree);
        var ir = subject.Compile();
        
        Assert.That(ir, Is.Not.Null);
        Assert.That(ir.Instructions.Count, Is.Not.Zero);
        
        Console.Write(ir.Describe());
    }
}