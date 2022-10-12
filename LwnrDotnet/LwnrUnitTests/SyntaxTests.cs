using LwnrCore.Parser;
using NUnit.Framework;

namespace LwnrUnitTests;

[TestFixture]
public class SyntaxTests
{
    [Test]
    public void basic_parse_and_render_test()
    {
        const string helloWorld = @"
(log `hello, world`)
";

        var tree = Parser.Parse(helloWorld);

        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.True, "IsValid");
        Assert.That(tree.Type, Is.EqualTo(SyntaxNodeType.Root), "root type");
        Assert.That(tree.Items.Count, Is.EqualTo(1), "root item count");
        Assert.That(tree.Items[0].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[0].Items.Count, Is.EqualTo(2), "leaf item count");

        var rendered = Parser.Render(tree);

        Assert.That(rendered.Trim(), Is.EqualTo(helloWorld.Trim()), "rendered output");
    }

    [Test]
    public void comment_test()
    {
        const string helloWorld = @"
// comment on its own line
(log `hello, world`) // this is a line comment
(ping pong) // different line
";

        var tree = Parser.Parse(helloWorld);

        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.True, "IsValid");
        Assert.That(tree.Type, Is.EqualTo(SyntaxNodeType.Root), "root type");
        Assert.That(tree.Items.Count, Is.EqualTo(5), "root item count");

        var rendered = Parser.Render(tree);

        Assert.That(rendered.Trim(), Is.EqualTo(helloWorld.Trim()), "rendered output");
    }
    
    [Test]
    public void numbers_and_empty_stuff_tests()
    {
        const string helloWorld = @"
(log 22.2 0xfA `` 50'000.000_001 `hello, world`)
";

        var tree = Parser.Parse(helloWorld);

        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.True, "IsValid");
        Assert.That(tree.Type, Is.EqualTo(SyntaxNodeType.Root), "root type");
        Assert.That(tree.Items.Count, Is.EqualTo(1), "root item count");
        Assert.That(tree.Items[0].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[0].Items.Count, Is.EqualTo(6), "leaf item count");

        // Check values. Note that no transforms happen here -- we store exactly what we read.
        var list = tree.Items[0].Items;

        Assert.That(list[0].TokenType, Is.EqualTo(TokenType.Atom));
        Assert.That(list[0].Value, Is.EqualTo("log"));

        Assert.That(list[1].TokenType, Is.EqualTo(TokenType.LiteralNumber));
        Assert.That(list[1].Value, Is.EqualTo("22.2"));

        Assert.That(list[2].TokenType, Is.EqualTo(TokenType.LiteralNumber));
        Assert.That(list[2].Value, Is.EqualTo("0xfA"));

        Assert.That(list[3].TokenType, Is.EqualTo(TokenType.LiteralString));
        Assert.That(list[3].Value, Is.EqualTo(""));

        Assert.That(list[4].TokenType, Is.EqualTo(TokenType.LiteralNumber));
        Assert.That(list[4].Value, Is.EqualTo("50'000.000_001"));

        Assert.That(list[5].TokenType, Is.EqualTo(TokenType.LiteralString));
        Assert.That(list[5].Value, Is.EqualTo("hello, world"));

        var rendered = Parser.Render(tree);

        Assert.That(rendered.Trim(), Is.EqualTo(helloWorld.Trim()), "rendered output");
    }

    [Test]
    public void not_ending_a_string_results_in_a_syntax_error()
    {
        //                                               ↴
        const string helloWorld = "(log warn `hello, world)";

        var tree = Parser.Parse(helloWorld);

        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.False, "IsValid");
    }
    
    [Test]
    public void too_many_open_parens_is_a_syntax_error ()
    {
        const string helloWorld = "(log warn (string.uppercase `hello, world`)";

        var tree = Parser.Parse(helloWorld);

        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.False, "IsValid");
    }
    
    [Test]
    public void too_many_close_parens_is_a_syntax_error ()
    {
        const string helloWorld = "(log (warn))) (string.uppercase `hello, world`)))";

        var tree = Parser.Parse(helloWorld);

        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.False, "IsValid");
    }
    
    [Test]
    public void can_handle_more_complex_structures()
    {
        const string nonTrivial = @"
(def is-even (value result) // number -> maybe
    (if (= 0 (value % 2))
        then:(result set true)
        else:(result clear)
    )
)

(def pick-even (source dest)
    (alias cursor (source items))
    (set item new@maybe)
    (while (cursor item)
        (set keep new@maybe)
        (is-even item keep)
        (if keep
            (dest add copy@item)
        )
    )
)
";

        var tree = Parser.Parse(nonTrivial);

        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.True, "IsValid");
        Assert.That(tree.Type, Is.EqualTo(SyntaxNodeType.Root), "root type");
        Assert.That(tree.Items.Count, Is.EqualTo(2), "root item count");
        
        Assert.That(tree.Items[0].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[0].Items.Count, Is.EqualTo(5), "leaf item count");
        
        Assert.That(tree.Items[1].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[1].Items.Count, Is.EqualTo(6), "leaf item count");

        // TODO: indent in render
        Console.WriteLine(Parser.Render(tree));
        //Assert.That(rendered.Trim(), Is.EqualTo(nonTrivial.Trim()), "rendered output");
    }

}