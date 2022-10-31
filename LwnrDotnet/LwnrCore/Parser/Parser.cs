using System.Text;

namespace LwnrCore.Parser;

/// <summary>
/// Basic parser, string to AST.
/// The language is a simple S-Expr thing.
/// </summary>
public static class Parser
{
    /// <summary>
    /// Number of spaces per indent depth
    /// </summary>
    private const int IndentDepth = 4;
    
    /// <summary>
    /// Parse an input
    /// </summary>
    public static SyntaxTree Parse(string input)
    {
        var outp = new SyntaxTree();
        
        var target = outp;
        string? label = null;
        var cursor = new ParserCursor(input, 0);
        var lastPosition = input.Length - 1;

        while (cursor.HasSome())
        {
            // read next token
            var start = cursor.Position();
            var token = cursor.ReadToken(out var tokenType);
            var end = cursor.Position();
            
            switch (tokenType)
            {
                // categorise token
                case TokenType.Invalid:
                    target.AddToken(token, tokenType, start, end, label);
                    label = null;
                    outp.IsValid = false;
                    outp.Reasons.Add($"Invalid token '{token}', Positions {start}..{end}");
                    break;
                
                case TokenType.OpenList: // go deeper
                    target = target.AddListNode(start, token, label);
                    label = null;
                    break;
                
                case TokenType.CloseList when target.Parent is null:// too many close paren
                    outp.IsValid = false;
                    outp.Reasons.Add($"Too many close parenthesis '{token}', Positions {start}..{end}");
                    return outp;
                
                case TokenType.CloseList: // up
                    target.End = end;
                    target = target.Parent;
                    if (label is not null)
                    {
                        outp.IsValid = false; // labeling nothing?
                        outp.Reasons.Add($"Label '{label}' applies to nothing, Positions {start}..{end}");
                    }

                    break;
                
                case TokenType.EndOfInput:
                    if (target.Parent is not null)
                    {
                        outp.IsValid = false; // false if not enough close paren
                        outp.Reasons.Add($"Not enough close parenthesis '{token}', Positions {start}..{end}");
                    }

                    if (label is not null)
                    {
                        outp.IsValid = false; // labeling nothing?
                        outp.Reasons.Add($"Label '{label}' applies to nothing, Positions {start}..{end}");
                    }

                    return outp;

                case TokenType.LiteralString:
                case TokenType.LiteralNumber:
                    target.AddToken(token, tokenType, start, end, label);
                    label = null;
                    break;

                case TokenType.Atom:
                {
                    if (token.EndsWith(':')) // this is a label. Append to next token or list.
                    {
                        label = token[..^1];
                    }
                    else
                    {
                        target.AddToken(token, tokenType, start, end, label);
                        label = null;
                    }

                    break;
                }

                case TokenType.LineBreak:
                    if (start > 0 && end <= lastPosition) // ignore leading and trailing newlines
                        target.AddMeta(token, tokenType, start, end);
                    break;
                
                case TokenType.Comment:
                    target.AddMeta(token, tokenType, start, end);
                    break;
                
                default: throw new Exception($"Unexpected token type '{tokenType.ToString()}'");
            }
        }

        if (target.Parent is not null)
        {
            outp.IsValid = false; // false if not enough close paren
            outp.Reasons.Add("Not enough close parenthesis by end of input");
        }

        return outp;
    }

    /// <summary>
    /// Render a human-readable language string for a syntax tree
    /// </summary>
    public static string Render(SyntaxTree input)
    {
        var sb = new StringBuilder();

        var newLine = false;
        RenderRecursive(input, sb, depth: 0, ref newLine);
        
        return sb.ToString();
    }

    private static void RenderRecursive(SyntaxTree input, StringBuilder sb, int depth, ref bool newLine)
    {
        var firstInList = true;
        foreach (var item in input.Items)
        {

            switch (item.Type)
            {
                case SyntaxNodeType.List:
                    if (newLine) sb.Append(new string(' ', depth * IndentDepth));
                    if (item.Label is not null) sb.Append($"{item.Label}: ");
                    if (!firstInList) sb.Append(' ');
                    firstInList = false;
                    
                    sb.Append(item.TokenType == TokenType.StackQuote ? '{' : '(');
                    newLine = false;
                    
                    RenderRecursive(item, sb, depth + 1, ref newLine);
                    
                    if (newLine) sb.Append(new string(' ', depth * IndentDepth));
                    sb.Append(item.TokenType == TokenType.StackQuote ? '}' : ')');
                    
                    break;

                case SyntaxNodeType.Token:
                    if (newLine) sb.Append(new string(' ', depth * IndentDepth));
                    if (!firstInList) sb.Append(' ');
                    firstInList = false;
                    newLine = false;
                    
                    if (item.Label is not null) sb.Append($"{item.Label}: ");
                    RenderToken(item, sb);
                    break;

                case SyntaxNodeType.Meta:
                    RenderMeta(firstInList, newLine, item, sb, depth);
                    if (item.TokenType is TokenType.Comment or TokenType.LineBreak)
                    {
                        newLine = true;
                        firstInList = true;
                    }

                    break;
                
                case SyntaxNodeType.Root: // there should be only one root node
                default: throw new Exception("Unexpected syntax node type");
            }
        }
    }

    private static void RenderMeta(bool firstInList, bool newLine, SyntaxTree item, StringBuilder sb, int depth)
    {
        switch (item.TokenType)
        {
            case TokenType.Comment:
                if (newLine) sb.Append(new string(' ', depth * IndentDepth));
                if (!firstInList) sb.Append(' ');
                sb.Append(item.Value);
                break;
            
            case TokenType.LineBreak:
                sb.AppendLine();
                break;
        }
    }

    private static void RenderToken(SyntaxTree item, StringBuilder sb)
    {
        switch (item.TokenType)
        {
            case TokenType.Invalid:
                sb.Append($"(? {item.Value??""})");
                break;
            
            case TokenType.EndOfInput:
                break;
            
            case TokenType.LiteralString:
                sb.Append('`');
                sb.Append(item.Value);
                sb.Append('`');
                break;
            
            case TokenType.LiteralNumber:
            case TokenType.Atom:
                sb.Append(item.Value);
                break;
            
            case TokenType.Comment:
                sb.Append(item.Value);
                break;
            
            case TokenType.OpenList:
            case TokenType.CloseList:
            default:
                throw new Exception($"Unexpected token type '{item.TokenType.ToString()}'");
        }
    }
}