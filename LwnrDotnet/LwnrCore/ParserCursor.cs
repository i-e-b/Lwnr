using System.Text;

namespace LwnrCore;

/// <summary>
/// A string scanner for parsing
/// </summary>
public class ParserCursor
{
    private readonly string _input;
    private int _idx;
    private char _on;

    /// <summary>
    /// Start parsing a string at an offset
    /// </summary>
    public ParserCursor(string input, int startOffset)
    {
        _input = input;
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
    public string ReadToken(out TokenType type)
    {
        type = TokenType.Invalid;
        
        // skip whitespace
        while (OnWhiteSpace()) { Step(); }
        
        // handle single-char cases
        switch (_on)
        {
            case '(':
            {
                Step();
                type = TokenType.OpenParen;
                return "(";
            }
            case ')':
            {
                Step();
                type = TokenType.CloseParen;
                return ")";
            }
            case '`':
            {
                type = TokenType.LiteralString;
                var str = ReadQuotedString(out var complete);
                if (!complete) type = TokenType.Invalid;
                return str;
            }
            case '\0':
            {
                type = TokenType.EndOfInput;
                return "";
            }
        }
        
        // Some other type of token. Read up to next paren, quote, or whitespace
        var sb = new StringBuilder();

        while (!IsBreakChar())
        {
            sb.Append(_on);
            Step();
        }

        type = TokenType.General;
        return sb.ToString();
    }

    private string ReadQuotedString(out bool complete)
    {
        complete = false;
        if (_on != '`') throw new Exception("Lost the start of a string?");
        
        Step();
        if (_on == '`')
        {
            complete = true;
            return ""; // empty
        }

        var sb = new StringBuilder();

        while (_on != '`')
        {
            if (_on == '\0')
            {
                complete = false;
                return sb.ToString();
            }
            
            sb.Append(_on);
            Step();
        }

        Step(); // eat the close quote
        complete = true;
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
            
            _ => char.IsWhiteSpace(_on)
        };
    }

    /// <summary>
    /// Returns true if there is input to read
    /// </summary>
    public bool HasSome()
    {
        return _on != '\0';
    }
}