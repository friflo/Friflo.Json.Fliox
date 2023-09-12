// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// #pragma warning disable CS3001  // Argument type 'ulong' is not CLS-compliant

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
            int len = pos + length;
            if (len <= target.Length) {
                return target;
            }
            var newTarget = new byte[2 * len];
            var targetLen = pos;
            for (int n = 0; n < targetLen; n++) {
                newTarget[n] = target[n];
            }
            return target = newTarget;
        }
       
        // --- nil
        public void WriteNull() {
            var data    = Reserve(1);
            data[pos++] = (byte)MsgFormat.nil;
        }
        
        private void WriteKeyNil(int keyLen, long key) {
            if (!writeNil) {
                return;
            }
            var cur     = pos;
            var data    = Reserve(1 + 8 + 1);       // key: 1 + 8,  val: 1
            WriteKeyFix(data, cur, keyLen, key);
            pos         = cur + 1 + keyLen + 1;
            data[cur + 1 + keyLen] = (byte)MsgFormat.nil;
        }
        
        private void WriteKeyNil(ReadOnlySpan<byte> key) {
            if (!writeNil) {
                return;
            }
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
            Write_string_pos(val);
        }
        
        public void WriteKeyString(int keyLen, long key, string val) {
            if (val == null) {
                WriteKeyNil(keyLen, key);
                return;
            }
            var cur     = pos;
            var data    = Reserve(1 + 8);           // key: 1 + 8
            WriteKeyFix(data, cur, keyLen, key);
            pos         = cur + 1 + keyLen;
            Write_string_pos(val);
        }

        
        public void WriteKeyString(ReadOnlySpan<byte> key, string val) {
            if (val == null) {
                WriteKeyNil(key);
                return;
            }
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
        
        public void WriteKeyBool(int keyLen, long key, bool? val) {
            if (val.HasValue) {
                WriteKeyBool(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key);
        }
        
        public void WriteKeyBool(ReadOnlySpan<byte> key, bool val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 1);  // key: 2 + keyLen,  val: 1
            WriteKeySpan(data, ref cur, key);
            Write_bool_pos(data, cur, val);
        }
        
        public void WriteKeyBool(ReadOnlySpan<byte> key, bool? val) {
            if (val.HasValue) {
                WriteKeyBool(key, val.Value);
                return;
            }
            WriteKeyNil(key);
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
        
        public void WriteKeyByte(int keyLen, long key, byte? val) {
            if (val.HasValue) {
                WriteKeyByte(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key);
        }
        
        public void WriteKeyByte(ReadOnlySpan<byte> key, byte val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 2);  // key: 2 + keyLen,  val: 2
            WriteKeySpan(data, ref cur, key);
            Write_byte_pos(data, cur, val);
        }
        
        public void WriteKeyByte(ReadOnlySpan<byte> key, byte? val) {
            if (val.HasValue) {
                WriteKeyByte(key, val.Value);
                return;
            }
            WriteKeyNil(key);
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
        
        public void WriteKeyInt16(int keyLen, long key, short? val) {
            if (val.HasValue) {
                WriteKeyInt16(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key);
        }
        
        public void WriteKeyInt16(ReadOnlySpan<byte> key, short val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 3);  // key: 2 + keyLen,  val: 3
            WriteKeySpan(data, ref cur, key);
            Write_short_pos(data, cur, val);
        }
        
        public void WriteKeyInt16(ReadOnlySpan<byte> key, short? val) {
            if (val.HasValue) {
                WriteKeyInt16(key, val.Value);
                return;
            }
            WriteKeyNil(key);
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
        
        public void WriteKeyInt32(int keyLen, long key, int? val) {
            if (val.HasValue) {
                WriteKeyInt32(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key);
        }
        
        public void WriteKeyInt32(ReadOnlySpan<byte> key, int val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 5);  // key: 2 + keyLen,  val: 5
            WriteKeySpan(data, ref cur, key);
            Write_int_pos(data, cur, val);
        }
        
        public void WriteKeyInt32(ReadOnlySpan<byte> key, int? val) {
            if (val.HasValue) {
                WriteKeyInt32(key, val.Value);
                return;
            }
            WriteKeyNil(key);
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
        
        public void WriteKeyInt64(int keyLen, long key, long? val) {
            if (val.HasValue) {
                WriteKeyInt64(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key);
        }
        
        public void WriteKeyInt64(ReadOnlySpan<byte> key, int val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 9);  // key: 2 + keyLen,  val: 9
            WriteKeySpan(data, ref cur, key);
            Write_long_pos(data, cur, val);
        }
        
        public void WriteKeyInt64(ReadOnlySpan<byte> key, int? val) {
            if (val.HasValue) {
                WriteKeyInt64(key, val.Value);
                return;
            }
            WriteKeyNil(key);
        }
    }
}