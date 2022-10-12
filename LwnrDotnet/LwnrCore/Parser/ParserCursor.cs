using System.Globalization;
using System.Text;

namespace LwnrCore.Parser;

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
    
    private char Peek()
    {
        if (_idx >= _input.Length - 1) return '\0';
        return _input[_idx + 1];
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
            case '/':
            {
                if (Peek() == '/') // double slash
                {
                    type = TokenType.Comment;
                    return ReadLineComment();
                }

                break;
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

        var value = sb.ToString();
        type = LooksLikeNumber(value) ? TokenType.LiteralNumber : TokenType.Atom;
        return value;
    }

    private bool LooksLikeNumber(string value)
    {
        // strip out separators, allowing "10'000" or "10_000" or "0x7FFF_8050"
        if (value.Contains('\'') || value.Contains('_'))
        {
            value = value.Replace("'", "").Replace("_","");
        }

        // 'float' style should handle most things
        if (double.TryParse(value, NumberStyles.Float, null, out _)) return true;
        
        // maybe a different base?
        if (value.StartsWith("0x") || value.StartsWith("0b")) value = value.Substring(2);
        if (int.TryParse(value, NumberStyles.HexNumber, null, out _)) return true;
        
        return false;
    }

    private string ReadQuotedString(out bool complete)
    {
        complete = false;
        if (_on != '`') throw new Exception("Lost the start of a string?");
        
        Step();
        if (_on == '`')
        {
            Step(); // eat the close quote
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
    
    /// <summary>
    /// Read from the start of a comment to either end of line
    /// or end of stream.
    /// </summary>
    private string ReadLineComment()
    {
        if (_on != '/') throw new Exception("Lost the start of a comment?");
        
        var sb = new StringBuilder();
        
        sb.Append(_on);
        Step();

        while (_on != '\r' && _on != '\n')
        {
            if (_on == '\0')
            {
                return sb.ToString();
            }
            
            sb.Append(_on);
            Step();
        }
        
        // add the newline into the comment
        sb.Append(_on);
        if (_on == '\r' && Peek() == '\n')
        {
            Step();
            sb.Append(_on);
        }
        Step(); // eat last char

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