using LwnrCore.Interpreter;
using LwnrCore.Parser;
using NUnit.Framework;

namespace LwnrUnitTests;

[TestFixture]
public class InterpreterTests
{
    [Test]
    public void program_must_have_main()
    {
        const string basicProgram = @"(nothing here)";

        var tree = Parser.Parse(basicProgram);
        var err = Assert.Throws<Exception>(() =>
        {
            var _ = new BasicInterpreter(tree);
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
        var subject = new BasicInterpreter(tree);
        
        int i;
        var complete = false;
        for (i = 0; i < 1000; i++)
        {
            if (!subject.Step())
            {
                complete = true;
                break;
            }
        }
        
        Assert.That(complete, Is.True, "ran to completion");
        
        var result = subject.ReadFromOutput();
        Assert.That(result, Is.EqualTo("hello, world"));
    }
}