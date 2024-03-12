// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;

namespace Friflo.Json.Burst.Utils
{
    [CLSCompliant(true)]
    public static class ValueParser
    {
        private static void SetErrorFalse (string msg, in ReadOnlySpan<byte> value, ref Bytes dst) {
            if (dst.IsCreated()) {
                dst.Clear();
                dst.AppendStr128(msg);
                dst.AppendBytesSpan(value);
            }
        }
        
        public static int ParseInt(in ReadOnlySpan<byte> bytes, ref Bytes valueError, out bool success) {
            success = false;
            valueError.Clear();
            int val         = 0;
            bool positive   = true;
            int first       = 0;
            int limit       = -int.MaxValue;
            var len         = bytes.Length;
            if (len > 0)
            {
                int c = bytes[first];
                if          (c == '-') {
                    positive = false;
                    limit = int.MinValue;
                    first++;
                } else if   (c == '+') {
                    first++;
                }           
            }
            int multLimit = limit / 10;
            
            for (int n = first; n < len; n++)
            {
                int digit = bytes[n] - '0';
                if (digit < 0 || digit > 9) {
                    SetErrorFalse ("Invalid character when parsing integer: ", bytes, ref valueError);
                    return 0;
                }
                if (val < multLimit) {
                    SetErrorFalse ("Value out of range when parsing integer: ", bytes, ref valueError);
                    return 0;
                }
                val *= 10;
                if (val < limit + digit) {
                    SetErrorFalse ("Value out of range when parsing integer: ", bytes, ref valueError);
                    return 0;
                }               
                val -= digit;
            }
            success = true;
            return positive ? -val : val;
        }

        public static long ParseLong(in ReadOnlySpan<byte> bytes, ref Bytes valueError, out bool success) {
            success = false;
            valueError.Clear();
            long val        = 0;
            bool positive   = true;
            int  first      = 0;
            long limit      = -long.MaxValue;
            var len         = bytes.Length;
            if (len > 0)
            {
                int c = bytes[first];
                if          (c == '-') {
                    positive = false;
                    limit = long.MinValue;
                    first++;
                } else if   (c == '+') {
                    first++;
                }           
            }
            long multLimit = limit / 10;

            for (int n = first; n < len; n++)
            {
                int digit = bytes[n] - '0';
                if (digit < 0 || digit > 9) {
                    SetErrorFalse ("Invalid character when parsing long: ", bytes, ref valueError);
                    return 0;
                }
                if (val < multLimit) {
                    SetErrorFalse ("Value out of range when parsing long: ", bytes, ref valueError);
                    return 0;
                }
                val *= 10;
                if (val < limit + digit) {
                    SetErrorFalse ("Value out of range when parsing long: ", bytes, ref valueError);
                    return 0;
                }               
                val -= digit;
            }
            success = true;
            return positive ? -val : val;
        }

        public static double ParseDouble(in ReadOnlySpan<byte> bytes, ref Bytes valueError, out bool success)
        {
            valueError.Clear();
            success         = false;
            bool negative   = false;
            int len         = bytes.Length;
            int n           = 0;
            if (n >= len) {
                SetErrorFalse("Invalid number: ", bytes, ref valueError);
                return 0;
            }

            int c = bytes[n];
            if (c == '-')
            {
                negative = true;
                n++;
            }
            else if (c == '+')
            {
                n++;
            }
            
            int     comma   = -1;
            int     lastDigit = len; 
            long    val     = 0;
            int     exp     = 0;        
            
            for (; n < len; n++)
            {
                c = bytes[n];
                switch (c)
                {
                case '0':   case '1':   case '2':   case '3':   case '4':   
                case '5':   case '6':   case '7':   case '8':   case '9':
                    int digit = c - '0';
                    val = val * 10 + digit;
                    break;
                case '.':
                    if (comma == -1)
                        comma = n;
                    else {
                        SetErrorFalse("Invalid floating point number: ", bytes, ref valueError);
                        return 0;
                    }

                    break;
                case 'e':
                case 'E':
                    lastDigit = n;
                    if (++n < len)
                    {
                        bool negativeExp = false;
                        c = bytes[n];
                        if (c == '-')
                        {
                            negativeExp = true;
                            n++;
                        }
                        else if (c == '+')
                        {
                            n++;
                        }

                        if (n == len) {
                            SetErrorFalse("Invalid floating point number: ", bytes, ref valueError);
                            return 0;
                        }

                        for (; n < len; n++)
                        {
                            c = bytes[n];
                            if (('0' <= c) && (c <= '9'))
                            {
                                digit = c - '0';
                                exp = exp * 10 + digit;
                            }
                            else {
                                SetErrorFalse("Invalid floating point number: ", bytes, ref valueError);
                                return 0;
                            }
                        }
                        if (negativeExp)
                            exp = -exp;
                        break;
                    }
                    
                    SetErrorFalse ("Invalid floating point number: ", bytes, ref valueError);
                    return 0;
                default:
                    SetErrorFalse ("Invalid floating point number: ", bytes, ref valueError);
                    return 0;
                }
            }
            double d = val;
            valueError.Clear();
            if (comma >= 0)
            {
                int decimals = lastDigit - comma - 1;
                exp -= decimals;
                int dif = -308 - exp;
                if (dif > 0)
                {
                    d /=  PowTable[dif];
                    exp = -308;
                }
            }
            
            if      (exp > 0)
            {
                if (exp < 309)
                    d *=  PowTable[exp];
                else
                    d =  1.0 / 0.0;
            }
            else if (exp < 0)
            {
                if (-exp < 309)
                    d /=  PowTable[-exp];
                else
                    d = 0.0;                
            }
            success = true;
            return negative ? -d : d;
        }
        
