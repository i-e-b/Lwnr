namespace LwnrCore.Helpers;

/// <summary>
/// Golay encoding and decoding.
/// This is a forward error correcting code with the advantage
/// that it can correct up to 3 errors in each 3 bytes of transmission
/// without needing much in the way of processing power or working RAM.
///
/// It does however expand the data to twice its original size.
/// 
/// Adapted from http://aqdi.com/articles/using-the-golay-error-detection-and-correction-code-3/
///
/// This does not do bit interleaving
/// </summary>
public static class GolayCodec
{
    // states
    private const int Left = 0;
    private const int Mid = 1;
    private const int Right = 2;

    /// <summary>
    /// Encode source data to destination.
    /// This will double the data size
    /// </summary>
    public static byte[] Encode(byte[] source, out int length)
    {
        var padding = 2 - (source.Length + 2) % 3;
        var finalLength = (source.Length + padding) * 2;
        var result = new byte[finalLength];

        uint cw = 0;
        int codedBytes = 0;
        int sourceByte = 0;
        int state = Left;

        while (sourceByte < source.Length)
        {
            var b = (uint)source[sourceByte++];
            switch (state)
            {
                case Left: // This byte goes to the bottom of codeword
                    cw = b << 4; // HLx
                    state = Mid;
                    break;

                case Mid: // This byte gets split, and we output a codeword
                    cw |= b >> 4; // HLx + xxH -> HLH : write
                    codedBytes = PushCodeword(Golay(cw), codedBytes, result); // get codeword and put
                    cw = (b & 0x0f) << 8; // Lxx

                    state = Right;
                    break;

                case Right: // we've completed a codeword
                    cw |= b; // Lxx + xHL -> LHL : write
                    codedBytes = PushCodeword(Golay(cw), codedBytes, result); // get codeword and put

                    state = Left;
                    break;
            }
        }

        // push anything left over
        if (state != Left) // if current state is "Left" prev state was "Right", and we don't have any trailing data 
        {
            codedBytes = PushCodeword(Golay(cw), codedBytes, result); // get codeword and put
        }

        length = codedBytes;
        return result;
    }


