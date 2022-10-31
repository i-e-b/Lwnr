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
    OpenList = 5,
    
    /// <summary>
    /// ')' character
    /// </summary>
    CloseList = 6,
    
    /// <summary>
    /// A comment until end of line
    /// </summary>
    Comment = 7,
    
    /// <summary>
    /// A list type is a normal quote
    /// (as can be a function call or
    /// a macro input)
    /// </summary>
    CodeQuote = 8,
    
    /// <summary>
    /// A list type is a stack-based program.
    /// </summary>
    StackQuote = 9,
    
    /// <summary>
    /// Line break in source code (no program meaning)
    /// </summary>
    LineBreak = 10
}