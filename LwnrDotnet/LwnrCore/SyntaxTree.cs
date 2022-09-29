namespace LwnrCore;

/// <summary>
/// Syntax tree for the parser and interpreter
/// </summary>
public class SyntaxTree
{
    /// <summary>
    /// True if syntax contained no errors
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// What does this node represent?
    /// </summary>
    public SyntaxNodeType Type { get; set; }

    /// <summary>
    /// Child nodes
    /// </summary>
    public List<SyntaxTree> Items { get; } = new();
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
    List
}