using System.Text;

namespace LwnrCore;

/// <summary>
/// Basic parser, string to AST.
/// The language is a simple S-Expr thing.
/// </summary>
public static class Parser
{
    /// <summary>
    /// Parse an input
    /// </summary>
    public static SyntaxTree Parse(string input)
    {
        var outp = new SyntaxTree();
        
        var cursor = new ParserCursor(input, 0);
        
        // read next token
        var token = cursor.ReadToken();
        
        // categorise token

        return outp;
    }

    /// <summary>
    /// Render a human-readable language string for a syntax tree
    /// </summary>
    public static string Render(SyntaxTree input)
    {
        var sb = new StringBuilder();
        
        return sb.ToString();
    }
}

/// <summary>
/// A string scanner for parsing
/// </summary>
public class ParserCursor
{
    private readonly string _input;
    private readonly int _startOffset;
    private int _idx;
    private char _on;

    /// <summary>
    /// Start parsing a string at an offset
    /// </summary>
    public ParserCursor(string input, int startOffset)
    {
        _input = input;
        _startOffset = startOffset;
        _idx = startOffset;
        ReadChar();
    }

    private void ReadChar()
    {
        if (_idx >= _input.Length) _on = '\0';
        else _on = _input[_idx];
    }

    /// <summary>
    /// Move the cursor forward
    /// </summary>
    public void Step()
    {
        _idx++;
        ReadChar();
    }

    /// <summary>
    /// Returns true if the current char is any form of whitespace
    /// </summary>
    public bool OnWhiteSpace()
    {
        if (_on == '\0') return false;
        return char.IsWhiteSpace(_on);
    }

    /// <summary>
    /// Read characters until the class changes or we hit end of input.
    /// An empty result indicates end of input.
    /// </summary>
    public string ReadToken()
    {
        // skip whitespace
        while (OnWhiteSpace()) { Step(); }
        
        // handle single-char cases
        switch (_on)
        {
            case '(':
            {
                Step();
                return "(";
            }
            case ')':
            {
                Step();
                return ")";
            }
            case '`': return ReadQuotedString();
            case '\0': return "";
        }
        
        // Some other type of token. Read up to next paren, quote, or whitespace
        var sb = new StringBuilder();

        while (!IsBreakChar())
        {
            sb.Append(_on);
            Step();
        }

        return sb.ToString();
    }

    private bool IsBreakChar()
    {
        return _on switch
        {
            '(' => true,
            ')' => true,
            '`' => true,
            '\0' => true,
            
            _ => false
        };
    }

    private string ReadQuotedString()
    {
        throw new NotImplementedException();
    }
}