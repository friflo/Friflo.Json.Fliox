// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;

// ReSharper disable once CheckNamespace
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
        
        private sbyte read_int8_pos(MsgReaderState expect, int cur) {
            var value = read_int8(cur);
            if (value >= 0) {
                return value;
            }
            SetRangeError(expect, MsgFormat.int8, cur);
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
                return ReadInt16BigEndian(data.Slice(cur + 1, 2));
            }
            SetEofErrorType(MsgFormat.int16, cur);
            return 0;
        }
        
        private short read_int16_range(MsgReaderState expect, int cur, int min, int max) {
            var value = read_int16(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetRangeError(expect, MsgFormat.int16, cur);
            return 0;
        }
        
        
        // --- uint16
        private ushort read_uint16(int cur) {
            pos = cur + 3;
            if (pos <= data.Length) {
                return ReadUInt16BigEndian(data.Slice(cur + 1, 2));
            }
            SetEofErrorType(MsgFormat.uint16, cur);
            return 0;
        }
        
        private ushort read_uint16_max(MsgReaderState expect, int cur, int max) {
            var value = read_uint16(cur);
            if (value <= max) {
                return value;
            }
            SetRangeError(expect, MsgFormat.uint16, cur);
            return 0;
        }
        
        
        // --- int32
        private int read_int32(int cur) {
            pos = cur + 5;
            if (pos <= data.Length) {
                return ReadInt32BigEndian(data.Slice(cur + 1, 4));
            }
            SetEofErrorType(MsgFormat.int32, cur);
            return 0;
        }
       
        private int read_int32_range(MsgReaderState expect, int cur, int min, int max) {
            var value = read_int32(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetRangeError(expect, MsgFormat.int32, cur);
            return 0;
        }
        
        
        // --- uint32
        private uint read_uint32(int cur) {
            pos = cur + 5;
            if (pos <= data.Length) {
                return ReadUInt32BigEndian(data.Slice(cur + 1, 4));
            }
            SetEofErrorType(MsgFormat.uint32, cur);
            return 0;
        }
        
        private uint read_uint32_max(MsgReaderState expect, int cur, int max) {
            var value = read_uint32(cur);
            if (value <= max) {
                return value;
            }
            SetRangeError(expect, MsgFormat.uint32, cur);
            return 0;
        }
        
        
        // --- int64
        private long read_int64(int cur) {
            pos = cur + 9;
            if (pos <= data.Length) {
                return ReadInt64BigEndian(data.Slice(cur + 1, 8));
            }
            SetEofErrorType(MsgFormat.int64, cur);
            return 0;
        }
        
        private long read_int64_range(MsgReaderState expect, int cur, long min, long max) {
            var value = read_int64(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetRangeError(expect, MsgFormat.int64, cur);
            return 0;
        }
        
        
        // --- uint64
        private ulong read_uint64(int cur) {
            pos = cur + 9;
            if (pos <= data.Length) {
                return ReadUInt64BigEndian(data.Slice(cur + 1, 8));
            }
            SetEofErrorType(MsgFormat.uint64, cur);
            return 0;
        }
        
        private ulong read_uint64_max(MsgReaderState expect, int cur, ulong max) {
            var value = read_uint64(cur);
            if (value <= max) {
                return value;
            }
            SetRangeError(expect, MsgFormat.uint64, cur);
            return 0;
        }
        
        
        // --- float32
        private float read_float32(int cur) {
            pos = cur + 5;
            if (pos <= data.Length) {
#if NETSTANDARD2_0
                throw new NotSupportedException();
#else
                var bits = ReadInt32BigEndian(data.Slice(cur + 1, 4));
                return BitConverter.Int32BitsToSingle(bits);    // missing in netstandard2.0
#endif
            }
            SetEofErrorType(MsgFormat.float32, cur);
            return 0;
        }
        
        private float read_float32_range(MsgReaderState expect, int cur, float min, float max) {
            var value = read_float32(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetRangeError(expect, MsgFormat.float32, cur);
            return 0;
        }
        
        
        // --- float64
        private double read_float64(int cur) {
            pos = cur + 9;
            if (pos <= data.Length) {
                var bits = ReadInt64BigEndian(data.Slice(cur + 1, 8));
                return BitConverter.Int64BitsToDouble(bits);
            }
            SetEofErrorType(MsgFormat.float64, cur);
            return 0;
        }
        
        private double read_float64_range(MsgReaderState expect, int cur, double min, double max) {
            var value = read_float64(cur);
            if (min <= value && value <= max) {
                return value;
            }
            SetRangeError(expect, MsgFormat.float64, cur);
            return 0;
        }
        
        // --- str
        private  ReadOnlySpan<byte> read_str (int cur, int len, MsgFormat type) {
            pos     = cur + len;
            if (pos > data.Length) {
                SetEofErrorType(type, cur);
                return default;
            }
            return data.Slice(cur, len);
        }
        
        // --- bin
        private   ReadOnlySpan<byte> read_bin(int cur, int len, MsgFormat type) {
            pos     = cur + len;
            if (pos > data.Length) {
                SetEofErrorType(type, cur);
                return default;
            }
            return data.Slice(cur, len);
        }
        

    }
}