        private static readonly double[] PowTable = CreatePowTable ();

        private static double[] CreatePowTable ()
        {
            // max exponent: 4.9e-324 see Double.MIN_VALUE
            int max = 309;
            double[] powTable = new double[max];        
            for (int n = 0; n < max; n++)
                powTable[n] = Math. Pow (10, n);
            return powTable;
        }
        
        //  Exponentiation by squaring. == Math.pow (base, exp)
        /*  private static int ipow(int base, int exp)
        {
            int result = 1;
            while (exp != 0)
            {
                if ((exp & 1) != 0)
                    result *= base;
                exp >>= 1;
                base *= base;
            }
            return result;
        } */
        
        // --- NON_CLS
        private static bool TryParseULong(in ReadOnlySpan<byte> bytes, out ulong result) {
            var len             = bytes.Length;
            Span<char> charBuf  = stackalloc char[len];
            for (int n = 0; n < len; n++) {
                charBuf[n] = (char)bytes[n];
            }
            return MathExt.TryParseULong(charBuf, NumberStyles.None, NumberFormatInfo.InvariantInfo, out result);
        }

        internal static ulong ParseULongStd(in ReadOnlySpan<byte> bytes, ref Bytes valueError, out bool success) {
            valueError.Clear();
            success = true;
            if (TryParseULong(bytes, out ulong result)) {
                return result;
            }
            success = false;
            SetErrorFalse ("Parsing ulong failed. val: ", bytes, ref valueError);
            return 0;
        }
        
        private static bool TryParseUInt(in ReadOnlySpan<byte> bytes, out uint result) {
            var len             = bytes.Length;
            Span<char> charBuf  = stackalloc char[len];
            for (int n = 0; n < len; n++) {
                charBuf[n] = (char)bytes[n];
            }
            return MathExt.TryParseUInt(charBuf, NumberStyles.None, NumberFormatInfo.InvariantInfo, out result);
        }

        internal static uint ParseUIntStd(in ReadOnlySpan<byte> bytes, ref Bytes valueError, out bool success) {
            valueError.Clear();
            success = true;
            if (TryParseUInt(bytes, out uint result)) {
                return result;
            }
            success = false;
            SetErrorFalse ("Parsing uint failed. val: ", bytes, ref valueError);
            return 0;
        }

        // --- double / float
        private static bool TryParseDouble(in ReadOnlySpan<byte> bytes, out double result) {
            var len             = bytes.Length;
            Span<char> charBuf  = stackalloc char[len];
            for (int n = 0; n < len; n++) {
                charBuf[n] = (char)bytes[n];
            }
            return MathExt.TryParseDouble(charBuf, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result);
        }

        public static double ParseDoubleStd(in ReadOnlySpan<byte> bytes, ref Bytes valueError, out bool success) {
            valueError.Clear();
            success = true;
            if (TryParseDouble(bytes, out double result)) {
                if (double.IsInfinity(result) || double.IsNegativeInfinity(result)) {
                    SetErrorFalse("double value out of range. val: ", bytes, ref valueError);
                    success = false;
                    return 0;
                }
                return result;
            }
            success = false;
            SetErrorFalse ("Parsing double failed. val: ", bytes, ref valueError);
            return 0;
        }

        private static bool TryParseFloat(in ReadOnlySpan<byte> bytes, out float result) {
            int len             = bytes.Length;
            Span<char> charBuf  = stackalloc char[len];
            for (int n = 0; n < len; n++) {
                charBuf[n] = (char)bytes[n];
            }
            return MathExt.TryParseFloat(charBuf, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result);
        }

        public static float ParseFloatStd(in ReadOnlySpan<byte> bytes, ref Bytes valueError, out bool success) {
            valueError.Clear();
            success = true;
            if (TryParseFloat(bytes, out float result)) {
                if (float.IsInfinity(result) || float.IsNegativeInfinity(result)) {
                    SetErrorFalse ("float value out of range. val: ", bytes, ref valueError);
                    success = false;
                    return 0;
                }
                return result;
            }
            success = false;
            SetErrorFalse ("Parsing double failed. val: ", bytes, ref valueError);
            return 0;
        }
    }

}