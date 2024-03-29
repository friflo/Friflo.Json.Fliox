﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;

// JSON_BURST_TAG
using Str32 = System.String;

namespace Friflo.Json.Burst.Utils
{
    [CLSCompliant(true)]
    public struct ValueFormat : IDisposable
    {
        // need to fit longest string created by double.ToString(). E.g. "-1.9007199254740992e+308"
        // 9007199254740992 = max mantissa 2^53
        private char[]      charBuf;    
        private ValueArray<int> digit;
        private Str32       @true;
        private Str32       @false;
        private Str32       nan;
        private Str32       infinity;
        private Str32       negInfinity;
        private Str32       zero;

        private static readonly int maxDigit = 20;

        public void InitTokenFormat() {
            if (!digit.IsCreated()) {
                digit   = new ValueArray<int>(maxDigit);
                charBuf = new char[32];
            }
            @true =         "true";
            @false =        "false";
            infinity =      "Infinity";
            negInfinity =   "-Infinity";
            nan =           "NaN";
            zero =          "0.0";
        }

        public void Dispose() {
            if (digit.IsCreated())
                digit.Dispose();
        }

        public void AppendBool (ref Bytes dst, bool val)
        {
            if (val)
                dst.AppendStr32(@true);
            else
                dst.AppendStr32(@false);
        }

        public void AppendInt (ref Bytes dst, int val)
        {
            dst.EnsureCapacity (dst.end + 12); //  -2147483648 - 11 bytes + 1 for safety :)
            var dstArr = dst.buffer;
            if (val == 0) {
                dstArr[dst.end++] = (byte)'0'; 
                return;
            }

            if (val < 0) {
                dstArr[dst.end++] = (byte)'-'; 
                val = -val;
            }
            int i = val;
            int len = 0;
            ref var digits = ref digit.array;
            while (i > 0) {
                digits[len++] = i % 10;
                i /= 10;                
            }
            int last = dst.end + len - 1;
            for (int n = 0; n < len; n++)
                dstArr[last - n] = (byte)('0' + digits[n]);
            dst.end += len;
        }

        public void AppendLong (ref Bytes dst, long val)
        {
            if (val == 0) {
                dst.AppendChar('0');
                return;
            }

            if (val < 0) {
                dst.AppendChar('-');
                val = -val;
            }
            long i = val;
            int len = 0;
            while (i > 0)
            {
                digit.array[len++] = (int)(i % 10);
                i /= 10;                
            }
            dst.EnsureCapacity (len);
            int last = dst.end + len - 1;
            for (int n = 0; n < len; n++)
                dst.buffer[last - n] = (byte)('0' + digit.array [n]);
            dst.end += len;
        }
        
        // NON_CLS
#pragma warning disable 3001  // Warning CS3001 : Argument type '...' is not CLS-compliant
        public void AppendULong (ref Bytes dst, ulong val)
        {
            if (val == 0) {
                dst.AppendChar('0');
                return;
            }
            ulong i = val;
            int len = 0;
            while (i > 0)
            {
                digit.array[len++] = (int)(i % 10);
                i /= 10;                
            }
            dst.EnsureCapacity (len);
            int last = dst.end + len - 1;
            for (int n = 0; n < len; n++)
                dst.buffer[last - n] = (byte)('0' + digit.array [n]);
            dst.end += len;
        }

        private static int GetExponent(double d)
        {
            return (int)(((BitConverter.DoubleToInt64Bits(d) & 0x7FF0000000000000L) >> 52) - 1023);
        }

        //              exp2 * log10(2) = exp10 * log10 (10)         , log10(10)= 1
        // readonly static double log2 = Math. Log10 (2); // ~ 0.3
    
        // alternative: exp2 * log(2)   = exp10 * log (10)
        // readonly static double log2b = Math. Log (2) / Math. Log (10); // ~ 0.3
    
        private static readonly int         powNeutral = 323;   // 324: =>  0.0
        private static readonly int         powMax   = 309; // 309: => +Infity
        private static readonly double[]    PowTable = CreatePowTable ();
    
        private static double[] CreatePowTable ()
        {
            // max exponent: 4.9e-324 see Double.MIN_VALUE
            double[] powTable = new double[powNeutral + powMax];
            for (int n = -powNeutral; n < powMax; n++)
                powTable[n + powNeutral] = Math. Pow (10, n);
            return powTable;
        }
    
