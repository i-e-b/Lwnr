using LwnrCore.Containers;

namespace LwnrCore.ByteCode;

/// <summary>
/// Interpreter holds a runtime state (program and memory),
/// and handles execution of the program.
/// </summary>
public class Interpreter
{
    /// <summary>
    /// Program
    /// </summary>
    public ByteCodeInstruction[] Program { get; }

    /// <summary>
    /// The scope stack, which contains allocated memory, call stacks, etc
    /// </summary>
    public Vector<RuntimeFrame> Scopes { get; set; } = new();
    
    /// <summary>
    /// Program counter
    /// </summary>
    public int PC;
    
    /// <summary>
    /// The general 'condition' flag.
    /// Used by conditional operations, set by ALU ops.
    /// </summary>
    public bool Condition;
    
    /// <summary>
    /// Create a runtime with a program.
    /// Memory will be allocated as required
    /// </summary>
    /// <param name="program">the program to run. Execution starts from index zero</param>
    public Interpreter(ByteCodeInstruction[] program)
    {
        PC = 0;
        Program = program;
    }

    /// <summary>
    /// Execute the current instruction, then return
    /// </summary>
    public void Step()
    {
        if (PC < 0 || PC >= Program.Length) throw new Exception("Invalid program counter");
        var instr = Program[PC];
        switch (instr.Operation)
        {
            case OpCode.OpenScope:
                // Open scope acts somewhat like 'Call'.
                // It starts its own call stack, and switches to using it.
                // It starts its own memory space, and new allocations will use this
                PC++;
                break;
            
            case OpCode.CloseScope:
                // Should return from the scope, restore PC to top of last scope's stack
                break;
            
            case OpCode.Declare:
                // Supplies a variable index.
                // We should add or overwrite variable at this index.
                // Indexes don't need to be tightly packed, but should not be too sparse.
                break;
            case OpCode.Defer:
                // Same as making a function call, but the call doesn't happen until the scope is closed.
                // Deferred functions can be added at any point of a scope. They can't have parameters.
                break;
            case OpCode.ConditionalJump:
                // Make a relative jump only if condition flag is set
                break;
            case OpCode.ConditionalCopy:
                // Copy data from one variable to another, only if condition flag is set
                break;
            case OpCode.Call:
                // Call a subroutine. Calling return will resume from after this operation.
                // Subroutines can also be exited by closing the containing scope (somewhat like long-jumps or exception unwinding)
                
                // IEB: Question: how to pass parameters, given they might be selected by logic, and might cross scopes?
                break;
            case OpCode.Return:
                // Return from a subroutine. This should not cross scope boundaries
                break;
            case OpCode.ArithmeticLogic:
                // Do logical / arithmetic operations.
                // This will update the condition flag.
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

/// <summary>
/// A single instruction, with parameters
/// </summary>
public class ByteCodeInstruction
{
    /// <summary>
    /// Operation to perform
    /// </summary>
    public OpCode Operation;
    
    /// <summary>
    /// Parameters for the operation
    /// </summary>
    public ParamSet Parameters = new NoParams();
}

/// <summary>
/// An instruction operation code
/// </summary>
public enum OpCode
{
    /// <summary>
    /// Start a new scope. This affects the scope stack but not the call stack.
    /// </summary>
    [ParamType(typeof(ScopeParams))]
    OpenScope,
    
    /// <summary>
    /// Close the most recently opened scope, releasing all resources held by the scope.
    /// This affects the scope stack but not the call stack.
    /// </summary>
    [ParamType(typeof(NoParams))]
    CloseScope,
    
    /// <summary>
    /// Call a function. This affects the call stack but not the scope stack
    /// </summary>
    [ParamType(typeof(ParamSet/* TODO*/))]
    Call,
    
    /// <summary>
    /// Return from a function. This affects the call stack but not the scope stack
    /// </summary>
    [ParamType(typeof(ParamSet/* TODO*/))]
    Return,
    
    /// <summary>
    /// Declare a new variable in the current scope
    /// </summary>
    [ParamType(typeof(VarDecParams))]
    Declare,
    
    /// <summary>
    /// Set a function to be called when the current scope closes.
    /// Deferred functions are called in the reverse order they are declared.
    /// </summary>
    [ParamType(typeof(ParamSet/* TODO*/))]
    Defer,
    
    /// <summary>
    /// If the 'condition' flag is set, perform relative PC jump.
    /// This does not affect the call stack.
    /// </summary>
    [ParamType(typeof(ParamSet/* TODO*/))]
    ConditionalJump,
    
    /// <summary>
    /// If the 'condition' flag is set, copy the value from one memory slot to another
    /// </summary>
    [ParamType(typeof(ParamSet/* TODO*/))]
    ConditionalCopy,
    
    /// <summary>
    /// Perform arithmetic/logic. These can set/reset the 'condition' flag.
    /// </summary>
    [ParamType(typeof(ParamSet/* TODO*/))]
    ArithmeticLogic
}

/// <summary>
/// A scope stack frame
/// </summary>
public class RuntimeFrame
{
    /// <summary>
    /// Scope memory
    /// </summary>
    public Vector<ulong> Memory { get; set; } = new();

    /// <summary>
    /// Variables (references to any scope and memory location)
    /// </summary>
    public Vector<Variable> Variables { get; set; } = new();

    /// <summary>
    /// List of deferred functions. These are called immediately before the scope is closed
    /// </summary>
    public Vector<DeferFunc> DeferredActions { get; set; } = new();
    
    /// <summary>
    /// Subroutine call/return stack
    /// </summary>
    public Vector<CallFrame> CallStack { get; set; } = new();
}

/// <summary>
/// Subroutine call/return stack entry
/// </summary>
public class CallFrame
{
}

/// <summary>
/// A deferred function. These are called immediately before their owner scope is closed
/// </summary>
public class DeferFunc
{
}

/// <summary>
/// A variable - this is a reference to a scope (by index)
/// and a memory location inside that scope
/// </summary>
public struct Variable
{
    /// <summary>
    /// Index of scope that owns this variable
    /// </summary>
    public int ScopeIndex;
    
    /// <summary>
    /// Location in the scope's memory
    /// </summary>
    public int MemoryIndex;
}

/// <summary>
/// Parameters for declaring a variable
/// </summary>
public class VarDecParams:ParamSet
{
}

/// <summary>
/// Marker class for a parameter set
/// </summary>
public abstract class ParamSet { }

/// <summary>
/// Op code with no parameters
/// </summary>
public class NoParams : ParamSet
{
}

/// <summary>
/// Parameters for <see cref="OpCode.OpenScope"/>
/// </summary>
public class ScopeParams : ParamSet
{
    /// <summary>
    /// Number of bytes to allocate.
    /// </summary>
    public int ByteSize;
}

/// <summary>
/// Attribute for linking <see cref="OpCode"/> to its <see cref="ParamSet"/> class
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ParamTypeAttribute : Attribute
{
    /// <summary>
    /// Type of the operation parameter
    /// </summary>
    public Type ParameterType { get; }

    /// <summary>
    /// Mark this operation with its expected parameters
    /// </summary>
    /// <param name="type">Parameter type</param>
    public ParamTypeAttribute(Type type)
    {
        ParameterType = type;
    }
}