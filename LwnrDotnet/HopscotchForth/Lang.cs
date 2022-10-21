using System.Collections;
using System.Text;

namespace HopscotchForth;

/// <summary>
/// A quick experiment based on
/// https://gist.github.com/i-e-b/e91e28a61b55ad88aed0e4944a7f1b80
///
/// It's nothing to do with Lwnr, but it's not big enough to bother making a new project.
/// </summary>
public static class Lang
{
    /// <summary>
    /// Take an input which is a [' ', '(', ')'] separated string of tokens,
    /// and re-order so the parenthesis are removed and the symbol to the
    /// left of parenthesis is moved to the right
    /// </summary>
    public static List<string> ParseAndReorder(string input)
    {
        var output = new List<string>();
        var sb = new StringBuilder();
        var flip = new Stack<string>(); // left token each time we find a '(', popped when we find ')'

        for (var position = 0; position < input.Length; position++)
        {
            var c = input[position];
            
            if (char.IsWhiteSpace(c))
            {
                if (sb.Length > 0) output.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            if (c == '(')
            {
                if (sb.Length > 0)
                {
                    flip.Push(sb.ToString());
                    sb.Clear();
                }
                else if (output.Count > 0)
                {
                    var idx = output.Count - 1;
                    flip.Push(output[idx]);
                    output.RemoveAt(idx);
                }
                else throw new Exception($"Unmatched open parenthesis at {position}");

                continue;
            }

            if (c == ')')
            {
                if (flip.Count < 1) throw new Exception($"Unmatched close parenthesis at {position}");
                if (sb.Length > 0)
                {
                    output.Add(sb.ToString());
                    sb.Clear();
                }
                output.Add(flip.Pop());
                continue;
            }

            // nothing else, add to current 
            sb.Append(c);
        }
        
        if (flip.Count != 0) throw new Exception($"Unmatched close parenthesis at end: {flip.Count} left on flip stack");

        return output;
    }

    /// <summary>
    /// Run a basic forth program
    /// </summary>
    public static Stack<double> Run(List<string> program, Dictionary<string, double>? vars)
    {
        vars ??= new Dictionary<string, double>();
        var valueStack = new Stack<double>();
        var returnStack = new Stack<int>();
        
        var position = 0;
        while (true)
        {
            if (position >= program.Count) return valueStack;
            if (position < 0) throw new Exception("unexpected position");
            
            var cmd = program[position];
            if (InterpretCommand(cmd, ref position, program, valueStack, returnStack, vars)) position++;
            
            Console.WriteLine($"{cmd}@{position} -> {PrintStack(valueStack)}");
        }
    }

    private static string PrintStack(Stack<double> stack)
    {
        return string.Join(", ", stack.ToArray());
    }

    /// <summary>
    /// Interpret a single command.
    /// Return true if position should advance normally,
    /// return false if command has set position (call, return, etc)
    /// </summary>
    private static bool InterpretCommand(string cmd, ref int position, List<string> program, Stack<double> valueStack, Stack<int> returnStack, Dictionary<string,double> vars)
    {
        // easy stuff
        switch (cmd)
        {
            case "=":
            case ">":
            case "<":
            case "!=":
            case ">=":
            case "<=":
            {
                AssertStack(2, valueStack, position);
                var right = valueStack.Pop();
                var left = valueStack.Pop();
                valueStack.Push(Inequality(cmd, left, right));
                return true;
            }

            case "-":
            case "+":
            case "*":
            case "/":
            {
                AssertStack(2, valueStack, position);
                var right = valueStack.Pop();
                var left = valueStack.Pop();
                valueStack.Push(Maths(cmd, left, right));
                return true;
            }
        }

        // literal values
        if (double.TryParse(cmd, out var value))
        {
            valueStack.Push(value);
            return true;
        }
        
        // complex stuff
        if (cmd == "if") return DoIf(cmd, ref position, program, valueStack, returnStack, vars);
        if (cmd == "then") return SkipToEndOfIf(ref position, program);
        if (cmd == "else" || cmd == "endif") return true;
        
        // named values
        if (vars.ContainsKey(cmd))
        {
            valueStack.Push(vars[cmd]);
            return true;
        }
        
        throw new Exception($"Could not resolve '{cmd}' at {position}");
    }

    /// <summary>
    /// Scan forward until we hit 'else' or 'endif'
    /// </summary>
    private static bool SkipToEndOfIf(ref int position, List<string> program)
    {
        while (position < program.Count)
        {
            var cmd = program[position];
            if (cmd == "else" || cmd == "endif") return false;
            position++;
        }
        return false; // we changed position
    }
    
    /// <summary>
    /// Scan forward until we hit 'then'
    /// </summary>
    private static bool SkipToThen(ref int position, List<string> program)
    {
        while (position < program.Count)
        {
            var cmd = program[position];
            position++; // always go past the 'then'
            if (cmd == "then") return false;
        }
        return false; // we changed position
    }

    /// <summary>
    /// Look at the stack, and either continue or scan for 'then'
    /// </summary>
    private static bool DoIf(string cmd, ref int position, List<string> program, Stack<double> valueStack, Stack<int> returnStack, Dictionary<string,double> vars)
    {
        AssertStack(1, valueStack, position);
        var val = valueStack.Pop();
        
        if (Math.Abs(val + 1) < 1e-6) return true; // "True", keep going
        
        // "False", scan *past* the 'then', which should hit either 'else' or 'endif'
        return SkipToThen(ref position, program);
    }

    /// <summary>
    /// Basic operators
    /// </summary>
    private static double Maths(string cmd, double left, double right)
    {
        return cmd switch
        {
            "-" => left - right,
            "+" => left + right,
            "*" => left * right,
            "/" => right != 0 ? left / right : 0,
            _ => throw new Exception($"Unknown operator '{cmd}'")
        };
    }

    /// <summary>
    /// Basic inequality
    /// </summary>
    private static double Inequality(string cmd, double left, double right)
    {
        return cmd switch
        {
            "=" => (Math.Abs(left - right) < 1e-6) ? -1 : 0,
            "!=" => (Math.Abs(left - right) > 1e-6) ? -1 : 0,
            ">" => left > right ? -1 : 0,
            "<" => left < right ? -1 : 0,
            ">=" => left >= right ? -1 : 0,
            "<=" => left <= right ? -1 : 0,
            _ => throw new Exception($"Unknown inequality '{cmd}'")
        };
    }

    private static void AssertStack(int count, ICollection stack, int position)
    {
        if (stack.Count < count) throw new Exception($"Stack underflow at {position}");
    }
}