        private static double Pow10(int exp)
        {
            if (exp >= powMax)
                return 1.0 / 0.0;  // +Infity
            if (exp < -powNeutral)
                return 0.0;
            return PowTable[powNeutral + exp];
        }
        
        public void AppendFlt (ref Bytes dst, float val) {
#if NETSTANDARD2_0
            var chars   = val.ToString(CultureInfo.InvariantCulture).AsSpan();
            var len     = chars.Length;
#else
            var chars = charBuf;
            if (!val.TryFormat(chars, out int len, null, CultureInfo.InvariantCulture)) {
                throw new InvalidOperationException($"format float failed. value: {val}");
            }
#endif
            dst.EnsureCapacity(len);
            var buf     = dst.buffer;
            var start   = dst.end;
            for (int i = 0; i < len; i++) {
                buf[start + i] = (byte)chars[i];
            }
            dst.end += len;
        }
        
        public void AppendDbl (ref Bytes dst, double val) {
#if NETSTANDARD2_0
            var chars   = val.ToString(CultureInfo.InvariantCulture).AsSpan();
            var len     = chars.Length;
#else
            var chars = charBuf;
            if (!val.TryFormat(chars, out int len, null, CultureInfo.InvariantCulture)) {
                throw new InvalidOperationException($"format double failed. value: {val}");
            }
#endif
            dst.EnsureCapacity(len);
            var buf     = dst.buffer;
            var start   = dst.end;
            for (int i = 0; i < len; i++) {
                buf[start + i] = (byte)chars[i];
            }
            dst.end += len;
        }
    
        /// <summary>replaced by <see cref="AppendFlt"/></summary>
        public void AppendFltOld (ref Bytes dst, float val)
        {
            if (val == 0.0f) {
                dst.AppendStr32(zero);
                return;
            }

            // if (val == 1.0f / 0.0f) {
            if (float.IsPositiveInfinity(val)) {
                dst.AppendStr32(infinity);
                return;
            }

            // if (val == -1.0f / 0.0f) {
            if (float.IsNegativeInfinity(val)) {
                dst.AppendStr32(negInfinity);
                return;
            }

            if (Single.IsNaN(val)) {
                dst.AppendStr32(nan);
                return;
            }

            int exp         = GetExponent(val);
            int shiftExp10 =      (3 * exp / 10) - 7;  // ~ 0.3

            double valShifted = (val * PowTable[powNeutral-shiftExp10]); // = Math.pow(10, -shiftExp10);

            long bits = BitConverter.DoubleToInt64Bits (valShifted);
            bool negative =     ((ulong) bits & 0x8000000000000000L) != 0;
            int exp2  =        (int) ((bits & 0x7ff0000000000000L) >> 52) - 1023;
            int significand =  (int) (((bits & 0x000fffffffffffffL) |
                                              0x0010000000000000L) >> (52-23));
            int shift = exp2 - 23;
            int sigShifted = shift >= 0 ? significand << shift : significand >> -shift;
    //      System.out.println("\nval:        " + val);
    //      System.out.println("sigShifted: " + sigShifted + "    shilft: " + shift);
    //      System.out.println("shiftExp10: " + shiftExp10);
        
            WriteDecimal (ref dst, sigShifted, shiftExp10, negative);
    //      System.out.println("string:     " + this);
        }
        
