using NUnit.Framework;

// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace LwnrUnitTests;

[TestFixture]
public class ReedSolomonWordTests
{
    private static readonly Random _rnd = new();

    [Test, Repeat(100)]
    public void can_read_and_write_correct_data()
    {
        var original = (_rnd.Next() << 1) ^ (_rnd.Next());
        var encoded = ReedSolomonWord.Encode(original);

        Console.WriteLine($" {original:X8} -> {encoded:X16}");

        var result = ReedSolomonWord.Decode(encoded);
        Console.WriteLine($" {encoded:X16} -> {result:X8}");

        Assert.That(result, Is.EqualTo(original));
    }

    [Test, Repeat(16)]
    public void can_recover_correct_data_from_damaged_data()
    {
        var original = (_rnd.Next() << 1) ^ (_rnd.Next());
        var encoded = ReedSolomonWord.Encode(original);

        Console.WriteLine($" {original:X8} -> {encoded:X16}");

        encoded ^= 0x80000100202000L; // 4 flipped bits

        var result = ReedSolomonWord.Decode(encoded);
        Console.WriteLine($" {encoded:X16} -> {result:X8}");

        Assert.That(result, Is.EqualTo(original));
    }

    [Test, Repeat(16)]
    public void very_damaged_data_throws_an_exception_when_decoding()
    {
        var original = (_rnd.Next() << 1) ^ (_rnd.Next());
        var encoded = ReedSolomonWord.Encode(original);

        Console.WriteLine($" {original:X8} -> {encoded:X16}");

        encoded ^= 4422242428444222; // 16 flipped bits

        Assert.Throws<Exception>(() => { ReedSolomonWord.Decode(encoded); });
    }

    [Test]//, Repeat(16)]
    public void data_can_be_damaged_repeatedly_between_refreshes()
    {
        var original = (_rnd.Next() << 1) ^ (_rnd.Next());
        var encoded = ReedSolomonWord.Encode(original);

        Console.WriteLine($"{original:X8}        -> {encoded:X16}                        {encoded:X16}");

        var damaged = encoded;
        for (int i = 0; i < 16; i++)
        {
            // TODO: this fails if the last symbol is damaged at all (probably an off-by-1)
            var flips = 0x1;//(1L << _rnd.Next(0, 60)) | (1L << _rnd.Next(0, 63));
            damaged ^= flips;
            Console.Write($"{flips:X16} | ");
            
            Console.Write($"{encoded:X16} -> {damaged:x16}");
            Assert.That(damaged, Is.Not.EqualTo(encoded));
            
            var recovered = ReedSolomonWord.Refresh(damaged);
            Console.WriteLine($" => {recovered:X16}");
            Assert.That(recovered, Is.EqualTo(encoded));
            
            damaged = recovered;
        }

        var result = ReedSolomonWord.Decode(encoded);
        Console.WriteLine($" {encoded:X16} -> {result:X8}");

        Assert.That(result, Is.EqualTo(original));
    }

}

/// <summary>
/// Tools for error correcting stored and transmitted data.
/// <p/>
/// This breaks the core data into 4 bit sections, representing a [0..15] RS symbol.
/// </summary>
public static class ReedSolomonWord
{
    private const int extraCodes = 8; // 50% redundancy

    /// <summary>
    /// Encode an original value 32bit to 64bit FEC/ECC
    /// </summary>
    public static long Encode(int value)
    {
        var msg = new int[8]; // 8 nybbles of 0..15
        for (int i = 7; i >= 0; i--)
        {
            msg[i] = value & 0b1111;
            value >>= 4;
        }
        
        
        var gen = gfIrreduciblePoly(extraCodes);
        var mix = new int[msg.Length + gen.Length];//Array(msg.length + gen.length - 1).fill(0);
        for (var i = 0; i < msg.Length; i++) {
            mix[i] = msg[i];
        }

        for (var i = 0; i < msg.Length; i++) {
            var coeff = mix[i];
            if (coeff == 0) continue;
            for (var j = 1; j < gen.Length; j++) {
                mix[i + j] ^= gfMul(gen[j], coeff);
            }
        }

        var outp = new List<int>();
        for (var i = 0; i < msg.Length + gen.Length - 1; i++) { outp.Add(mix[i]); }
        for (var i = 0; i < msg.Length; i++) { outp[i] = msg[i]; }
        
        ulong result = 0UL;

        for (int i = 0; i < outp.Count; i++)
        {
            result = (result << 4) | (uint)(outp[i] & 0x0F);
        }

        return (long)result; // should be 16 nybbles, 8 bytes
    }

