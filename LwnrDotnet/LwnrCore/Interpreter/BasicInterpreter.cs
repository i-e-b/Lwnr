using System.Text;
using LwnrCore.Parser;

namespace LwnrCore.Interpreter;

/// <summary>
/// A very simple AST-walking interpreter.
/// It will be slow, but that's not currently the point.
/// </summary>
public class BasicInterpreter
{
    private readonly SyntaxTree _program;
    private readonly Queue<char> _stdIn;
    private readonly StringBuilder _stdOut;
    
    private readonly Dictionary<string, SyntaxTree> _rootFunctions;
    
    private readonly Stack<SyntaxTreeReference> _callStack;

    /// <summary>
    /// Prepare an interpreter for a program
    /// </summary>
    public BasicInterpreter(SyntaxTree program)
    {
        if (!program.IsValid) throw new Exception("Can't run an invalid program");
        
        
        _rootFunctions = new Dictionary<string, SyntaxTree>();
        
        // scan the root levels looking for basic definitions
        foreach (var item in program.Items)
        {
            TryAddFunctionDef(item);
        }
        
        if (!_rootFunctions.ContainsKey("main")) throw new Exception("Program must have 'main' function defined.");
        
        // store basics
        _program = program;
        _stdIn = new Queue<char>();
        _stdOut = new StringBuilder();
        _callStack = new Stack<SyntaxTreeReference>();
        
        // push start of main function into stack
        var main = _rootFunctions["main"];
        _callStack.Push(new SyntaxTreeReference(main, 0));
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
        if (_rootFunctions.ContainsKey(name)) throw new Exception($"Function '{name}' is redefined.");
        
        // Unique definition, add it to the lookup
        _rootFunctions.Add(name, item);
    }

    /// <summary>
    /// Run a single step of the program.
    /// Returns true if the program is still running,
    /// false if it has finished
    /// </summary>
    public bool Step()
    {
        // We assume each list is (function-name args...) unless
        // there is something else.
        // Args are passed as syntax trees at the moment.
        
        if (_callStack.Count < 1) return false;
        
        var top = _callStack.Pop();
        var next = InterpretStep(top);
        if (next is not null) _callStack.Push(next);
        return true;
    }

    /// <summary>
    /// Run a single step
    /// </summary>
    private SyntaxTreeReference? InterpretStep(SyntaxTreeReference command)
    {
        if (command.Index >= command.Node.Items.Count) return null; // ran off the end
        
        var item = command.Node.Items[command.Index];

        switch (item.Type)
        {
            case SyntaxNodeType.Root:
                throw new Exception("Tried to execute root?");
                
            case SyntaxNodeType.List:
                Console.WriteLine("List");
                // Try to run a function?
                // TODO: push more onto stack?
                break;
            
            case SyntaxNodeType.Scope:
                throw new Exception("Not yet implemented");
                
            case SyntaxNodeType.Token:
                Console.WriteLine("Token: "+item.Describe());
                break;
            
            case SyntaxNodeType.Meta: // ignore & continue
                return command.Advance();
                
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return null;
    }

    /// <summary>
    /// Append a string to the program's input stream
    /// </summary>
    public void WriteToInput(string str)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Read any output from a program.
    /// Returns empty if nothing written yet.
    /// </summary>
    public string ReadFromOutput()
    {
        return _stdOut.ToString();
    }
}

/// <summary>
/// Reference to an item in a syntax tree
/// </summary>
public class SyntaxTreeReference
{
    /// <summary>
    /// The syntax tree node
    /// </summary>
    public SyntaxTree Node { get; }
    
    /// <summary>
    /// Item being indexed
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Create a reference
    /// </summary>
    public SyntaxTreeReference(SyntaxTree node, int index)
    {
        Node = node;
        Index = index;
    }

    /// <summary>
    /// Next step in this node
    /// </summary>
    public SyntaxTreeReference Advance() => new(Node, Index+1);
}