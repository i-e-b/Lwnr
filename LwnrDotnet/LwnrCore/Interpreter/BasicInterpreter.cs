using LwnrCore.Parser;
using LwnrCore.Types;

namespace LwnrCore.Interpreter;

/// <summary>
/// A very simple AST-walking interpreter.
/// It will be slow, but that's not currently the point.
/// </summary>
public class BasicInterpreter
{
    private readonly SyntaxTree _program;

    /// <summary>
    /// Prepare an interpreter for a program
    /// </summary>
    public BasicInterpreter(SyntaxTree program)
    {
        _program = program;
    }

    /// <summary>
    /// Run a single step of the program.
    /// Returns true if the program is still running,
    /// false if it has finished
    /// </summary>
    public bool Step()
    {
        return false;
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
        throw new NotImplementedException();
    }
}