using ClockHands;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace LwnrUnitTests;

[TestFixture]
public class ClockHandsTests
{
    [Test]
    public void circular_buffer_pushes_values_back()
    {
        var subject = new CircularOffsetBuffer<int>(5);

        Assert.That(subject[0], Is.EqualTo(0), "All positions exist, and return default values");

        subject.Push(1);
        subject.Push(2);
        subject.Push(4);

        Assert.That(subject[0], Is.EqualTo(4), "Most recent value is at head");
        Assert.That(subject[2], Is.EqualTo(1), "Oldest value is pushed furthest back");

        subject.Push(8);
        subject.Push(16);
        subject.Push(32);

        Assert.That(subject[0], Is.EqualTo(32), "Most recent value is at head after roll-over");
        Assert.That(subject[4], Is.EqualTo(2), "Original values are lost after enough pushes");
    }

    [Test]
    public void test_program__hello_with_cursor()
    {
        var memory = new List<int>
        {
            /*0:start*/ 2,
            /*1:end*/ 14,
            /*2..14:data*/'H', 'e', 'l', 'l', 'o', ',', ' ', 'W', 'o', 'r', 'l', 'd', '!',
            /* run off space*/ 'X', 'X', 'X'
        };

        // Uses 4 'hands' for a total of 7 register positions
        var program = new List<Instruction>
        {
            new(Operation.Immediate, Hand.A0), // Write zero to A0[0]
            new(Operation.Load, Hand.A0, 0, Hand.D1), // Load start offset into D1[0]
            new(Operation.Incr, Hand.A0, 0, Hand.A0), // A0[0]++ (A0[1] discarded)
            new(Operation.Load, Hand.A0, 0, Hand.D1), // Load end offset into D1[0], start is now in D1[1]
            new(Operation.Immediate, Hand.D0), // Write zero to D0[0] (will be cursor)
            new(Operation.Sub, Hand.D1, 0, Hand.D1, 1, Hand.D0), // D0[0] (length) = end - start; D0[1] = cursor
            new(Operation.SetPoint, Hand.A0), // Set top of loop to A0[0]; (A0[1] discarded) 

            // Loop:
            // copy memory to IO device
            new(Operation.Add, Hand.D0, 1, Hand.D1, 1, Hand.D2), // D2[0] = (cursor + start)
            new(Operation.Load, Hand.D2, 0, Hand.D2), // Load char into D2[0] (D2[1] discarded)
            new(Operation.Write, Hand.D2, 0), // Write char (D2[0] discarded)

            // if (cursor < end) loop again -- note, this could be done with an increment, but I'm trying to make it harder.
            new(Operation.Sub, Hand.D0, 0, Hand.D0, 1, Hand.D0), // D0[0] (remains) = length - cursor; D0[1]: length; D0[2]: cursor;
            new(Operation.Incr, Hand.D0, 2, Hand.D0), // D0[0]: cursor++; D0[1]: remains; D0[2]:length;
            new(Operation.Move, Hand.D0, 2, Hand.D0), // D0[0]: length; D0[1]: length; D0[2]: remains; ...
            new(Operation.BranchNz, Hand.D0, 2, Hand.A0, 0), // if remains not zero, go back up loop

            // End of program
            new(Operation.Halt)
        };
        var subject = new ClockHandsVirtualMachine(program, memory);
        Console.WriteLine(subject.ToString());

        var loops = 0;
        while (subject.Step())
        {
            if (loops++ > 1000) throw new Exception("Program stalled?");
        }

        var result = subject.OutputString();
        Console.WriteLine($"\r\n\r\n{loops} steps, result:\r\n{result}");
        Assert.That(result, Is.EqualTo("Hello, World!"));

        Console.WriteLine("\r\n[Halt]");
    }


    [Test]
    public void test_program__hello_with_count_down()
    {
        var memory = new List<int>
        {
            /*0:start*/ 2,
            /*1:length*/ 13,
            /*2..14:data*/'H', 'e', 'l', 'l', 'o', ',', ' ', 'W', 'o', 'r', 'l', 'd', '!',
            /* run off space*/ 'X', 'X', 'X'
        };

        var program = new List<Instruction>
        {
            new(Operation.Immediate, Hand.A0), // Write zero to A0[0]
            new(Operation.Load, Hand.A0, 0, Hand.D1), // Load start offset into D1[0] (will be cursor)
            new(Operation.Incr, Hand.A0, 0, Hand.A0), // A0[0]++ (A0[1] discarded)
            new(Operation.Load, Hand.A0, 0, Hand.D0), // Load length offset into D0[0]
            new(Operation.SetPoint, Hand.A0), // Set top of loop to A0[0]; (A0[1] discarded) 

            // Loop:
            // copy memory to IO device
            new(Operation.Load, Hand.D1, 0, Hand.D2), // Load char into D2[0]
            new(Operation.Write, Hand.D2, 0), // Write char (D2[0] discarded)

            // cursor++; remain--; if (remains > 0) loop again
            new(Operation.Incr, Hand.D1, 0, Hand.D1), // D0[0]: cursor++
            new(Operation.Decr, Hand.D0, 0, Hand.D0), // D0[0]: remains--
            new(Operation.BranchNz, Hand.D0, 0, Hand.A0, 0), // if remains not zero, go back up loop

            // End of program
            new(Operation.Halt)
        };
        var subject = new ClockHandsVirtualMachine(program, memory);
        Console.WriteLine(subject.ToString());

        var loops = 0;
        while (subject.Step())
        {
            if (loops++ > 1000) throw new Exception("Program stalled?");
        }

        var result = subject.OutputString();
        Console.WriteLine($"\r\n\r\n{loops} steps, result:\r\n{result}");
        Assert.That(result, Is.EqualTo("Hello, World!"));

        Console.WriteLine("\r\n[Halt]");
    }
}