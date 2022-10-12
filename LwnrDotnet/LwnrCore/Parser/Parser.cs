using System.Text;

namespace LwnrCore.Parser;

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
        
        var target = outp;
        var cursor = new ParserCursor(input, 0);

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
                    target.AddToken(token, tokenType, start, end);
                    outp.IsValid = false;
                    break;
                
                case TokenType.OpenParen: // go deeper
                    target = target.AddListNode(start);
                    break;
                
                case TokenType.CloseParen when target.Parent is null:// too many close paren
                    outp.IsValid = false;
                    return outp;
                
                case TokenType.CloseParen: // up
                    target.End = end;
                    target = target.Parent;
                    break;
                
                case TokenType.EndOfInput:
                    outp.IsValid = target.Parent is null; // false if not enough close paren
                    return outp;

                case TokenType.LiteralString:
                case TokenType.LiteralNumber:
                case TokenType.Atom:
                    target.AddToken(token, tokenType, start, end);
                    break;
                
                case TokenType.Comment:
                    target.AddMeta(token, tokenType, start, end);
                    break;
                
                default: throw new Exception($"Unexpected token type '{tokenType.ToString()}'");
            }
        }

        outp.IsValid = target.Parent is null; // false if not enough close paren
        return outp;
    }

    /// <summary>
    /// Render a human-readable language string for a syntax tree
    /// </summary>
    public static string Render(SyntaxTree input)
    {
        var sb = new StringBuilder();

        RenderRecursive(input, sb);
        
        return sb.ToString();
    }

    private static void RenderRecursive(SyntaxTree input, StringBuilder sb)
    {
        var first = true;
        foreach (var item in input.Items)
        {
            if (!first) sb.Append(' ');
            first = false;
            
            switch (item.Type)
            {
                case SyntaxNodeType.List:
                    sb.Append('(');
                    RenderRecursive(item, sb);
                    sb.Append(')');
                    break;
                
                case SyntaxNodeType.Token:
                    RenderToken(item, sb);
                    break;
                
                case SyntaxNodeType.Meta:
                    sb.Append(item.Value);
                    if (item.TokenType == TokenType.Comment) first = true;
                    break;
                
                case SyntaxNodeType.Root: // there should be only one root node
                default: throw new Exception("Unexpected syntax node type");
            }
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
            
            case TokenType.OpenParen:
            case TokenType.CloseParen:
            default:
                throw new Exception($"Unexpected token type '{item.TokenType.ToString()}'");
        }
    }
}