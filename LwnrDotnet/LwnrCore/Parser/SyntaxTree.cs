using System.Text;

namespace LwnrCore.Parser;

/// <summary>
/// Syntax tree for the parser and interpreter
/// </summary>
public class SyntaxTree
{
    /// <summary>
    /// Create a SyntaxTree root node.
    /// Non-root nodes should be created with <see cref="AddListNode"/>
    /// or <see cref="AddToken"/>
    /// </summary>
    public SyntaxTree()
    {
        Type = SyntaxNodeType.Root;
        TokenType = TokenType.Invalid;
        Parent = null;
        Value = null;
        IsValid = true;
    }

    /// <summary>
    /// True if syntax contained no errors
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// What does this node represent?
    /// </summary>
    public SyntaxNodeType Type { get; private init; }

    /// <summary>
    /// Token value of this node
    /// </summary>
    public string? Value { get; private init; }
    
    /// <summary>
    /// A label added to this node, if any
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Type of token, if any
    /// </summary>
    public TokenType TokenType { get; private init; }
    
    /// <summary>
    /// Child nodes
    /// </summary>
    public List<SyntaxTree> Items { get; } = new();

    /// <summary>
    /// The parent node of this, or null if we're root
    /// </summary>
    public SyntaxTree? Parent { get; private init; }

    /// <summary>
    /// Create a new node that is a child of this one.
    /// </summary>
    public SyntaxTree AddListNode(int start, string token, string? label)
    {
        var tokenType = TokenType.Invalid;
        switch (token)
        {
            case "(": 
                tokenType = TokenType.CodeQuote;
                break;
            
            case "{":
                tokenType = TokenType.StackQuote;
                break;
        }

        var child = new SyntaxTree{
            Parent = this,
            Type = SyntaxNodeType.List,
            TokenType = tokenType,
            Label = label,
            Start = start
        };
        Items.Add(child);
        return child;
    }

    /// <summary>
    /// Add a new leaf node with a token value and type,
    /// to represent program data.
    /// </summary>
    public void AddToken(string token, TokenType type, int start, int end, string? label)
    {
        var child = new SyntaxTree{
            Parent = this,
            Type = SyntaxNodeType.Token,
            Value = token,
            Label = label,
            TokenType = type,
            Start = start,
            End = end
        };
        Items.Add(child);
    }

    /// <summary>
    /// Position in input that this token ended
    /// </summary>
    public int End { get; set; }

    /// <summary>
    /// Position in input that this token started
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// <see cref="Items"/> with meta-information (comments etc.) filtered out.
    /// </summary>
    public IEnumerable<SyntaxTree> ProgramItems =>  Items.Where(i=>i.Type != SyntaxNodeType.Meta);

    /// <summary>
    /// Parser/Compiler error messages
    /// </summary>
    public List<string> Reasons { get; } = new();

    /// <summary>
    /// Add a new leaf node with a token value and type,
    /// to represent non-program data such as whitespace and
    /// comments
    /// </summary>
    public void AddMeta(string token, TokenType type, int start, int end)
    {
        var child = new SyntaxTree{
            Parent = this,
            Type = SyntaxNodeType.Meta,
            Value = token,
            TokenType = type,
            Start = start,
            End = end
        };
        Items.Add(child);
    }

    /// <summary>
    /// Generate a human-readable debug representation of the whole syntax tree from this point
    /// </summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        DescribeRecursive(this, sb, 0);
        return sb.ToString();
    }

    private void DescribeRecursive(SyntaxTree node, StringBuilder sb, int depth)
    {
        sb.AppendLine();
        if (depth > 0) sb.Append(' ', depth * 2);
        sb.Append(node.Type.ToString());
        if (node.Type == SyntaxNodeType.List)
        {
            sb.Append(' ');
            sb.Append(node.Items.Count.ToString());
        }

        if (node.Value is not null)
        {
            sb.Append($" {node.TokenType.ToString()}: {node.Value}");
        }

        if (node.Label is not null)
        {
            sb.Append($" label={node.Label}");
        }

        foreach (var item in node.Items)
        {
            DescribeRecursive(item, sb, depth + 1);
        }
    }
    
    /// <summary>
    /// Returns true if this tree node is an atom with the given name
    /// </summary>
    public bool IsAtom(string name) => Type == SyntaxNodeType.Token && TokenType == TokenType.Atom && Value == name;

    /// <summary>
    /// Describe the node position in the input
    /// </summary>
    public string Position() => End > Start ? $"{Start}..{End}" : $"{Start}..?";

}

/// <summary>
/// What does a node in the syntax tree represent
/// </summary>
public enum SyntaxNodeType
{
    /// <summary>
    /// Root of a parsed document
    /// </summary>
    Root,
    
    /// <summary>
    /// An S-Expression list
    /// </summary>
    List,
    
    /// <summary>
    /// Some other kind of token
    /// </summary>
    Token,
    
    /// <summary>
    /// Whitespace, comments, etc
    /// </summary>
    Meta
}