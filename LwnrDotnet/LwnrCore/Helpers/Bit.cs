using System.Text;

namespace LwnrCore.Helpers;

/// <summary>
/// Bitwise and bytewise helpers
/// </summary>
public class Bit
{
    /// <summary>
    /// Default: False. If true, data descriptions will be output as C# code fragments
    /// <see cref="Describe(string,System.Collections.Generic.IEnumerable{byte}?)"/>
    /// </summary>
    public static bool CodeModeForDescription { get; set; }
    
    /// <summary>
    /// Render a human-friendly string for a file size in bytes
    /// </summary>
    public static string Human(ulong byteLength)
    {
        double size = byteLength;
        var prefix = new []{ "b", "kb", "mb", "gb", "tb", "pb" };
        int i;
        for (i = 0; i < prefix.Length; i++)
        {
            if (size < 1024) break;
            size /= 1024;
        }
        return size.ToString("#0.##") + prefix[i];
    }
    
    /// <summary>
    /// Render a human-friendly string for a file size in bytes
    /// </summary>
    public static string Human(long byteLength) => Human((ulong)byteLength);
    
    /// <summary>
    /// Generate a description string in the same format as StrongSwan logs
    /// </summary>
    public static string Describe(string name, IEnumerable<byte>? bytes)
    {
        if (CodeModeForDescription)
        {
            name = Safe(name);
            if (bytes is null) return $"var {name} = new byte[0];";
            
            var sb = new StringBuilder();
            
            sb.Append("var ");
            sb.Append(name);
            sb.Append(" = new byte[] {");
            
            foreach (var b in bytes)
            {
                sb.Append($"0x{b:X2}, ");
            }
            
            sb.Append("};");
            sb.AppendLine();
            
            return sb.ToString();
        }
        else
        {
            if (bytes is null)
            {
                return $"{name} => 0 bytes (null)\r\n";
            }

            var sb = new StringBuilder();

            var idx = 0;
            var chunks = bytes.Chunk(16);
            
            foreach (var chunk in chunks)
            {
                sb.AppendLine();
                sb.Append($"{idx:d4}: ");
                var x = idx;
                foreach(var b in chunk)
                {
                    sb.Append($"{b:X2} ");
                    idx++;
                }
                var gap = 16 - (idx - x);
                for (int i = 0; i < gap; i++)
                {
                    sb.Append("   ");
                }
                foreach(var b in chunk)
                {
                    var ch = b;
                    if (ch >= ' ' && ch <= '~') sb.Append((char)ch);
                    else sb.Append('.');
                }
            }

            sb.AppendLine();
            return $"{name} => {idx} bytes" + sb;
        }
    }
    
    /// <summary>
    /// Generate a description string in the same format as StrongSwan logs
    /// </summary>
    public static string Describe(string name, byte[]? bytes, int offset, int length)
    {
        return Describe(name, bytes?.Skip(offset).Take(length));
    }
    
    /// <summary>
    /// Code friendly version of a string
    /// </summary>
    private static string Safe(string name)
    {
        var sb = new StringBuilder();

        var i = 0;
        foreach (var c in name)
        {
            switch (c)
            {
                case >= '0' and <= '9':
                    if (i==0) sb.Append('_');
                    i++;
                    sb.Append(c);
                    break;
                
                case >= 'a' and <= 'z':
                case >= 'A' and <= 'Z':
                case '_':
                    i++;
                    sb.Append(c);
                    break;
                
                case ' ':
                    i++;
                    sb.Append('_');
                    break;
            }
        }
        
        return sb.ToString();
    }
}