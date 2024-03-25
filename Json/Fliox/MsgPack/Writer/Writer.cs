// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// #pragma warning disable CS3001  // Argument type 'ulong' is not CLS-compliant

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public partial struct MsgWriter
    {
        internal        byte[]              target;
        private         int                 pos;
        private         bool                writeNil;
        
        public          int                 Length => pos;
        
        public          ReadOnlySpan<byte>  Data        => new ReadOnlySpan<byte>(target, 0, pos);
        /// <summary> Convert hex to JSON with [msgpack-lite demo] https://kawanet.github.io/msgpack-lite/ </summary>
        public          string              DataHex     => MsgPackUtils.GetDataHex(Data);
        public          string              DataDec     => MsgPackUtils.GetDataDec(Data);
        public override string              ToString()  => $"pos: {pos}";

        public MsgWriter(byte[] target, bool writeNil) {
            this.target     = target;
            pos             = 0;
            this.writeNil   = writeNil;
        }
        
        public void Init() {
            pos = 0;
        }
        
        private byte[] Reserve(int length) {
            if (pos + length <= target.Length) {
                return target;
            }
            return Resize(length);
        }
        
        private byte[] Resize(int length) {
            int newLen      = 2 * (pos + length);
            var newTarget   = new byte[newLen];
            var copyLen     = pos;
            for (int n = 0; n < copyLen; n++) {
                newTarget[n] = target[n];
            }
            return target = newTarget;
        }
       
        // --- nil
        public void WriteNull() {
            var data    = Reserve(1);
            data[pos++] = (byte)MsgFormat.nil;
        }
        
        public void WriteKeyNil(int keyLen, long key, ref int count) {
            if (!writeNil) {
                return;
            }
            count++;
            var cur     = pos;
            var data    = Reserve(1 + 8 + 1);       // key: 1 + 8,  val: 1
            WriteKeyFix(data, cur, keyLen, key);
            pos         = cur + 1 + keyLen + 1;
            data[cur + 1 + keyLen] = (byte)MsgFormat.nil;
        }
        
        public void WriteKeyNil(ReadOnlySpan<byte> key, ref int count) {
            if (!writeNil) {
                return;
            }
            count++;
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 1);  // key: 2 + keyLen,  val: 1
            WriteKeySpan(data, ref cur, key);
            pos         = cur + 1;
            data[cur]   = (byte)MsgFormat.nil;
        }
        
        // --- string
        public void WriteString(string val) {
            if (val == null) {
                WriteNull();
                return;
            }
            Write_string_pos(val.AsSpan());
        }
        
        public void WriteKeyString(int keyLen, long key, string val, ref int count) {
            if (val == null) {
                WriteKeyNil(keyLen, key, ref count);
                return;
            }
            count++;
            var cur     = pos;
            var data    = Reserve(1 + 8);           // key: 1 + 8
            WriteKeyFix(data, cur, keyLen, key);
            pos         = cur + 1 + keyLen;
            Write_string_pos(val.AsSpan());
        }

        
        public void WriteKeyString(ReadOnlySpan<byte> key, string val, ref int count) {
            if (val == null) {
                WriteKeyNil(key, ref count);
                return;
            }
            count++;
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(1 + keyLen);      // key: 2 + keyLen
            WriteKeySpan(data, ref cur, key);
            pos         = cur;
            Write_string_pos(val.AsSpan());
        }
        
        // --- string - UTF-8 ReadOnlySpan<byte>
        public void WriteStringUtf8(ReadOnlySpan<byte> val) {
            if (val == null) {
                WriteNull();
                return;
            }
            Write_string_pos(val);
        }
        
        public void WriteKeyStringUtf8(int keyLen, long key, ReadOnlySpan<byte> val, ref int count) {
            if (val == null) {
                WriteKeyNil(keyLen, key, ref count);
                return;
            }
            count++;
            var cur     = pos;
            var data    = Reserve(1 + 8);           // key: 1 + 8
            WriteKeyFix(data, cur, keyLen, key);
            pos         = cur + 1 + keyLen;
            Write_string_pos(val);
        }
        
        public void WriteKeyStringUtf8(ReadOnlySpan<byte> key, ReadOnlySpan<byte> val, ref int count) {
            if (val == null) {
                WriteKeyNil(key, ref count);
                return;
            }
            count++;
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(1 + keyLen);      // key: 2 + keyLen
            WriteKeySpan(data, ref cur, key);
            pos         = cur;
            Write_string_pos(val);
        }
        
        
        // --- bool
        public void WriteBool(bool val) {
            var data    = Reserve(1);               // val: 2
            Write_bool_pos(data, pos, val);
        }
        
        public void WriteKeyBool(int keyLen, long key, bool val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 1);       // key: 1 + 8,  val: 1
            WriteKeyFix(data, cur, keyLen, key);
            Write_bool_pos(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyBool(int keyLen, long key, bool? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyBool(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key, ref count);
        }
        
        public void WriteKeyBool(ReadOnlySpan<byte> key, bool val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 1);  // key: 2 + keyLen,  val: 1
            WriteKeySpan(data, ref cur, key);
            Write_bool_pos(data, cur, val);
        }
        
        public void WriteKeyBool(ReadOnlySpan<byte> key, bool? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyBool(key, val.Value);
                return;
            }
            WriteKeyNil(key, ref count);
        }
        
        
        // --- byte
        public void WriteByte(byte val) {
            var data    = Reserve(2);               // val: 2
            Write_byte_pos(data, pos, val);
        }
        
        public void WriteKeyByte(int keyLen, long key, byte val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 2);       // key: 1 + 8,  val: 2
            WriteKeyFix(data, cur, keyLen, key);
            Write_byte_pos(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyByte(int keyLen, long key, byte? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyByte(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key, ref count);
        }
        
        public void WriteKeyByte(ReadOnlySpan<byte> key, byte val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 2);  // key: 2 + keyLen,  val: 2
            WriteKeySpan(data, ref cur, key);
            Write_byte_pos(data, cur, val);
        }
        
        public void WriteKeyByte(ReadOnlySpan<byte> key, byte? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyByte(key, val.Value);
                return;
            }
            WriteKeyNil(key, ref count);
        }
        
        
        // --- short
        public void WriteInt16(short val) {
            var data    = Reserve(3);               // val: 3
            Write_short_pos(data, pos, val);
        }
        
        public void WriteKeyInt16(int keyLen, long key, short val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 3);       // key: 1 + 8,  val: 3
            WriteKeyFix(data, cur, keyLen, key);
            Write_short_pos(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyInt16(int keyLen, long key, short? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyInt16(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key, ref count);
        }
        
        public void WriteKeyInt16(ReadOnlySpan<byte> key, short val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 3);  // key: 2 + keyLen,  val: 3
            WriteKeySpan(data, ref cur, key);
            Write_short_pos(data, cur, val);
        }
        
        public void WriteKeyInt16(ReadOnlySpan<byte> key, short? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyInt16(key, val.Value);
                return;
            }
            WriteKeyNil(key, ref count);
        }
        
        
        // --- int
        public void WriteInt32(int val) {
            var data    = Reserve(5);               // val: 5
            Write_int_pos(data, pos, val);
        }
        
        public void WriteKeyInt32(int keyLen, long key, int val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 5);       // key: 1 + 8,  val: 5
            WriteKeyFix(data, cur, keyLen, key);
            Write_int_pos(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyInt32(int keyLen, long key, int? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyInt32(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key, ref count);
        }
        
        public void WriteKeyInt32(ReadOnlySpan<byte> key, int val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 5);  // key: 2 + keyLen,  val: 5
            WriteKeySpan(data, ref cur, key);
            Write_int_pos(data, cur, val);
        }
        
        public void WriteKeyInt32(ReadOnlySpan<byte> key, int? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyInt32(key, val.Value);
                return;
            }
            WriteKeyNil(key, ref count);
        }
        
        
        // --- long
        public void WriteInt64(long val) {
            var data    = Reserve(9);               // val: 9
            Write_long_pos(data, pos, val);
        }
        
        public void WriteKeyInt64(int keyLen, long key, long val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 9);       // key: 1 + 8,  val: 9
            WriteKeyFix(data, cur, keyLen, key);
            Write_long_pos(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyInt64(int keyLen, long key, long? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyInt64(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key, ref count);
        }
        
        public void WriteKeyInt64(ReadOnlySpan<byte> key, long val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 9);  // key: 2 + keyLen,  val: 9
            WriteKeySpan(data, ref cur, key);
            Write_long_pos(data, cur, val);
        }
        
        public void WriteKeyInt64(ReadOnlySpan<byte> key, long? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyInt64(key, val.Value);
                return;
            }
            WriteKeyNil(key, ref count);
        }
        
        // --- bin
        public void WriteBin(ReadOnlySpan<byte> bytes) {
            if (bytes == null) {
                WriteNull();
                return;
            }
            var data    = Reserve(5 + bytes.Length);                // val: 5 + bytes.Length
            Write_bin(data, pos, bytes);
        }
        
        public void WriteKeyBin(int keyLen, long key, ReadOnlySpan<byte> bytes) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 5 + bytes.Length);        // key: 1 + 8,  val: 5 + bytes.Length
            WriteKeyFix(data, cur, keyLen, key);
            Write_bin(data, cur + 1 + keyLen, bytes);
        }
        
        
        public void WriteKeyBin(ReadOnlySpan<byte> key, ReadOnlySpan<byte> bytes) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 5 + bytes.Length);   // key: 2 + keyLen,  val: 5 + bytes.Length
            WriteKeySpan(data, ref cur, key);
            Write_bin(data, cur, bytes);
        }
    }
}