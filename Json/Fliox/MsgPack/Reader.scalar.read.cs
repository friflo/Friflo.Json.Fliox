// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Friflo.Json.Fliox.MsgPack
{
    public ref partial struct MsgReader
    {
        // --- fix int + / -
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte read_fixInt(int cur, MsgFormat type) {
            pos = cur + 1;
            return (byte)type;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private sbyte read_fixInt_neg(int cur, MsgFormat type) {
            pos = cur + 1;
            return (sbyte)((int)type - 256);
        }
        

        // --- int8
        private sbyte read_int8(int cur) {
            pos = cur + 2;
            if (pos <= data.Length) {
                return (sbyte)data[cur + 1];
            }
            SetEofErrorType(MsgFormat.int8, cur);
            return 0;
        }
        
        private sbyte read_int8_pos(int cur) {
            var value = read_int8(cur);
            if (value >= 0) {
                return value;
            }
            SetIntRangeError(MsgFormat.int8, value, cur);
            return 0;
        }
        
        
        // --- uint8
        private byte read_uint8(int cur) {
            pos = cur + 2;
            if (pos <= data.Length) {
                return data[cur + 1];
            }
            SetEofErrorType(MsgFormat.uint8, cur);
            return 0;
        }
        
       
        // --- int16
        private short read_int16(int cur) {
            pos = cur + 3;
            if (pos <= data.Length) {
                return BinaryPrimitives.ReadInt16BigEndian(data.Slice(cur + 1, 2));
            }
            SetEofErrorType(MsgFormat.int16, cur);
            return 0;
        }
        
        private short read_int16_range(int cur, int min, int max) {
            var value = read_int16(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetIntRangeError(MsgFormat.int16, value, cur);
            return 0;
        }
        
        
        // --- uint16
        private ushort read_uint16(int cur) {
            pos = cur + 3;
            if (pos <= data.Length) {
                return BinaryPrimitives.ReadUInt16BigEndian(data.Slice(cur + 1, 2));
            }
            SetEofErrorType(MsgFormat.uint16, cur);
            return 0;
        }
        
        private ushort read_uint16_max(int cur, int max) {
            var value = read_uint16(cur);
            if (value <= max) {
                return value;
            }
            SetIntRangeError(MsgFormat.uint16, value, cur);
            return 0;
        }
        
        
        // --- int32
        private int read_int32(int cur) {
            pos = cur + 5;
            if (pos <= data.Length) {
                return BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
            }
            SetEofErrorType(MsgFormat.int32, cur);
            return 0;
        }
       
        private int read_int32_range(int cur, int min, int max) {
            var value = read_int32(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetIntRangeError(MsgFormat.int32, value, cur);
            return 0;
        }
        
        
        // --- uint32
        private uint read_uint32(int cur) {
            pos = cur + 5;
            if (pos <= data.Length) {
                return BinaryPrimitives.ReadUInt32BigEndian(data.Slice(cur + 1, 4));
            }
            SetEofErrorType(MsgFormat.uint32, cur);
            return 0;
        }
        
        private uint read_uint32_max(int cur, int max) {
            var value = read_uint32(cur);
            if (value <= max) {
                return value;
            }
            SetIntRangeError(MsgFormat.uint32, value, cur);
            return 0;
        }
        
        
        // --- int64
        private long read_int64(int cur) {
            pos = cur + 9;
            if (pos <= data.Length) {
                return BinaryPrimitives.ReadInt64BigEndian(data.Slice(cur + 1, 8));
            }
            SetEofErrorType(MsgFormat.int64, cur);
            return 0;
        }
        
        private long read_int64_range(int cur, long min, long max) {
            var value = read_int64(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetIntRangeError(MsgFormat.int64, value, cur);
            return 0;
        }
        
        
        // --- uint64
        private ulong read_uint64(int cur) {
            pos = cur + 9;
            if (pos <= data.Length) {
                return BinaryPrimitives.ReadUInt64BigEndian(data.Slice(cur + 1, 8));
            }
            SetEofErrorType(MsgFormat.uint64, cur);
            return 0;
        }
        
        private ulong read_uint64_max(int cur, ulong max) {
            var value = read_uint64(cur);
            if (value <= max) {
                return value;
            }
            SetIntRangeError(MsgFormat.uint64, (long)value, cur);
            return 0;
        }
        
        
        // --- float32
        private float read_float32(int cur) {
            pos = cur + 5;
            if (pos <= data.Length) {
#if NETSTANDARD2_0
                throw new NotSupportedException();
#else
                var bits = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
                return BitConverter.Int32BitsToSingle(bits);    // missing in netstandard2.0
#endif
            }
            SetEofErrorType(MsgFormat.float32, cur);
            return 0;
        }
        
        private float read_float32_range(int cur, float min, float max) {
            var value = read_float32(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetFloatRangeError(MsgFormat.float32, value, cur);
            return 0;
        }
        
        
        // --- float64
        private double read_float64(int cur) {
            pos = cur + 9;
            if (pos <= data.Length) {
                var bits = BinaryPrimitives.ReadInt64BigEndian(data.Slice(cur + 1, 8));
                return BitConverter.Int64BitsToDouble(bits);
            }
            SetEofErrorType(MsgFormat.float64, cur);
            return 0;
        }
        
        private double read_float64_range(int cur, double min, double max) {
            var value = read_float64(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetFloatRangeError(MsgFormat.float64, value, cur);
            return 0;
        }
        
        // --- str
        private  bool read_str (out ReadOnlySpan<byte> target, int cur, int len, MsgFormat type) {
            pos     = cur + len;
            if (pos > data.Length) {
                SetEofErrorType(type, cur);
                target = default;
                return false;
            }
            target = data.Slice(cur, len);
            return true;
        }
    }
}