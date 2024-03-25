// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;

// #pragma warning disable CS3001  // Argument type 'ulong' is not CLS-compliant

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public partial struct MsgWriter
    {
        // --- double
        public void WriteFloat64(double val) {
            var data    = Reserve(9);               // val: 9
            Write_float64_pos(data, pos, val);
        }
        
        public void WriteKeyFloat64(int keyLen, long key, double val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 9);       // key: 1 + 8,  val: 9
            WriteKeyFix(data, cur, keyLen, key);
            Write_float64_pos(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyFloat64(int keyLen, long key, double? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyFloat64(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key, ref count);
        }
        
        public void WriteKeyFloat64(ReadOnlySpan<byte> key, double val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 9);  // key: 2 + keyLen,  val: 9
            WriteKeySpan(data, ref cur, key);
            Write_float64_pos(data, cur, val);
        }
        
        public void WriteKeyFloat64(ReadOnlySpan<byte> key, double? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyFloat64(key, val.Value);
                return;
            }
            WriteKeyNil(key, ref count);
        }
        
        // ----------------------------------- utils ----------------------------------- 
        private void Write_float64_pos(byte[]data, int cur, double val)
        {
            int int32 = (int)val;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (int32 == val)
            {
                // case: double value can be encoded as int
                switch (int32) {
                    case > ushort.MaxValue:
                        break;
                    case > byte.MaxValue:
                        data[cur]   = (byte)MsgFormat.uint16;
                        BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(data, cur + 1, 2), (ushort)int32);
                        pos = cur + 3;
                        return;
                    case > sbyte.MaxValue: 
                        data[cur]   = (byte)MsgFormat.uint8;
                        data[cur + 1] = (byte)int32;
                        pos = cur + 2;
                        return;
                    case >= 0:
                        data[cur]   = (byte)int32;
                        pos = cur + 1;
                        return;
                    case >= -32:
                        data[cur]   = (byte)int32;
                        pos = cur + 1;
                        return;
                    case >= sbyte.MinValue:
                        data[cur]   = (byte)MsgFormat.int8;
                        data[cur + 1] = (byte)int32;
                        pos = cur + 2;
                        return;
                    case >= short.MinValue:
                        data[cur]   = (byte)MsgFormat.int16;
                        BinaryPrimitives.WriteInt16BigEndian(new Span<byte>(data, cur + 1, 2), (short)int32);
                        pos = cur + 3;
                        return;
                }
            }
            var flt = (float)val;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (flt == val) {
                // case: double value can be encoded as float
                data[cur]   = (byte)MsgFormat.float32;
#if NETSTANDARD2_0
                throw new NotSupportedException();
#else
                var bits32  = BitConverter.SingleToInt32Bits(flt);
                BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(data, cur + 1, 4), bits32);
#endif
                pos = cur + 5;
                return;
            }
            data[cur]   = (byte)MsgFormat.float64;
            var bits64  = BitConverter.DoubleToInt64Bits(val);
            BinaryPrimitives.WriteInt64BigEndian(new Span<byte>(data, cur + 1, 8), bits64);
            pos = cur + 9;
        }
    }
}