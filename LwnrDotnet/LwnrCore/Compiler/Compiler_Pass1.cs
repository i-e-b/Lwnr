using LwnrCore.Parser;

namespace LwnrCore.Compiler;

/// <summary>
/// A very simple AST-walking compiler.
/// It will be slow, but that's not currently the point.
/// </summary>
public class CompilerPass1
{
    private readonly Dictionary<string, SyntaxTree> _functions;

    private readonly Dictionary<string, Dictionary<string, int>> _builtInArgs = new()
    {
        { "new", new Dictionary<string, int> { { "type", 0 }, { "name", 1 } } },
        { "log", new Dictionary<string, int> { { "target", 0 } } }
    };

    /// <summary>
    /// Prepare an interpreter for a program
    /// </summary>
    public CompilerPass1(SyntaxTree program)
    {
        if (!program.IsValid) throw new Exception("Can't run an invalid program");
        
        
        _functions = new Dictionary<string, SyntaxTree>();
        
        // scan the root levels looking for basic definitions
        foreach (var item in program.Items)
        {
            TryAddFunctionDef(item);
        }
        
        if (!_functions.ContainsKey("main")) throw new Exception("Program must have 'main' function defined.");
    }

    private void TryAddFunctionDef(SyntaxTree item)
    {
        // does it look like a function def?
        if (item.Type != SyntaxNodeType.List) return;
        if (item.Items.Count < 2) return;
        if (!item.Items[0].IsAtom("def")) return;
        
        // Is it valid?
        if (item.Items[1].Type != SyntaxNodeType.Token) throw new Exception($"Expected function name, but got {item.Items[1].Describe()}");
        if (item.Items[1].TokenType != TokenType.Atom) throw new Exception($"Expected function name, but got {item.Items[1].Describe()}");
        
        var name = item.Items[1].Value;
        if (string.IsNullOrWhiteSpace(name)) throw new Exception("Unexpected empty value");
        
        // Ok, looks like a def call. Check it's unique
        if (_functions.ContainsKey(name)) throw new Exception($"Function '{name}' is redefined.");
        
        // Unique definition, add it to the lookup
        _functions.Add(name, item);
    }

    /// <summary>
    /// Compile program to IR
    /// </summary>
    public IntermediaryRepresentation Compile()
    {
        // Build each function, and update a jump table
        var ir = new IntermediaryRepresentation();

        foreach (var func in _functions)
        {
            AddFunction(func.Key, func.Value, ir);
        }
        
        return ir;
    }

    private void AddFunction(string name, SyntaxTree func, IntermediaryRepresentation ir)
    {
        var subProgram = new IntermediaryRepresentation();
        
        // First, we expect 3 items:
        // 1. "def"
        // 2. the function name
        // 3. the argument list -- which could be empty "()"
        
        var list = func.ProgramItems.ToList(); // filter out comments etc.
        if (list.Count < 3) throw new Exception($"Invalid function definition at {func.Position()}. Expected '(def name (args) ...)', got {func.Describe()}");
        
        var def = list[0];
        if (!def.IsAtom("def")) throw new Exception($"Invalid function definition at {def.Position()}. Expected 'def', got {def.Describe()}");
        
        if (name != list[1].Value) throw new Exception("Internal error!");
        
        var args = list[2];
        if (args.Type != SyntaxNodeType.List) throw new Exception($"Invalid function definition at {args.Position()}. Expected argument list '(...)', got {args.Describe()}");
        
        // Add callouts to the arguments
        int index = 0;
        foreach (var argument in args.ProgramItems)
        {
            if (argument.Type != SyntaxNodeType.Token || argument.TokenType != TokenType.Atom)
                throw new Exception($"Unexpected argument name at {argument.Position()}. Expected an atom name, got {argument.Describe()}");
            
            if (string.IsNullOrWhiteSpace(argument.Value)) throw new Exception("Logic error in compiler");
            
            subProgram.AliasParameter(argument.Value, index);
            index++;
        }
        
        var useScope = false;
        
        // each item at this level should be either:
        // - a call to a built-in
        // - a call to a defined function
        for (int idx = 3; idx < list.Count; idx++)
        {
            CompileFunctionCall(list, idx, ref useScope, subProgram);
        }
        
        ir.MergeAsFunction(name, subProgram, useScope);
    }

