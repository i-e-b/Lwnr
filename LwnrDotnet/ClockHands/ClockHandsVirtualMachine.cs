using System.Text;

namespace ClockHands;

/// <summary>
/// A quick experiment based on https://dl.acm.org/doi/fullHtml/10.1145/3613424.3614272
/// <p/>
/// This replaces a stack system, or registers (+renaming)
/// with a set of circular buffers.
/// </summary>
public class ClockHandsVirtualMachine
{
    private readonly List<int> _memory;
    private readonly List<Instruction> _instr;
    private readonly List<CircularOffsetBuffer<int>> _hands;
    private readonly StringBuilder _output;
    
    /// <summary> Program counter (into _instr) </summary>
    private int _pc;
    private bool _halted;

    /// <summary>
    /// Create a new blank machine
    /// with an initial program and memory.
    /// <p/>
    /// The machine has 5 hands, each with 5 slots (25 registers)
    /// </summary>
    public ClockHandsVirtualMachine(List<Instruction> instructions, List<int> memory)
    {
        _memory = memory;
        _instr = instructions;
        _hands = new List<CircularOffsetBuffer<int>>();
        _output = new StringBuilder();
        for (var i = 0; i < 5; i++)
        {
            _hands.Add(new CircularOffsetBuffer<int>(5));
        }
        Reset();
    }

    /// <summary>
    /// Reset to start of program, clear halt flag
    /// </summary>
    public void Reset()
    {
        _output.Clear();
        _pc = 0;
        _halted = false;
    }

    /// <summary>
    /// Run the CPU one step.
    /// Returns `true` unless a halt instruction was processed
    /// </summary>
    public bool Step()
    {
        if (_halted) return false;
        if (_pc >= _instr.Count) // program ran off end. Treat as a halt
        {
            _halted = true;
            return false;
        }

        var inst = _instr[_pc];
        if (inst.OpCode == Operation.Halt)
        {
            _halted = true;
            return false;
        }
        
        HandleInstruction(inst);
        Console.Write($" PC->{_pc}; ");
        
        return true;
    }

