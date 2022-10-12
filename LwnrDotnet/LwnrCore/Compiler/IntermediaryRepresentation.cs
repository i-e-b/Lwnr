using System.Text;
using LwnrCore.Parser;

namespace LwnrCore.Compiler;

/// <summary>
/// This is the compiler output, a series of IR commands
/// </summary>
public class IntermediaryRepresentation
{
    /// <summary>
    /// Locations of functions in the <see cref="Instructions"/> list.
    /// </summary>
    public Dictionary<string, int> FunctionOffsets { get; } = new();
    
    /// <summary>
    /// List of compiled instructions
    /// </summary>
    public List<Instruction> Instructions { get; } = new();

    /// <summary>
    /// Add a sub-program as a named function
    /// </summary>
    public void MergeAsFunction(string name, IntermediaryRepresentation subProgram)
    {
        FunctionOffsets.Add(name, Instructions.Count);
        foreach (var instruction in subProgram.Instructions)
        {
            Instructions.Add(instruction);
        }
        Instructions.Add(new Instruction{Command = IrCmdType.EndOfFunction});
    }

    /// <summary>
    /// Add a command that maps a parameter position to a scope name
    /// </summary>
    public void AliasParameter(string name, int index)
    {
        Instructions.Add(new Instruction{Command = IrCmdType.AliasParam, Arguments = new[]{name, index.ToString()}});
    }

    /// <summary>
    /// Human readable representation of the stored IR
    /// </summary>
    public string Describe()
    {
        var lookup = new Dictionary<int, string>();
        foreach (var kvp in FunctionOffsets) { lookup.Add(kvp.Value, kvp.Key); }
        
        var sb = new StringBuilder();
        for (int i = 0; i < Instructions.Count; i++)
        {
            var cmd = Instructions[i];
            sb.Append($"{i:0000} {cmd.Command.ToString()} ");
            if (cmd.Arguments.Length > 0) sb.Append(string.Join(", ", cmd.Arguments));
            if (lookup.ContainsKey(i)) sb.Append($" <- (def {lookup[i]}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Mark the start of a set of arguments
    /// </summary>
    public void StartArguments()
    {
        Instructions.Add(new Instruction{Command = IrCmdType.StartArgs});
    }

    /// <summary>
    /// Store a list quotation as an argument to a call
    /// </summary>
    public void QuoteParameter(SyntaxTree treeNode, int paramIdx)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Store a name reference as an argument to a call
    /// </summary>
    public void NameParameter(string name, int paramIdx)
    {
        Instructions.Add(new Instruction { Command = IrCmdType.RefArg, Arguments = new[] { name, paramIdx.ToString() } });
    }

    /// <summary>
    /// Store a literal value as an argument to a call
    /// </summary>
    public void ValueParameter(string value, int paramIdx)
    {
        Instructions.Add(new Instruction { Command = IrCmdType.ValArg, Arguments = new[] { value, paramIdx.ToString() } });
    }

    /// <summary>
    /// Call a function or built-in
    /// </summary>
    public void CallFunction(string name)
    {
        Instructions.Add(new Instruction { Command = IrCmdType.Call, Arguments = new[] { name } });
    }
}

/// <summary>
/// A single instruction from the IR
/// </summary>
public class Instruction
{
    /// <summary> Command </summary>
    public IrCmdType Command { get; set; } = IrCmdType.Invalid;

    /// <summary> Arguments </summary>
    public string[] Arguments { get; set; }=Array.Empty<string>();
}

/// <summary>
/// Commands
/// </summary>
public enum IrCmdType
{
    /// <summary>
    /// Non-valid value
    /// </summary>
    Invalid = 0,
    
    /// <summary>
    /// End of a function. No args
    /// </summary>
    EndOfFunction = 1,
    
    /// <summary>
    /// Alias a parameter position to a name.
    /// Args = name, position
    /// </summary>
    AliasParam = 2,
    
    /// <summary>
    /// Start of a set of arguments
    /// </summary>
    StartArgs = 3,
    
    /// <summary>
    /// Declare a parameter position from a reference name
    /// Args = name, position
    /// </summary>
    RefArg = 4,
    
    /// <summary>
    /// Declare a parameter position from a code literal
    /// Args = name, position
    /// </summary>
    ValArg = 5,
    
    /// <summary>
    /// Call a function or built-in
    /// </summary>
    Call = 6
}