    private void CompileFunctionCall(List<SyntaxTree> list, int idx, ref bool useScope, IntermediaryRepresentation subProgram)
    {
        var callNode = list[idx];
        if (callNode.Type != SyntaxNodeType.List) throw new Exception($"Invalid call at {callNode.Position()}. Expected (func ...), got {callNode.Describe()}");
        if (!callNode.ProgramItems.Any()) throw new Exception($"Invalid call at {callNode.Position()}. Empty list. Expected (func ...), got {callNode.Describe()}");
        var call = callNode.ProgramItems.ToList();

        var target = call[0];
        if (target.Type != SyntaxNodeType.Token || target.TokenType != TokenType.Atom)
            throw new Exception($"Invalid call at {target.Position()}. No target name. Expected (func ...), got {target.Describe()}");
        if (string.IsNullOrWhiteSpace(target.Value)) throw new Exception("Internal error: empty target");
        
        // if there is a call to 'new', the container function needs a scope
        var targetName = target.Value;
        if (targetName == "new") useScope = true; // the outer lexical scope is also a resource scope

        subProgram.StartArguments();
        
        CompileArguments(subProgram, call, targetName);

        subProgram.CallFunction(target.Value);
    }

    private void CompileArguments(IntermediaryRepresentation subProgram, List<SyntaxTree> call, string functionBeingCalled)
    {
        // We go through the arguments twice --
        // Once to get the labelled/named positions,
        // Then again to output the IR commands
        var argNameToPos = GetArgumentPositions(functionBeingCalled);
        
        int paramIdx;
        var parameters = call.Skip(1).ToList();

        // Separate labelled and unlabelled arguments
        var labelledArgs = new Dictionary<int, SyntaxTree>();
        var unlabelledArgs = new Queue<SyntaxTree>();
        foreach (var param in parameters)
        {
            if (!string.IsNullOrWhiteSpace(param.Label)) // parameter is labelled.
            {
                if (!argNameToPos.ContainsKey(param.Label)) throw new Exception($"Label '{param.Label}' at {param.Position()} does not match any function arguments");
                paramIdx = argNameToPos[param.Label];
                if (labelledArgs.ContainsKey(paramIdx)) throw new Exception($"Parameter at {param.Position()} duplicates argument {paramIdx} of the function");
                labelledArgs.Add(paramIdx, param);
            }
            else
            {
                unlabelledArgs.Enqueue(param);
            }
        }
        
        // Compile references in order
        paramIdx = 0;
        while (paramIdx < parameters.Count)
        {
            var param = labelledArgs.ContainsKey(paramIdx)
                ? labelledArgs[paramIdx]
                : unlabelledArgs.Dequeue();
            
            if (param.Type == SyntaxNodeType.List) subProgram.QuoteParameter(param, paramIdx);
            else if (param.Type == SyntaxNodeType.Token)
            {
                if (!string.IsNullOrWhiteSpace(param.Label)) // parameter is labelled.
                {
                    paramIdx = argNameToPos[param.Label];
                }

                if (string.IsNullOrWhiteSpace(param.Value)) throw new Exception("Internal error: empty token");
                if (param.TokenType == TokenType.Atom) subProgram.NameParameter(param.Value, paramIdx);
                else subProgram.ValueParameter(param.Value, paramIdx);
            }
            else throw new Exception($"Invalid parameter at {param.Position()}. Expected value, name or quote; got {param.Describe()}");

            paramIdx++;
        }
    }

    private Dictionary<string,int> GetArgumentPositions(string functionName)
    {
        if (_builtInArgs.ContainsKey(functionName)) return _builtInArgs[functionName];
        var target = _functions[functionName];
        
        var argNode = target.Items[2];
        if (argNode.Type != SyntaxNodeType.List) throw new Exception($"Invalid function definition at {argNode.Position()}. Expected argument list '(...)', got {argNode.Describe()}");

        var outp= new Dictionary<string,int>();
        var args = argNode.ProgramItems.ToList();
        for (int i = 0; i < args.Count; i++)
        {
            var name = args[i].Value;
            if (name is null) throw new Exception($"Invalid function argument definition at {argNode.Position()}");
            outp.Add(name, i);
        }

        return outp;
    }
}