    private void HandleInstruction(Instruction inst)
    {
        var p1 = _hands[(int)inst.Param1Hand][inst.Param1Offset];
        var p2 = _hands[(int)inst.Param2Hand][inst.Param2Offset];
        var r = _hands[(int)inst.ResultHand];

        // IMPORTANT: all operations should change the PC value
        switch (inst.OpCode)
        {
            case Operation.BranchNeg:
            {
                if (p2 == _pc) throw new Exception("Invalid BranchNeg (p2 = PC)");
                Console.Write($"bng({p1},{p2}) ");
                if (p1 < 0) _pc = p2;
                else _pc++;
                return;
            }

            case Operation.BranchNz:
            {
                if (p2 == _pc) throw new Exception("Invalid BranchNz (p2 = PC)");
                Console.Write($"bnz({p1},{p2}) ");
                if (p1 != 0) _pc = p2;
                else _pc++;
                return;
            }

            case Operation.SetPoint:
            {
                _pc++;
                Console.Write($"spt({_pc}:{inst.ResultHand.ToString()}) ");
                r.Push(_pc);
                return;
            }

            case Operation.Jump:
            {
                if (p1 == 0) throw new Exception("Invalid Jump (zero)");
                Console.Write($"jmp({p1}) ");
                _pc += p1;
                return;
            }

            case Operation.Load:
            {
                r.Push(_memory[p1]);
                Console.Write($"lod([{p1}]:{inst.ResultHand.ToString()}) ");
                _pc++;
                return;
            }
            
            case Operation.Store:
            {
                _memory[p2] = p1;
                Console.Write($"sto({p1},[{p2}]) ");
                _pc++;
                return;
            }
            
            case Operation.Write:
            {
                _output.Append((char)p1);
                Console.Write($"out({p1}={(char)p1}) ");
                _pc++;
                return;
            }
            
            case Operation.Add:
            {
                r.Push(p1 + p2);
                Console.Write($"add({p1}+{p2}:{inst.ResultHand.ToString()}) ");
                _pc++;
                return;
            }
            
            case Operation.Sub:
            {
                r.Push(p1 - p2);
                Console.Write($"sub({p1}-{p2}:{inst.ResultHand.ToString()}) ");
                _pc++;
                return;
            }
            
            case Operation.Move:
            {
                r.Push(p1);
                Console.Write($"mov({p1}:{inst.ResultHand.ToString()}) ");
                _pc++;
                return;
            }
            
            case Operation.Incr:
            {
                r.Push(p1 + 1);
                Console.Write($"inc({p1}:{inst.ResultHand.ToString()}) ");
                _pc++;
                return;
            }
            
            case Operation.Decr:
            {
                r.Push(p1 - 1);
                Console.Write($"dec({p1}:{inst.ResultHand.ToString()}) ");
                _pc++;
                return;
            }
            
            case Operation.Immediate:
            {
                // In reality, this would be heavily limited by encoding
                r.Push(inst.Param1Offset - inst.Param2Offset);
                Console.Write($"imm({inst.Param1Offset}-{inst.Param2Offset}:{inst.ResultHand.ToString()}) ");
                _pc++;
                return;
            }

            case Operation.Halt:
            default:
                throw new Exception($"Invalid operation {inst} at {_pc}");
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{_instr.Count} instructions:");
        foreach (var i in _instr)
        {
            sb.AppendLine($"    {i.ToString()}");
        }

        sb.AppendLine($"{_memory.Count} memory cells:");
        for (int i = 0; i < _memory.Count; i++)
        {
            var m = _memory[i];
            sb.Append($"    {i:X4}: {_memory[i]:X8}");
            if (m >= ' ') sb.Append($" {(char)m}");
            sb.AppendLine();
        }
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Read out all output since last reset
    /// </summary>
    public string OutputString()
    {
        return _output.ToString();
    }
}

/// <summary>
/// Single instruction for ClockHands VM.
/// Note there is no 'immediate' encoding in this experiment, so static values need to be loaded from memory.
/// </summary>
public class Instruction
{
    /// <summary>
    /// Encode an operation with no input or output (for Halt)
    /// </summary>
    public Instruction(Operation op)
    {
        OpCode = op;
    }
    
    /// <summary>
    /// Encode an operation with only output
    /// </summary>
    public Instruction(Operation op, Hand result)
    {
        OpCode = op;
        ResultHand = result;
    }
    
    /// <summary>
    /// Encode an operation with a single input and no output
    /// </summary>
    public Instruction(Operation op, Hand p1Hand, int p1Offset)
    {
        OpCode = op;
        Param1Hand = p1Hand;
        Param1Offset = p1Offset;
    }

    /// <summary>
    /// Encode an operation with a single input and output
    /// </summary>
    public Instruction(Operation op, Hand p1Hand, int p1Offset, Hand result)
    {
        OpCode = op;
        Param1Hand = p1Hand;
        Param1Offset = p1Offset;
        ResultHand = result;
    }
    
    /// <summary>
    /// Encode an operation with a two inputs and no output
    /// </summary>
    public Instruction(Operation op, Hand p1Hand, int p1Offset, Hand p2Hand, int p2Offset)
    {
        OpCode = op;
        Param1Hand = p1Hand;
        Param1Offset = p1Offset;
        Param2Hand = p2Hand;
        Param2Offset = p2Offset;
    }
    
    /// <summary>
    /// Encode an operation with a two inputs and output
    /// </summary>
    public Instruction(Operation op, Hand p1Hand, int p1Offset, Hand p2Hand, int p2Offset, Hand result)
    {
        OpCode = op;
        Param1Hand = p1Hand;
        Param1Offset = p1Offset;
        Param2Hand = p2Hand;
        Param2Offset = p2Offset;
        ResultHand = result;
    }

    public override string ToString()
    {
        return $"{OpCode.ToString()} ({Param1Hand.ToString()}[{Param1Offset}] {Param2Hand.ToString()}[{Param2Offset}]) -> {ResultHand.ToString()}";
    }

    /// <summary>
    /// Which hand does param 1 come from?
    /// </summary>
    public Hand Param1Hand { get; set; }

    /// <summary>
    /// Offset of param 1
    /// </summary>
    public int Param1Offset { get; set; }
    
    /// <summary>
    /// Which hand does param 2 come from?
    /// </summary>
    public Hand Param2Hand { get; set; }

    /// <summary>
    /// Offset of param 2
    /// </summary>
    public int Param2Offset { get; set; }
    
    /// <summary>
    /// Which hand does result get pushed to?
    /// </summary>
    public Hand ResultHand { get; set; }

    /// <summary>
    /// What operation is to be performed?
    /// </summary>
    public Operation OpCode { get; set; }
}

/// <summary>
/// Operation codes for instructions.
/// All values are 32-bit signed ints
/// </summary>
public enum Operation
{
    #region Flow control
    /// <summary>
    /// Inputs ignored.
    /// Stop processor.
    /// No output.
    /// </summary>
    Halt = 0,
    
    /// <summary>
    /// If param1 is negative, jump to para2; Otherwise continue.
    /// No output.
    /// </summary>
    BranchNeg = 1,
    
    /// <summary>
    /// If param1 is not zero, jump to para2; Otherwise continue.
    /// No output.
    /// </summary>
    BranchNz = 2,
    
    /// <summary>
    /// Inputs ignored.
    /// Store (PC+1) to output.
    /// </summary>
    SetPoint = 3,
    
    /// <summary>
    /// Add param1 to the current PC (can be negative)
    /// No output.
    /// </summary>
    Jump = 4,
    #endregion
    
    #region Memory
    /// <summary>
    /// Load memory at param1 into result.
    /// </summary>
    Load = 5,
    
    /// <summary>
    /// Store value at param1 into memory at param2.
    /// No output.
    /// </summary>
    Store = 6,
    #endregion
    
    #region IO
    /// <summary>
    /// Send param1 to device at param2
    /// (for demo, this ignores param2 and writes output as a char to console).
    /// No output.
    /// </summary>
    Write = 7,
    #endregion
    
    #region ALU
    /// <summary>
    /// Evaluate (param1 + param2).
    /// Store to result.
    /// </summary>
    Add = 8,
    
    /// <summary>
    /// Evaluate (param1 - param2).
    /// Store to result.
    /// </summary>
    Sub = 9,
    
    /// <summary>
    /// Evaluate (param1 + 1).
    /// Store to result.
    /// </summary>
    Incr = 10,
    
    /// <summary>
    /// Evaluate (param1 - 1).
    /// Store to result.
    /// </summary>
    Decr = 11,
    
    /// <summary>
    /// Evaluate (param1).
    /// Store to result.
    /// </summary>
    Move = 12,
    
    /// <summary>
    /// Treat offsets as integers.
    /// Evaluate (param1Offset - param2Offset).
    /// Store to result.
    /// </summary>
    Immediate = 13,
    #endregion

}

/// <summary>
/// The named/numbered 'hands' available in the machine.
/// 'Address' and 'Data' are only hints, the machine doesn't actually care.
/// </summary>
public enum Hand
{
    /// <summary> Data 0 </summary>
    D0 = 0,
    /// <summary> Data 1 </summary>
    D1 = 1,
    /// <summary> Data 2 </summary>
    D2 = 2,
    
    /// <summary> Address 0 </summary>
    A0 = 3,
    /// <summary> Address 1 </summary>
    A1 = 4,
}