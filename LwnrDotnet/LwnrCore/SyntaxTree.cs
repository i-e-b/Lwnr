namespace LwnrCore;

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
    public SyntaxNodeType Type { get; set; }

    /// <summary>
    /// Token value of this node
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Type of token, if any
    /// </summary>
    public TokenType TokenType { get; set; }
    
    /// <summary>
    /// Child nodes
    /// </summary>
    public List<SyntaxTree> Items { get; } = new();

    /// <summary>
    /// The parent node of this, or null if we're root
    /// </summary>
    public SyntaxTree? Parent { get; set; }

    /// <summary>
    /// Create a new node that is a child of this one.
    /// </summary>
    public SyntaxTree AddListNode()
    {
        var child = new SyntaxTree{
            Parent = this,
            Type = SyntaxNodeType.List,
            TokenType = TokenType.Invalid
        };
        Items.Add(child);
        return child;
    }

    /// <summary>
    /// Add a new leaf node with a token value and type
    /// </summary>
    public void AddToken(string token, TokenType type)
    {
        var child = new SyntaxTree{
            Parent = this,
            Type = SyntaxNodeType.Token,
            Value = token,
            TokenType = type
        };
        Items.Add(child);
    }
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
    Token
}