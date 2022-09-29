namespace LwnrCore;

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
    /// Some kind of un-quoted token
    /// </summary>
    General = 3,
    
    /// <summary>
    /// '(' character
    /// </summary>
    OpenParen = 4,
    
    /// <summary>
    /// ')' character
    /// </summary>
    CloseParen = 5
}