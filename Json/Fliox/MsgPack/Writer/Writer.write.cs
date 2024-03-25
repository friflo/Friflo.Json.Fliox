// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

// #pragma warning disable CS3001  // Argument type 'ulong' is not CLS-compliant

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public partial struct MsgWriter
    {
        private void Write_string_pos(ReadOnlySpan<char> val)
        {
            var len     = Encoding.UTF8.GetByteCount(val);
            var cur     = Write_string_len(len);
            Encoding.UTF8.GetBytes(val, new Span<byte>(target, cur, len));
            pos = cur + len;
        }
        
        private void Write_string_pos(ReadOnlySpan<byte> val)
        {
            var len     = val.Length;
            var cur     = Write_string_len(len);
            val.CopyTo(new Span<byte>(target, cur, len));
            pos = cur + len;
        }
        
        private int Write_string_len(int len)
        {
            var data    = Reserve(1 + 4 + len);
            int cur     = pos;
            switch (len) {
                case <= 31:
                    data[cur]       = (byte)((int)MsgFormat.fixstr | len);
                    return cur + 1;
                case <= byte.MaxValue:
                    data[cur]       = (byte)MsgFormat.str8;
                    data[cur + 1]   = (byte)len;
                    return cur + 2;
                case <= short.MaxValue:
                    data[cur]       = (byte)MsgFormat.str16;
                    WriteUInt16BigEndian (new Span<byte>(data, cur + 1, 2), (ushort)len);
                    return cur + 3;
                case <=  int.MaxValue:
                    data[cur]       = (byte)MsgFormat.str32;
                    WriteUInt32BigEndian (new Span<byte>(data, cur + 1, 4), (uint)len);
                    return cur + 5;
            }
            throw new InvalidOperationException("unexpected string len");
        }
        
        // --- bool
        private void Write_bool_pos(byte[]data, int cur, bool val)
        {
            pos = cur + 1;
            data[cur] = (byte)(val ? MsgFormat.True : MsgFormat.False);
        }
        
        
        // ----------------------------------- byte, short, int long -----------------------------------
        private void Write_byte_pos(byte[]data, int cur, byte val)
        {
            switch (val)
            {
                case > (int)sbyte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint8;
                    data[cur + 1]   = val;
                    pos = cur + 2;
                    return;
                default:
                    data[cur]   = val;
                    pos = cur + 1;
                    return;
            }
        }
        
        private void Write_short_pos(byte[]data, int cur, short val)
        {
            switch (val)
            {
                case > byte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint16;
                    WriteUInt16BigEndian (new Span<byte>(data, cur + 1, 2), (ushort)val);
                    pos = cur + 3;
                    return;
                case > sbyte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= 0:
                    data[cur]   = (byte)val;
                    pos = cur + 1;
                    return;
                // --------------------------------- val < 0  ---------------------------------
                case >= -32:
                    data[cur] = (byte)(0xe0 | val);
                    pos = cur + 1;
                    return;
                case >= sbyte.MinValue:
                    data[cur]       = (byte)MsgFormat.int8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= short.MinValue:
                    data[cur]       = (byte)MsgFormat.int16;
                    WriteInt16BigEndian (new Span<byte>(data, cur + 1, 2), (short)val);
                    pos = cur + 3;
                    return;
            }
        }
        
        private void Write_int_pos(byte[]data, int cur, int val)
        {
            switch (val)
            {
                case > ushort.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint32;
                    WriteUInt32BigEndian (new Span<byte>(data, cur + 1, 4), (uint)val);
                    pos = cur + 5;
                    return;
                case > byte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint16;
                    WriteUInt16BigEndian (new Span<byte>(data, cur + 1, 2), (ushort)val);
                    pos = cur + 3;
                    return;
                case > sbyte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= 0:
                    data[cur]   = (byte)val;
                    pos = cur + 1;
                    return;
                // --------------------------------- val < 0  ---------------------------------
                case >= -32:
                    data[cur] = (byte)(0xe0 | val);
                    pos = cur + 1;
                    return;
                case >= sbyte.MinValue:
                    data[cur]       = (byte)MsgFormat.int8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= short.MinValue:
                    data[cur]       = (byte)MsgFormat.int16;
                    WriteInt16BigEndian (new Span<byte>(data, cur + 1, 2), (short)val);
                    pos = cur + 3;
                    return;
                case >= int.MinValue:
                    data[cur]       = (byte)MsgFormat.int32;
                    WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), (int)val);
                    pos = cur + 5;
                    return;
            }
        }
        
        private void Write_long_pos(byte[]data, int cur, long val)
        {
            switch (val)
            {
                case > uint.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint64;
                    WriteUInt64BigEndian (new Span<byte>(data, cur + 1, 8), (ulong)val);
                    pos = cur + 9;
                    return;
                case > ushort.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint32;
                    WriteUInt32BigEndian (new Span<byte>(data, cur + 1, 4), (uint)val);
                    pos = cur + 5;
                    return;
                case > byte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint16;
                    WriteUInt16BigEndian (new Span<byte>(data, cur + 1, 2), (ushort)val);
                    pos = cur + 3;
                    return;
                case > sbyte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= 0:
                    data[cur]   = (byte)val;
                    pos = cur + 1;
                    return;
                // --------------------------------- val < 0  ---------------------------------
                case >= -32:
                    data[cur] = (byte)(0xe0 | val);
                    pos = cur + 1;
                    return;
                case >= sbyte.MinValue:
                    data[cur]       = (byte)MsgFormat.int8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= short.MinValue:
                    data[cur]       = (byte)MsgFormat.int16;
                    WriteInt16BigEndian (new Span<byte>(data, cur + 1, 2), (short)val);
                    pos = cur + 3;
                    return;
                case >= int.MinValue:
                    data[cur]       = (byte)MsgFormat.int32;
                    WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), (int)val);
                    pos = cur + 5;
                    return;
                case >= long.MinValue:
                    data[cur]       = (byte)MsgFormat.int64;
                    WriteInt64BigEndian (new Span<byte>(data, cur + 1, 8), val);
                    pos = cur + 9;
                    return;
            }
        }
        
        private void Write_bin(byte[] data, int cur, ReadOnlySpan<byte> bytes)
        {
            int len = bytes.Length;
            switch (len) 
            {
                case <= byte.MaxValue: {
                    data[cur]       = (byte)MsgFormat.bin8;
                    data[cur + 1]   = (byte)len;
                    bytes.CopyTo(new Span<byte>(data, cur + 2, len));
                    pos = cur + 2 + len;
                    return;
                }
                case <= ushort.MaxValue: {
                    data[cur]       = (byte)MsgFormat.bin16;
                    data[cur + 1]   = (byte)(len >> 8);
                    data[cur + 2]   = (byte)len;
                    bytes.CopyTo(new Span<byte>(data, cur + 3, len));
                    pos = cur + 3 + len;
                    return;
                }
                case <= int.MaxValue: {
                    data[cur]       = (byte)MsgFormat.bin32;
                    WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), len);
                    bytes.CopyTo(new Span<byte>(data, cur + 5, len));
                    pos = cur + 5 + len;
                    return;
                }
            }
        }
    }
}