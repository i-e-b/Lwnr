namespace LwnrCore.Parser;

/// <summary>
/// Token types
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Not a valid token
    /// </summary>
    Invalid = 0,
    
    /// <summary>
    /// Cursor ran out of data
    /// </summary>
    EndOfInput = 1,
    
    /// <summary>
    /// A quoted string value
    /// </summary>
    LiteralString = 2,
    
    /// <summary>
    /// An unquoted numeric string
    /// </summary>
    LiteralNumber = 3,
    
    /// <summary>
    /// Some kind of un-quoted, non-numeric token
    /// </summary>
    Atom = 4,
    
    /// <summary>
    /// '(' character
    /// </summary>
    OpenParen = 5,
    
    /// <summary>
    /// ')' character
    /// </summary>
    CloseParen = 6
}