    /// <summary>
    /// Attempt to correct any errors in the 64 FEC/ECC.
    /// This should be used to periodically refresh
    /// storage that is prone to errors.
    /// </summary>
    public static long Refresh(long value)
    {
        var uvalue = (ulong)value;
        var msg = new int[16]; // 16 nybbles of 0..15
        for (int i = 15; i >= 0; i--)
        {
            msg[i] = (int)(uvalue & 0x0F);
            uvalue >>= 4;
        }
        
        var syndrome = rsCalcSyndromes(msg, extraCodes);
        if (allZeros(syndrome)) {
            return value;
        }

        var errPoly = rsErrorLocatorPoly(syndrome, extraCodes, 0);

        var errorPositions = rsFindErrors(errPoly.Reverse().ToArray(), msg.Length);

        msg = rsCorrectErrors(msg, syndrome, errorPositions.Reverse().ToArray());

        // recheck result
        var synd2 = rsCalcSyndromes(msg, extraCodes);
        if (allZeros(synd2)) {
            int original = 0;

            for (int i = 0; i < 8; i++)
            {
                original = (original << 4) | (msg[i] & 0x0F);
            }

            return Encode(original); // rebuild polynomial
        }

        throw new Exception("Too many errors (C)");
    }
    
    /// <summary>
    /// Decode a 64bit FEC/ECC to the original 32bit value
    /// </summary>
    public static int Decode(long value)
    {
        var corrected = Refresh(value);
        Console.WriteLine($"{value:X8} -> {corrected:X8}");
        return (int)((corrected >> 32) & 0xFFFFFFFF);
    }

    #region GF 16
    private struct GF16
    {
        public int[] exp = new int[32];
        public int[] log = new int[16];

        public GF16()
        {
        }
        //exp: Array(32).fill(0),
        //log: Array(16).fill(0)
    }

    private static GF16? gf_table;

    private static GF16 gfTable()
    {
        if (gf_table is not null) return gf_table.Value;
        var table = new GF16();
        int x = 1;
        int prim = 19; // critical to get this right!

        for (var i = 0; i < 16; i++)
        {
            table.exp[i] = x & 0x0f;
            table.log[x] = i & 0x0f;
            x <<= 1;
            if ((x & 0x110) != 0) x ^= prim;
            x &= 0x0f;
        }

        for (var i = 15; i < 32; i++)
        {
            table.exp[i] = table.exp[i - 15] & 0x0f;
        }

        gf_table = table;
        return table;
    }

    private static int gfAddSub(int a, int b)
    {
        return (a ^ b) & 0x0f;
    } // add and subtract are same in GF256

    private static int gfMul(int a, int b)
    {
        if (a == 0 || b == 0) return 0;
        var gf = gfTable();
        return gf.exp[gf.log[a] + gf.log[b]];
    }

    private static int gfDiv(int a, int b)
    {
        if (a == 0 || b == 0) return 0;
        var gf = gfTable();
        return gf.exp[((gf.log[a] + 15) - gf.log[b]) % 15];
    }

    private static int gfPow(int n, int p)
    {
        var gf = gfTable();
        return gf.exp[(gf.log[n] * p) % 15];
    }

    private static int gfInverse(int n)
    {
        var gf = gfTable();
        return gf.exp[15 - gf.log[n]];
    }

    private static int[] gfPolyMulScalar(int[] p, int sc)
    {
        // coeff array, scalar
        var res = new int[p.Length]; // Array(p.length).fill(0);
        for (var i = 0; i < p.Length; i++)
        {
            res[i] = gfMul(p[i], sc);
        }

        return res;
    }

    private static int[] gfAddPoly(int[] p, int[] q)
    {
        // add two polynomials
        var len = p.Length >= q.Length ? p.Length : q.Length;
        var res = new int[len]; //Array(len).fill(0);
        for (var i = 0; i < p.Length; i++)
        {
            res[i + len - p.Length] = p[i];
        }

        for (var i = 0; i < q.Length; i++)
        {
            res[i + len - q.Length] ^= q[i];
        }

        return res;
    }

    private static int[] gfMulPoly(int[] p, int[] q)
    {
        // multiply two polynomials
        var res = new int[p.Length + q.Length - 1]; //Array(p.length + q.length - 1).fill(0);
        for (var j = 0; j < q.Length; j++)
        {
            for (var i = 0; i < p.Length; i++)
            {
                res[i + j] = gfAddSub(res[i + j], gfMul(p[i], q[j]));
            }
        }

        return res;
    }

    private static int gfEvalPoly(int[] p, int x)
    {
        // evaluate polynomial 'p' for value 'x', resulting in scalar
        var y = p[0];
        for (var i = 1; i < p.Length; i++)
        {
            y = gfMul(y, x) ^ p[i];
        }

        return y & 0x0f;
    }

    private static int[] gfIrreduciblePoly(int symCount)
    {
        var gen = new[] { 1 };
        for (var i = 0; i < symCount; i++)
        {
            gen = gfMulPoly(gen, new[] { 1, gfPow(2, i) });
        }

        return gen;
    }

    #endregion GF 16

    #region Error correction

    private static int[] removeLeadingZeros(int[] arr)
    {
        var outp = new List<int>();
        var latch = false;
        for (var i = 0; i < arr.Length; i++)
        {
            if (!latch && arr[i] == 0) continue;
            latch = true;
            outp.Add(arr[i]);
        }

        return outp.ToArray();
    }

