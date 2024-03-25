// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using static Friflo.Json.Fliox.MsgPack.MsgReaderState;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public ref partial struct MsgReader
    {
        public byte ReadByte ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return 0;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case <= fixintPosMax:   return          read_fixInt         (cur, type);
            //  case >= fixintNeg:    error
                //
                case    int8:           return (byte)   read_int8_pos       (ExpectUint8, cur);
                case    int16:          return (byte)   read_int16_range    (ExpectUint8, cur, 0, byte.MaxValue);
                case    int32:          return (byte)   read_int32_range    (ExpectUint8, cur, 0, byte.MaxValue);
                case    int64:          return (byte)   read_int64_range    (ExpectUint8, cur, 0, byte.MaxValue);
                //
                case    uint8:          return          read_uint8          (cur);
                case    uint16:         return (byte)   read_uint16_max     (ExpectUint8, cur,    byte.MaxValue);
                case    uint32:         return (byte)   read_uint32_max     (ExpectUint8, cur,    byte.MaxValue);
                case    uint64:         return (byte)   read_uint64_max     (ExpectUint8, cur,    byte.MaxValue);
                //
                case    float32:        return (byte)   read_float32_range  (ExpectUint8, cur, 0, byte.MaxValue);
                case    float64:        return (byte)   read_float64_range  (ExpectUint8, cur, 0, byte.MaxValue);
            }
            SetError(MsgReaderState.ExpectUint8, type, cur);
            return 0;
        }
        
        public short ReadInt16 ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return 0;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case <= fixintPosMax:   return          read_fixInt         (cur, type);
                case >= fixintNeg:      return          read_fixInt_neg     (cur, type);
                //
                case    int8:           return          read_int8           (cur);
                case    int16:          return          read_int16          (cur);
                case    int32:          return (short)  read_int32_range    (ExpectInt16, cur, short.MinValue, short.MaxValue);
                case    int64:          return (short)  read_int64_range    (ExpectInt16, cur, short.MinValue, short.MaxValue);
                //
                case    uint8:          return          read_uint8          (cur);
                case    uint16:         return (short)  read_uint16_max     (ExpectInt16, cur,                 short.MaxValue);
                case    uint32:         return (short)  read_uint32_max     (ExpectInt16, cur,                 short.MaxValue);
                case    uint64:         return (short)  read_uint64_max     (ExpectInt16, cur,           (long)short.MaxValue);
                //
                case    float32:        return (short)  read_float32_range  (ExpectInt16, cur, short.MinValue, short.MaxValue);
                case    float64:        return (short)  read_float64_range  (ExpectInt16, cur, short.MinValue, short.MaxValue);
            }
            SetError(ExpectInt16, type, cur);
            return 0;
        }
        
        public int ReadInt32 ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return 0;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case <= fixintPosMax:   return          read_fixInt         (cur, type);
                case >= fixintNeg:      return          read_fixInt_neg     (cur, type);
                //
                case    int8:           return          read_int8           (cur);
                case    int16:          return          read_int16          (cur);
                case    int32:          return          read_int32          (cur);
                case    int64:          return (int)    read_int64_range    (ExpectInt32, cur, int.MinValue, int.MaxValue);
                //
                case    uint8:          return          read_uint8          (cur);
                case    uint16:         return          read_uint16         (cur);
                case    uint32:         return (int)    read_uint32_max     (ExpectInt32, cur,               int.MaxValue);
                case    uint64:         return (int)    read_uint64_max     (ExpectInt32, cur,               int.MaxValue);
                //
                case    float32:        return (int)    read_float32_range  (ExpectInt32, cur, int.MinValue, int.MaxValue);
                case    float64:        return (int)    read_float64_range  (ExpectInt32, cur, int.MinValue, int.MaxValue);
            }
            SetError(ExpectInt32, type, cur);
            return 0;
        }
        
        public long ReadInt64 ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return 0;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case <= fixintPosMax:   return          read_fixInt         (cur, type);
                case >= fixintNeg:      return          read_fixInt_neg     (cur, type);
                //
                case    int8:           return          read_int8           (cur);
                case    int16:          return          read_int16          (cur);
                case    int32:          return          read_int32          (cur);
                case    int64:          return          read_int64          (cur);
                //
                case    uint8:          return          read_uint8          (cur);
                case    uint16:         return          read_uint16         (cur);
                case    uint32:         return          read_uint32         (cur);
                case    uint64:         return (long)   read_uint64_max     (ExpectInt64, cur,                long.MaxValue);
                //
                case    float32:        return (long)   read_float32_range  (ExpectInt64, cur, long.MinValue, long.MaxValue);
                case    float64:        return (long)   read_float64_range  (ExpectInt64, cur, long.MinValue, long.MaxValue);
            }
            SetError(ExpectInt64, type, cur);
            return 0;
        }
        
        public float ReadFloat32 ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return 0;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case <= fixintPosMax:   return          read_fixInt         (cur, type);
                case >= fixintNeg:      return          read_fixInt_neg     (cur, type);
                //
                case    int8:           return          read_int8           (cur);
                case    int16:          return          read_int16          (cur);
                case    int32:          return          read_int32          (cur);
                case    int64:          return          read_int64          (cur);
                //
                case    uint8:          return          read_uint8          (cur);
                case    uint16:         return          read_uint16         (cur);
                case    uint32:         return          read_uint32         (cur);
                case    uint64:         return          read_uint64         (cur);
                //
                case    float32:        return          read_float32        (cur);
                case    float64:        return (float)  read_float64_range  (ExpectFloat32, cur, float.MinValue, float.MaxValue);
            }
            SetError(ExpectFloat32, type, cur);
            return 0;
        }
        
        public double ReadFloat64 ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return 0;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case <= fixintPosMax:   return          read_fixInt         (cur, type);
                case >= fixintNeg:      return          read_fixInt_neg     (cur, type);
                //
                case    int8:           return          read_int8           (cur);
                case    int16:          return          read_int16          (cur);
                case    int32:          return          read_int32          (cur);
                case    int64:          return          read_int64          (cur);
                //
                case    uint8:          return          read_uint8          (cur);
                case    uint16:         return          read_uint16         (cur);
                case    uint32:         return          read_uint32         (cur);
                case    uint64:         return          read_uint64         (cur);
                //
                case    float32:        return          read_float32        (cur);
                case    float64:        return          read_float64        (cur);
            }
            SetError(ExpectFloat64, type, cur);
            return 0;
        }
    }
}