    /// <summary>
    /// Decode original data originally `Encode`d.
    /// This may have one or two extra bytes of padding.
    /// </summary>
    public static byte[] Decode(byte[] encoded, out int totalErrors)
    {
        if (encoded.Length % 3 != 0) throw new Exception("Incomplete data");
        var result = new byte[encoded.Length / 2]; // TODO: figure out how to un-pad?

        totalErrors = 0;
        int codeBytes = 0;
        int resultBytes = 0;
        int state = Left;

        while (codeBytes < encoded.Length)
        {
            var codeWord = PullCodeword(ref codeBytes, encoded); // this gives us 3 nybbles
            var dataWord = Correct(codeWord, out var errs);

            totalErrors += errs;

            switch (state)
            {
                case Left: // cw = HL(H)
                    result[resultBytes++] = (byte)(dataWord >> 4); // HL
                    result[resultBytes] = (byte)((dataWord & 0x0f) << 4); // Hx
                    state = Right;
                    break;

                case Right: // cw = (L)HL
                    result[resultBytes++] |= (byte)((dataWord >> 8) & 0x0f); // xL
                    result[resultBytes++] = (byte)(dataWord & 0xff); // HL
                    state = Left;
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Push 3 bytes of coded data into the result based
    /// on 12 bits of source data
    /// </summary>
    private static uint PullCodeword(ref int codeBytes, byte[] encoded)
    {
        uint codeWord = 0;

        codeWord += (uint)(encoded[codeBytes++] << 16);
        codeWord += (uint)(encoded[codeBytes++] << 8);
        codeWord += (uint)(encoded[codeBytes++] << 0);

        return codeWord;
    }


    /// <summary>
    /// Push 3 bytes of coded data into the result based
    /// on 12 bits of source data
    /// </summary>
    private static int PushCodeword(uint codeWord, int codedBytes, byte[] result)
    {
        result[codedBytes++] = (byte)((codeWord >> 16) & 0xff); // 23-16
        result[codedBytes++] = (byte)((codeWord >> 8) & 0xff); // 15- 8
        result[codedBytes++] = (byte)((codeWord >> 0) & 0xff); //  7- 0

        return codedBytes;
    }

    private const int Poly = 0xAE3; // or use the other polynomial, 0xC75

    /// <summary>
    /// Calculate a [23,12] Golay codeword.
    /// input should be 12 data-bits in the least significant bits
    /// return data is 11 check-bits plus 12 data bits.
    /// </summary>
    private static uint Golay(uint dataWord)
    {
        var codeWord = dataWord & 0xfff; // lower 12 bits only

        for (int i = 1; i <= 12; i++) // each bit of data
        {
            if ((codeWord & 1) > 0) // test bit
            {
                codeWord ^= Poly; // XOR with polynomial
            }

            codeWord >>= 1; // shift intermediate
        }

        var cw = (codeWord << 12) | dataWord;
        cw &= 0x7f_ff_ff; // 23 bits only
        return cw;
    }

    /// <summary>
    /// The syndrome is the remainder left after the codeword is divided by
    /// the generating polynomial, mod 2.
    /// If the codeword is a valid Golay codeword, the syndrome is zero,
    /// else non-zero.
    ///
    /// This version is intended for very small micro-controllers,
    /// so doesn't do any caching or table lookups
    /// </summary>
    private static uint Syndrome(uint cw)
    {
        cw &= 0x7f_ff_ff; // 23 bits only
        for (int i = 1; i <= 12; i++) // each data bit
        {
            if ((cw & 1) > 0) // test data bit
            {
                cw ^= Poly; // xor polynomial
            }

            cw = (cw >> 1) & 0x7f_ff_ff; // shift intermediate
        }

        return cw << 12;
    }

    /// <summary>
    /// Calculate the Hamming weight of a code-word.
    /// This is used to determine if the data can be recovered.
    /// </summary>
    private static int Weight(uint cw)
    {
        int bitCount = 0;
        uint k = 0;

        // 23 bits, up to 6 nybbles
        while ((k < 6) && (cw > 0))
        {
            bitCount += _nybbleBitCounts[cw & 0x0f];
            cw >>= 4;
            k++;
        }

        return bitCount;
    }

    private static readonly byte[] _nybbleBitCounts = { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };

    /// <summary>
    /// Rotates n bits left in a 23 bit codeword
    /// </summary>
    private static uint RotateLeft(uint cw, uint n)
    {
        for (int i = 1; i <= n; i++)
        {
            if ((cw & 0x40_00_00) != 0)
            {
                cw = (cw << 1) | 1;
            }
            else
            {
                cw <<= 1;
            }
        }

        return cw & 0x7f_ff_ff;
    }

    /// <summary>
    /// Rotates n bits right in a 23 bit codeword
    /// </summary>
    private static uint RotateRight(uint cw, uint n)
    {
        for (int i = 1; i <= n; i++)
        {
            if ((cw & 1) != 0)
            {
                cw = (cw >> 1) | 0x40_00_00;
            }
            else
            {
                cw >>= 1;
            }
        }

        return cw & 0x7f_ff_ff;
    }

    /// <summary>
    /// Correct a Golay [23,12] code-word, returning the corrected code-word.
    /// This will work if there are 3 or less errors in the codeword
    /// (12.5% error rate, factoring in the final parity bit used to pad bytes).
    /// If there are 4 or more errors, the output will be
    /// a valid but incorrect codeword. `errs` is set to the number of errors
    /// corrected.
    /// </summary>
    private static uint Correct(uint cw, out int errs)
    {
        var orig = cw; // initial code word
        errs = 0;

        var s = Syndrome(cw);
        if (s <= 0) return cw; // no errors

        byte w = 3;
        var j = -1;
        uint mask = 0x01;

        while (j < 23) // flip each trial bit
        {
            if (j != -1) // toggle trial bit
            {
                if (j > 0) mask <<= 1; // move to next bit
                cw = orig ^ mask; // flip next trial bit
                w = 2; // lower threshold during this phase IEB: test this!
            }

            s = Syndrome(cw); // look for errors
            if (s <= 0) return cw; // no errors

            uint i; // indices
            for (i = 0; i < 23; i++) // check syndrome of each cyclic shift
            {
                errs = Weight(s);
                if (errs <= w) // syndrome matches error pattern
                {
                    cw ^= s; // remove the error
                    cw = RotateRight(cw, i); // un-rotate data

                    if (j >= 0) // count the bit we just toggled
                    {
                        errs++;
                    }

                    return cw;
                }

                cw = RotateLeft(cw, 1); // rotate to next pattern
                s = Syndrome(cw);
            }

            j++; // next trial bit
        }

        return orig; // no corrections
    }
}