    private static bool allZeros(int[] arr)
    {
        for (var i = 0; i < arr.Length; i++)
        {
            if (arr[i] != 0) return false;
        }

        return true;
    }

    private static int[] rsCalcSyndromes(int[] msg, int sym)
    {
        var synd = new int[sym + 1]; //Array(sym + 1).fill(0); // with leading zero
        for (var i = 0; i < sym; i++)
        {
            synd[i + 1] = gfEvalPoly(msg, gfPow(2, i));
        }

        return synd;
    }

    private static int[] rsErrorLocatorPoly(int[] synd, int sym, int erases)
    {
        var errLoc = new List<int> { 1 };
        var oldLoc = new List<int> { 1 };

        // TODO: fix the array/List<> mix and reduce allocations

        var syndShift = 0;
        if (synd.Length > sym) syndShift = synd.Length - sym;

        for (var i = 0; i < (sym - erases); i++)
        {
            var kappa = i + syndShift;
            var delta = synd[kappa];
            for (var j = 1; j < errLoc.Count; j++)
            {
                delta ^= gfMul(errLoc[errLoc.Count - (j + 1)], synd[kappa - j]);
            }

            oldLoc.Add(0);
            if (delta != 0)
            {
                if (oldLoc.Count > errLoc.Count)
                {
                    var newLoc = gfPolyMulScalar(oldLoc.ToArray(), delta);
                    oldLoc = new List<int>(gfPolyMulScalar(errLoc.ToArray(), gfInverse(delta)));
                    errLoc = new List<int>(newLoc);
                }

                var scale = gfPolyMulScalar(oldLoc.ToArray(), delta);
                errLoc = new List<int>(gfAddPoly(errLoc.ToArray(), scale.ToArray()));
            }
        }

        errLoc = new List<int>(removeLeadingZeros(errLoc.ToArray()));
        var errCount = errLoc.Count - 1;
        if (errCount - erases > sym) throw new Exception("Too many errors (A)");

        return errLoc.ToArray();
    }

    private static int[] rsFindErrors(int[] locPoly, int len)
    {
        var errs = locPoly.Length - 1;
        var pos = new List<int>();

        for (var i = 0; i <= len; i++)
        {
            var test = gfEvalPoly(locPoly, gfPow(2, i)) & 0x0f;
            var loc = len - 1 - i;
            if (test == 0 && loc >= 0)
            {
                pos.Add(len - 1 - i);
            }
        }

        if (pos.Count != errs) throw new Exception($"Too many errors (B: {pos.Count} != {errs})");

        return pos.ToArray();
    }

    private static int[] rsDataErrorLocatorPoly(int[] pos)
    {
        var one = new[] { 1 };
        var eLoc = new List<int> { 1 };
        for (var i = 0; i < pos.Length; i++)
        {
            var add = gfAddPoly(one, new[] { gfPow(2, pos[i]), 0 });
            eLoc = new List<int>(gfMulPoly(eLoc.ToArray(), add));
        }

        return eLoc.ToArray();
    }

    private static int[] rsErrorEvaluator(int[] synd, int[] errLoc, int n)
    {
        var poly = gfMulPoly(synd, errLoc);
        var len = poly.Length - n;
        for (var i = 0; i < len; i++)
        {
            //poly[i] = poly[i + len]; // out-of-bounds with C#, JS just ignores
            
            if (i + len >= poly.Length) poly[i] = poly[i + len - poly.Length]; //???
            else poly[i] = poly[i + len];
            
            //var idx = (i+len)%poly.Length;
            //poly[i] = poly[idx];
        }

        return poly.Take(poly.Length - len).ToArray();
    }

    private static int[] rsCorrectErrors(int[] msg, int[] synd, int[] pos)
    {
        // Forney algorithm
        var len = msg.Length;
        var coeffPos = new List<int>();
        var rSynd = synd.Reverse().ToArray();
        for (var i = 0; i < pos.Length; i++)
        {
            coeffPos.Add(len - 1 - pos[i]);
        }

        var errLoc = rsDataErrorLocatorPoly(coeffPos.ToArray());
        var errEval = rsErrorEvaluator(rSynd, errLoc, errLoc.Length);

        var chi = new List<int>();
        for (var i = 0; i < coeffPos.Count; i++)
        {
            chi.Add(gfPow(2, coeffPos[i]));
        }

        var E = new int[len]; //Array(len).fill(0);
        for (var i = 0; i < chi.Count; i++)
        {
            var tmp = new List<int>();
            var ichi = gfInverse(chi[i]);
            for (var j = 0; j < chi.Count; j++)
            {
                if (i == j) continue;
                tmp.Add(gfAddSub(1, gfMul(ichi, chi[j])));
            }

            var prime = 1;
            for (var k = 0; k < tmp.Count; k++)
            {
                prime = gfMul(prime, tmp[k]);
            }

            var y = gfEvalPoly(errEval, ichi);
            y = gfMul(gfPow(chi[i], 1), y); // pow?
            E[pos[i]] = gfDiv(y, prime);
        }

        msg = gfAddPoly(msg, E);
        return msg;
    }

    #endregion Error correction
}