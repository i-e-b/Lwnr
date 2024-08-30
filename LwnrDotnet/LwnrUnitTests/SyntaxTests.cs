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
    public void colon_marks_a_named_argument()
    {
        // 'if' taking 3 arguments: predicate, then, else.
        // With a prefix like 'name:', we allow slightly
        // clearer syntax, and changing argument order
        const string namedArgCall = @"
(if myPredicate
    then: (result set true)
    else: (result clear)
)
";
        var tree = Parser.Parse(namedArgCall);

        Console.WriteLine(Parser.Render(tree));
        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.True, "IsValid");
        Assert.That(tree.Type, Is.EqualTo(SyntaxNodeType.Root), "root type");
        Assert.That(tree.Items.Count, Is.EqualTo(1), "root item count");
        
        var ifList = tree.Items[0];
        Assert.That(ifList.Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(ifList.Items.Count, Is.EqualTo(4+3), "leaf item count"); // 4 items, 3 new-lines
        
        var ifName = ifList.Items[0];
        Assert.That(ifName.Type, Is.EqualTo(SyntaxNodeType.Token), "target type");
        Assert.That(ifName.TokenType, Is.EqualTo(TokenType.Atom), "target sub-type");
        Assert.That(ifName.Value, Is.EqualTo("if"), "target value");
        
        var predName = ifList.Items[1];
        Assert.That(predName.Type, Is.EqualTo(SyntaxNodeType.Token), "predicate type");
        Assert.That(predName.TokenType, Is.EqualTo(TokenType.Atom), "predicate sub-type");
        Assert.That(predName.Value, Is.EqualTo("myPredicate"), "predicate value");
        
        var thenList = ifList.Items[3];
        Assert.That(thenList.Type, Is.EqualTo(SyntaxNodeType.List), "then type");
        Assert.That(thenList.Label, Is.EqualTo("then"), "then label");
        Assert.That(thenList.Items.Count, Is.EqualTo(3), "then value");
        
        var elseList = ifList.Items[5];
        Assert.That(elseList.Type, Is.EqualTo(SyntaxNodeType.List), "else type");
        Assert.That(elseList.Label, Is.EqualTo("else"), "else label");
        Assert.That(elseList.Items.Count, Is.EqualTo(2), "else value");
        
        var rendered = Parser.Render(tree);
        Console.WriteLine(rendered);
        Assert.That(FixLines(rendered), Is.EqualTo(FixLines(namedArgCall)), "rendered output");
    }

    [Test]
    public void can_handle_more_complex_structures()
    {
        const string nonTrivial = @"
(def is-even (value result) // number -> maybe
    (if (= 0 (value % 2))
        then: (result set true)
        else: (result clear)
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
        Assert.That(tree.Items.Count, Is.EqualTo(3), "root item count"); // two defs and a new-line
        
        Assert.That(tree.Items[0].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[0].Items.Count, Is.EqualTo(5+1), "leaf item count"); // 5 items + new-line
        
        Assert.That(tree.Items[2].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[2].Items.Count, Is.EqualTo(6+4), "leaf item count"); // 6 items + 4 new-lines

        // check indent in render
        var rendered = Parser.Render(tree);
        Console.WriteLine(rendered);
        Assert.That(FixLines(rendered), Is.EqualTo(FixLines(nonTrivial)), "rendered output");
    }

    [Test]
    public void stack_quotes_are_parsed()
    {
        const string stackQuote = @"
(def stackFunction {
        // Braces '{}' mark a 'stack quote'.
        // If the FIRST item after a function def name
        // is a stack quote, then that IS the function,
        // and there are no explicit parameters.
        // this function is accessible either in other
        // stack quotes, or when 'applying' a function
        // to a list of data.

        1 swap - // return (1 - input)
    }
)
(def mainlyMath (x y out:z)
    (new stack s)
    (apply s {x y /}) // push x, push y, pop2 & push x/y
    (set z s 0) // z := s[0] == x/y
)
";
        
        var tree = Parser.Parse(stackQuote);
        Console.WriteLine(string.Join(", ", tree.Reasons));

        Console.WriteLine(tree.Describe());

        Assert.That(tree.IsValid, Is.True, "IsValid");
        Assert.That(tree.Type, Is.EqualTo(SyntaxNodeType.Root), "root type");
        Assert.That(tree.Items.Count, Is.EqualTo(2+1), "root item count"); // 2 lists, 1 new-line
        
        Assert.That(tree.Items[0].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[0].Items.Count, Is.EqualTo(3+1), "leaf item count"); // 3 things, 1 new-line
        
        Assert.That(tree.Items[2].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[2].Items.Count, Is.EqualTo(6+2+2), "leaf item count"); // 5 lists, 2 comments, 2 new-lines
        
        var rendered = Parser.Render(tree);
        Console.WriteLine(rendered);
        Assert.That(FixLines(rendered), Is.EqualTo(FixLines(stackQuote)), "rendered output");
    }
    
    private string FixLines(string src) => src.Replace("\r","").Trim();
}