        /// <summary>replaced by <see cref="AppendDbl"/></summary>
        public void AppendDblOld (ref Bytes dst, double val)
        {
            if (val == 0.0) {
                dst.AppendStr32(zero);
                return;
            }

            // if (val == 1.0 / 0.0) {
            if (double.IsPositiveInfinity(val)) {
                dst.AppendStr32(infinity);
                return;
            }

            // if (val == -1.0 / 0.0) {
            if (double.IsNegativeInfinity(val)) {
                dst.AppendStr32(negInfinity);
                return;
            }

            if (Double.IsNaN(val)) {
                dst.AppendStr32(nan);
                return;
            }

            int exp         = GetExponent(val);
            //int shiftExp10 =     (int)(((double)exp) * log2) - 16;  // ~ 0.3
            int shiftExp10 =       (3 * exp / 10) - 16;  // ~ 0.3

            double factor = Pow10(-shiftExp10); // = Math.pow(10, -shiftExp10);
            // if (factor == 0.0 || factor == (1.0 / 0.0)) {
            if (factor == 0.0 || double.IsPositiveInfinity(factor)) {
                string str = val.ToString(NumberFormatInfo.InvariantInfo); // TODO not Burst compatible 
                dst.AppendString(str);
                return;
            }

            double valShifted = val * factor;       
            long bits = BitConverter.DoubleToInt64Bits (valShifted);
            bool negative =     ((ulong) bits & 0x8000000000000000L) != 0;
            long exp2  =       ((bits & 0x7ff0000000000000L) >> 52) - 1023;
            long significand =  (bits & 0x000fffffffffffffL) |
                                        0x0010000000000000L;
            int shift = (int) (exp2 - 52);
            long sigShifted = shift >= 0 ? significand << shift : significand >> -shift;
    //      System.out.println("\nval:        " + val);
    //      System.out.println("sigShifted: " + sigShifted + "    shilft: " + shift);
    //      System.out.println("shiftExp10: " + shiftExp10);
        
            WriteDecimal (ref dst, sigShifted, shiftExp10, negative);
    //      System.out.println("string:     " + this);
        }
    
        // >= 10.000.000 => 1.0E7   <= 0.0001 => 1.0E-4
        private void WriteDecimal(ref Bytes dst, long sig, int shiftExp10, bool negative)
        {
            long i = sig;
            int digitNum = 0;
            int first = 0;
            while (i > 0)
            {
                int d = (int)(i % 10);
                if (d != 0)
                {
                    first = digitNum;
                    do
                    {
                        digit.array[digitNum++] = (int)(i % 10);
                        i /= 10;
                    }
                    while (i > 0);
                    break;
                }
                digit.array[digitNum++] = d;
                i /= 10;                
            }
            int end = dst.end;
            int pos = end;      
            int exp = shiftExp10 + digitNum - 1;

            // --- render in "computerized scientific notation". E.g. -1.23E-300
            if (exp >= +7 || -4 >= exp)
            {
                dst.EnsureCapacity (digitNum + 8); // digitNum + worst cast: -1.0E-300  => 8
                var bytes = dst.buffer;
                if (negative)
                    bytes[pos++] = (byte) '-';
                bytes[pos++] = (byte)('0' + digit.array [digitNum- 1]);
                bytes[pos++] = (byte) '.';
                int from = digitNum - 2;
                if (from >= first)
                {
                    for (int n = from; n >= first; n--)
                        bytes[pos++] = (byte)('0' + digit.array [n]);
                }
                else
                    bytes[pos++] = (byte) '0';
                
                bytes[pos++]= (byte) 'E';
                if (exp < 0)
                {
                    bytes[pos++]= (byte) '-';           
                    exp = -exp;
                }
    //          else
    //              bytes[pos++]= '+';
            
                // exponent -> decimal
                i = exp;
                digitNum = 0;
                while (i > 0)
                {
                    int d = (int)(i % 10);
                    digit.array[digitNum++] = d;
                    i /= 10;                
                }
                for (int n = digitNum - 1; n >= 0; n--)
                    bytes[pos++] = (byte)('0' + digit.array [n]);     
            }
            else
            // --- render in common presentation. E.g: 123.456
            {
                dst.EnsureCapacity (digitNum + 6); // digitNum + worst cast: -0.0001
                var bytes = dst.buffer;
                if (negative)
                    bytes[pos++] = (byte) '-';
                if (exp < 0)
                {
                    bytes[pos++] = (byte) '0';
                    bytes[pos++] = (byte) '.';
                    for (int n = -exp; n > 1; n--)
                        bytes[pos++] = (byte) '0';
                    for (int n = digitNum - 1; n >= first; n--)
                        bytes[pos++] = (byte)('0' + digit.array [n]);
                }
                else
                {
                    int to = digitNum - exp - 1;
                    int n = digitNum - 1;
                    for ( ; n >= to; n--)
                        bytes[pos++] = (byte)('0' + digit.array [n]);
                    bytes[pos++] = (byte) '.';
                    if (to > first)
                    {
                        for ( ; n >= first; n--)
                            bytes[pos++] = (byte)('0' + digit.array [n]);
                    }
                    else
                        bytes[pos++] = (byte) '0';
                }
            }
        
            dst.end = pos;
        }
    }
}
