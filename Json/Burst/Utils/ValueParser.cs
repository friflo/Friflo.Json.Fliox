// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
    using Str128 = Unity.Collections.FixedString128;
#else
    using Str32 = System.String;
    using Str128 = System.String;
#endif

namespace Friflo.Json.Burst.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public struct ValueParser : IDisposable
    {
        private     Str32   @true;
        private     Str32   @false;
        private     Str32   _1;
        private     Str32   _0;
        private     bool    initialized;
#if !UNITY_5_3_OR_NEWER
        public      char[]  charBuf;
#endif
        public const int MaxGuidLength = 68; // {0x12345678,0x1234,0x1234,{0x12,0x34,0x12,0x34,0x56,0x78,0x9a,0xbc}}


        public void InitValueParser() {
            if (initialized)
                return;
            initialized = true;
            @true =     "true";
            @false =    "false";
            _1 =        "1";
            _0 =        "0";
#if !UNITY_5_3_OR_NEWER
            // at least a minimum of MaxGuidLength (68)
            charBuf = new char[128];
#endif
        }
        
        public void Dispose() {
        }
        
        private static void SetErrorFalse (in Str128 msg, ref Bytes value, ref Bytes dst) {
            if (dst.buffer.IsCreated()) {
                dst.Clear();
                dst.AppendStr128(in msg);
                dst.AppendBytes(ref value);
            }
        }
        
        public int ParseInt(ref Bytes bytes, ref Bytes valueError, out bool success) {
            success = false;
            valueError.Clear();
            int val = 0;
            bool positive= true;
            ref var str = ref bytes.buffer.array;
            int first = bytes.start;
            int limit = -int.MaxValue;
            if (bytes.end > bytes.start)
            {
                int c = str[first];
                if          (c == '-') {
                    positive = false;
                    limit = int.MinValue;
                    first++;
                } else if   (c == '+') {
                    first++;
                }           
            }
            int multLimit = limit / 10;
            
            for (int n = first; n < bytes.end; n++)
            {
                int digit = str[n] - '0';
                if (digit < 0 || digit > 9) {
                    SetErrorFalse ("Invalid character when parsing integer: ", ref bytes, ref valueError);
                    return 0;
                }
                if (val < multLimit) {
                    SetErrorFalse ("Value out of range when parsing integer: ", ref bytes, ref valueError);
                    return 0;
                }
                val *= 10;
                if (val < limit + digit) {
                    SetErrorFalse ("Value out of range when parsing integer: ", ref bytes, ref valueError);
                    return 0;
                }               
                val -= digit;
            }
            success = true;
            return positive ? -val : val;
        }

        public long ParseLong(ref Bytes bytes, ref Bytes valueError, out bool success) {
            success = false;
            valueError.Clear();
            long val = 0;
            bool positive= true;
            ref var str = ref bytes.buffer.array;
            int first = bytes.start;
            long limit = -long.MaxValue;
            if (bytes.end > bytes.start)
            {
                int c = str[first];
                if          (c == '-') {
                    positive = false;
                    limit = long.MinValue;
                    first++;
                } else if   (c == '+') {
                    first++;
                }           
            }
            long multLimit = limit / 10;

            for (int n = first; n < bytes.end; n++)
            {
                int digit = str[n] - '0';
                if (digit < 0 || digit > 9) {
                    SetErrorFalse ("Invalid character when parsing long: ", ref bytes, ref valueError);
                    return 0;
                }
                if (val < multLimit) {
                    SetErrorFalse ("Value out of range when parsing long: ", ref bytes, ref valueError);
                    return 0;
                }
                val *= 10;
                if (val < limit + digit) {
                    SetErrorFalse ("Value out of range when parsing long: ", ref bytes, ref valueError);
                    return 0;
                }               
                val -= digit;
            }
            success = true;
            return positive ? -val : val;
        }

        public double ParseDouble(ref Bytes bytes, ref Bytes valueError, out bool success)
        {
            valueError.Clear();
            success = false;
            bool negative = false;
            ref var str = ref bytes.buffer.array;
            int end = bytes.end;
            int n = bytes.start;
            if (n >= end) {
                SetErrorFalse("Invalid number: ", ref bytes, ref valueError);
                return 0;
            }

            int c = str[n];
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
            int     lastDigit = end; 
            long    val     = 0;
            int     exp     = 0;        
            
            for (; n < end; n++)
            {
                c = str[n];
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
                        SetErrorFalse("Invalid floating point number: ", ref bytes, ref valueError);
                        return 0;
                    }

                    break;
                case 'e':
                case 'E':
                    lastDigit = n;
                    if (++n < end)
                    {
                        bool negativeExp = false;
                        c = str[n];
                        if (c == '-')
                        {
                            negativeExp = true;
                            n++;
                        }
                        else if (c == '+')
                        {
                            n++;
                        }

                        if (n == end) {
                            SetErrorFalse("Invalid floating point number: ", ref bytes, ref valueError);
                            return 0;
                        }

                        for (; n < end; n++)
                        {
                            c = str[n];
                            if (('0' <= c) && (c <= '9'))
                            {
                                digit = c - '0';
                                exp = exp * 10 + digit;
                            }
                            else {
                                SetErrorFalse("Invalid floating point number: ", ref bytes, ref valueError);
                                return 0;
                            }
                        }
                        if (negativeExp)
                            exp = -exp;
                        break;
                    }
                    
                    SetErrorFalse ("Invalid floating point number: ", ref bytes, ref valueError);
                    return 0;
                default:
                    SetErrorFalse ("Invalid floating point number: ", ref bytes, ref valueError);
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
        
        private readonly static double[] PowTable = CreatePowTable ();

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
    
        public bool ParseBoolean(ref Bytes bytes, ref Bytes valueError, out bool success) {
            success = true;
            valueError.Clear();
            if (bytes.IsEqual32(in @true)   || bytes.IsEqual32(in _1))
                return true;
            if (bytes.IsEqual32(in @false)  || bytes.IsEqual32(in _0))
                return false;
            success = false;
            SetErrorFalse("Invalid boolean. Expected true/false but found: ", ref bytes, ref valueError);
            return false;
        }
        
        private bool ParseDoubleInternal(ref Bytes bytes, out double result) {
#if UNITY_5_3_OR_NEWER
            String val = bytes.ToString();
            return double.TryParse(val, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result);
#else
            if (bytes.Len > charBuf.Length) {
                result = default;
                return false;
            }
            byte[] arr = bytes.buffer.array;
            int pos = bytes.start;
            int len = bytes.Len;
            for (int n = 0; n < len; n++)
                charBuf[n] = (char)arr[pos + n];
            ReadOnlySpan<char> span = new ReadOnlySpan<char> (charBuf, 0 , len);
            return double.TryParse(span, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result);
#endif
        }

        public double ParseDoubleStd(ref Bytes bytes, ref Bytes valueError, out bool success) {
            valueError.Clear();
            success = true;
            if (ParseDoubleInternal(ref bytes, out double result)) {
                if (double.IsInfinity(result) || double.IsNegativeInfinity(result)) {
                    SetErrorFalse("double value out of range. val: ", ref bytes, ref valueError);
                    success = false;
                    return 0;
                }
                return result;
            }
            success = false;
            SetErrorFalse ("Parsing double failed. val: ", ref bytes, ref valueError);
            return 0;
        }

        private bool ParseFloatInternal(ref Bytes bytes, out float result) {
#if UNITY_5_3_OR_NEWER
            String val = bytes.ToString();
            return float.TryParse(val, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result);
#else
            if (bytes.Len > charBuf.Length) {
                result = default;
                return false;
            }
            byte[] arr = bytes.buffer.array;
            int pos = bytes.start;
            int len = bytes.Len;
            for (int n = 0; n < len; n++)
                charBuf[n] = (char)arr[pos + n];
            ReadOnlySpan<char> span = new ReadOnlySpan<char> (charBuf, 0 , len);
            return float.TryParse(span, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result);
#endif
        }

        public float ParseFloatStd(ref Bytes bytes, ref Bytes valueError, out bool success) {
            valueError.Clear();
            success = true;
            if (ParseFloatInternal(ref bytes, out float result)) {
                if (float.IsInfinity(result) || float.IsNegativeInfinity(result)) {
                    SetErrorFalse ("float value out of range. val: ", ref bytes, ref valueError);
                    success = false;
                    return 0;
                }
                return result;
            }
            success = false;
            SetErrorFalse ("Parsing double failed. val: ", ref bytes, ref valueError);
            return 0;
        }
    }

}