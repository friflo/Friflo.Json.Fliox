// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
                case <= MsgFormat.fixintPosMax: return          read_fixInt         (cur, type);
            //  case >= MsgFormat.fixintNeg:    error
                //
                case    MsgFormat.int8:         return (byte)   read_int8_pos       (cur);
                case    MsgFormat.int16:        return (byte)   read_int16_range    (cur, 0, byte.MaxValue);
                case    MsgFormat.int32:        return (byte)   read_int32_range    (cur, 0, byte.MaxValue);
                case    MsgFormat.int64:        return (byte)   read_int64_range    (cur, 0, byte.MaxValue);
                //
                case    MsgFormat.uint8:        return          read_uint8          (cur);
                case    MsgFormat.uint16:       return (byte)   read_uint16_max     (cur,    byte.MaxValue);
                case    MsgFormat.uint32:       return (byte)   read_uint32_max     (cur,    byte.MaxValue);
                case    MsgFormat.uint64:       return (byte)   read_uint64_max     (cur,    byte.MaxValue);
                //
                case    MsgFormat.float32:      return (byte)   read_float32_range  (cur, 0, byte.MaxValue);
                case    MsgFormat.float64:      return (byte)   read_float64_range  (cur, 0, byte.MaxValue);
            }
            SetTypeError("expect uint8 compatible type", type, cur);
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
                case <= MsgFormat.fixintPosMax: return          read_fixInt         (cur, type);
                case >= MsgFormat.fixintNeg:    return          read_fixInt_neg     (cur, type);
                //
                case    MsgFormat.int8:         return          read_int8           (cur);
                case    MsgFormat.int16:        return          read_int16          (cur);
                case    MsgFormat.int32:        return (short)  read_int32_range    (cur, short.MinValue, short.MaxValue);
                case    MsgFormat.int64:        return (short)  read_int64_range    (cur, short.MinValue, short.MaxValue);
                //
                case    MsgFormat.uint8:        return          read_uint8          (cur);
                case    MsgFormat.uint16:       return (short)  read_uint16_max     (cur,                 short.MaxValue);
                case    MsgFormat.uint32:       return (short)  read_uint32_max     (cur,                 short.MaxValue);
                case    MsgFormat.uint64:       return (short)  read_uint64_max     (cur,           (long)short.MaxValue);
                //
                case    MsgFormat.float32:      return (short)  read_float32_range  (cur, short.MinValue, short.MaxValue);
                case    MsgFormat.float64:      return (short)  read_float64_range  (cur, short.MinValue, short.MaxValue);
            }
            SetTypeError("expect int16 compatible type", type, cur);
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
                case <= MsgFormat.fixintPosMax: return          read_fixInt         (cur, type);
                case >= MsgFormat.fixintNeg:    return          read_fixInt_neg     (cur, type);
                //
                case    MsgFormat.int8:         return          read_int8           (cur);
                case    MsgFormat.int16:        return          read_int16          (cur);
                case    MsgFormat.int32:        return          read_int32          (cur);
                case    MsgFormat.int64:        return (int)    read_int64_range    (cur, int.MinValue, int.MaxValue);
                //
                case    MsgFormat.uint8:        return          read_uint8          (cur);
                case    MsgFormat.uint16:       return          read_uint16         (cur);
                case    MsgFormat.uint32:       return (int)    read_uint32_max     (cur,               int.MaxValue);
                case    MsgFormat.uint64:       return (int)    read_uint64_max     (cur,               int.MaxValue);
                //
                case    MsgFormat.float32:      return (int)    read_float32_range  (cur, int.MinValue, int.MaxValue);
                case    MsgFormat.float64:      return (int)    read_float64_range  (cur, int.MinValue, int.MaxValue);
            }
            SetTypeError("expect int32 compatible type", type, cur);
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
                case <= MsgFormat.fixintPosMax: return          read_fixInt         (cur, type);
                case >= MsgFormat.fixintNeg:    return          read_fixInt_neg     (cur, type);
                //
                case    MsgFormat.int8:         return          read_int8           (cur);
                case    MsgFormat.int16:        return          read_int16          (cur);
                case    MsgFormat.int32:        return          read_int32          (cur);
                case    MsgFormat.int64:        return          read_int64          (cur);
                //
                case    MsgFormat.uint8:        return          read_uint8          (cur);
                case    MsgFormat.uint16:       return          read_uint16         (cur);
                case    MsgFormat.uint32:       return          read_uint32         (cur);
                case    MsgFormat.uint64:       return (long)   read_uint64_max     (cur,                long.MaxValue);
                //
                case    MsgFormat.float32:      return (long)   read_float32_range  (cur, long.MinValue, long.MaxValue);
                case    MsgFormat.float64:      return (long)   read_float64_range  (cur, long.MinValue, long.MaxValue);
            }
            SetTypeError("expect int64 compatible type", type, cur);
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
                case <= MsgFormat.fixintPosMax: return          read_fixInt         (cur, type);
                case >= MsgFormat.fixintNeg:    return          read_fixInt_neg     (cur, type);
                //
                case    MsgFormat.int8:         return          read_int8           (cur);
                case    MsgFormat.int16:        return          read_int16          (cur);
                case    MsgFormat.int32:        return          read_int32          (cur);
                case    MsgFormat.int64:        return          read_int64          (cur);
                //
                case    MsgFormat.uint8:        return          read_uint8          (cur);
                case    MsgFormat.uint16:       return          read_uint16         (cur);
                case    MsgFormat.uint32:       return          read_uint32         (cur);
                case    MsgFormat.uint64:       return          read_uint64         (cur);
                //
                case    MsgFormat.float32:      return          read_float32        (cur);
                case    MsgFormat.float64:      return (float)  read_float64_range  (cur, float.MinValue, float.MaxValue);
            }
            SetTypeError("expect float32 compatible type", type, cur);
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
                case <= MsgFormat.fixintPosMax: return          read_fixInt         (cur, type);
                case >= MsgFormat.fixintNeg:    return          read_fixInt_neg     (cur, type);
                //
                case    MsgFormat.int8:         return          read_int8           (cur);
                case    MsgFormat.int16:        return          read_int16          (cur);
                case    MsgFormat.int32:        return          read_int32          (cur);
                case    MsgFormat.int64:        return          read_int64          (cur);
                //
                case    MsgFormat.uint8:        return          read_uint8          (cur);
                case    MsgFormat.uint16:       return          read_uint16         (cur);
                case    MsgFormat.uint32:       return          read_uint32         (cur);
                case    MsgFormat.uint64:       return          read_uint64         (cur);
                //
                case    MsgFormat.float32:      return          read_float32        (cur);
                case    MsgFormat.float64:      return          read_float64        (cur);
            }
            SetTypeError("expect float64 compatible type", type, cur);
            return 0;
        }
    }
}