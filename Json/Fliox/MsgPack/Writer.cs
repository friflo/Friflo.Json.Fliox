// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

#pragma warning disable CS3001  // Argument type 'ulong' is not CLS-compliant

namespace Friflo.Json.Fliox.MsgPack
{

    public partial struct MsgWriter
    {
        internal        byte[]              target;
        private         int                 pos;
        private         bool                writeNil;
        
        public          int                 Length => pos;
        
        public          ReadOnlySpan<byte>  Data        => new ReadOnlySpan<byte>(target, 0, pos);
        /// <summary> Convert hex to JSON with [Online msgpack converter]<br/>https://msgpack.solder.party/  </summary>
        public          string              DataHex     => MsgPackUtils.GetDataHex(target, pos);
        public          string              DataDec     => MsgPackUtils.GetDataDec(target, pos);
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
            var newTarget = new byte[len];
            for (int n = 0; n < pos; n++) {
                newTarget[n] = target[n];
            }
            return target = newTarget;
        }
       
        public void WriteNull() {
            var data    = Reserve(1);
            data[pos++] = (byte)MsgFormat.nil;
        }
        
        // --- byte
        public void WriteByte(byte val) {
            var data    = Reserve(2);               // val: 2
            Write_byte(data, pos, val);
        }
        
        public void WriteKeyByte(int keyLen, ulong key, byte val) {
            var cur     = pos;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(1 + 8 + 2);       // key: 1 + 8,  val: 2
            WriteKeyFix(data, cur, keyLen, key);
            Write_byte(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyByte(ReadOnlySpan<byte> key, byte val) {
            var cur     = pos;
            var keyLen  = key.Length;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(2 + keyLen + 2);  // key: 2 + keyLen,  val: 2
            WriteKeySpan(data, cur, key);
            Write_byte(data, cur + keyLen + 1, val);
        }
        
        // --- long
        public void WriteInt64(long val) {
            var data    = Reserve(9);               // val: 9
            Write_long(data, pos, val);
        }
        
        public void WriteKeyInt64(int keyLen, ulong key, long val) {
            var cur     = pos;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(1 + 8 + 9);       // key: 1 + 8,  val: 9
            WriteKeyFix(data, cur, keyLen, key);
            Write_long(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyInt64(ReadOnlySpan<byte> key, int val) {
            var cur     = pos;
            var keyLen  = key.Length;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(2 + keyLen + 9);  // key: 2 + keyLen,  val: 9
            WriteKeySpan(data, cur, key);
            Write_long(data, cur + keyLen + 1, val);
        }
    }
}