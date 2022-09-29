using LwnrCore;
using NUnit.Framework;

namespace LwnrUnitTests;

[TestFixture]
public class SyntaxTests
{
    [Test]
    public void basic_parse_and_render_test()
    {
        const string helloWorld= @"
(log `hello, world`)
";
        
        var tree = Parser.Parse(helloWorld);
        
        Assert.That(tree.IsValid, Is.True, "IsValid");
        Assert.That(tree.Type, Is.EqualTo(SyntaxNodeType.Root), "root type");
        Assert.That(tree.Items.Count, Is.EqualTo(1), "root item count");
        Assert.That(tree.Items[0].Type, Is.EqualTo(SyntaxNodeType.List), "leaf item type");
        Assert.That(tree.Items[0].Items.Count, Is.EqualTo(2), "leaf item count");
        
        var rendered = Parser.Render(tree);
        
        Assert.That(rendered.Trim(), Is.EqualTo(helloWorld.Trim()), "rendered output");
        
        Assert.Inconclusive("not implemented");
    }
}