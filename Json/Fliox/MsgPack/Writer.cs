// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;

namespace Friflo.Json.Fliox.MsgPack
{

    public partial struct MsgWriter
    {
        internal    byte[]  target;
        private     int     pos;
        
        public ReadOnlySpan<byte>  Data => new ReadOnlySpan<byte>(target, 0, pos);
            
        public MsgWriter(byte[] target) {
            this.target = target;
            pos         = 0;
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
        
        /*
        public void WriteUint8(int id, byte val) {
            if (id >= 0x80) throw new InvalidOperationException();
            var data        = Reserve(2);
            data[cur + 0]   = (byte)id;
            data[cur + 1]   = BinFormat.Uint8;
            data[cur + 2]   = val;
        }
        
        public void WriteInt16(int id, short val) {
            if (id >= 0x80) throw new InvalidOperationException();
            var cur = pos;
            var data        = Reserve(4);
            data[cur + 0]   = (byte)id;
            data[cur + 1]   = BinFormat.Int16;
            BinaryPrimitives.WriteInt16BigEndian(new Span<byte>(data, cur + 2, 2), val);
        } */
        
        /* public void WriteIdInt32(int id, int val) {
            var data        = Reserve(1 + 5, out int cur);
            data[cur + 0]   = (byte)id;
            WriteInt32(data, cur + 1, val);
        } */
        
        public void WriteInt32(int keyLen , ulong key, int val) {
            var cur     = pos;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(1 + 8 + 5);
            WriteKey(data, cur, keyLen, key);
            WriteInt32(data, cur + keyLen + 1, val);
        }
        
        public void WriteInt32(ReadOnlySpan<byte> key, int val) {
            var cur     = pos;
            var keyLen  = key.Length;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(2 + keyLen + 5);
            WriteKey(data, cur, key);
            WriteInt32(data, cur + keyLen + 1, val);
        }
        
        private void WriteInt32(byte[]data, int cur, int val) {
            if (val < short.MinValue || val > short.MaxValue) {
                data[cur]   = (byte)MsgFormat.int32;
                BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(data, cur + 1, 4), val);
                pos = cur + 5;
                return;
            }
            if (val < sbyte.MinValue || val > byte.MaxValue) {
                data[cur]   = (byte)MsgFormat.int16;
                BinaryPrimitives.WriteInt16BigEndian(new Span<byte>(data, cur + 1, 2), (short)val);
                pos = cur + 3;
                return;
            }
            if (val >= 0) {
                data[cur]   = (byte)MsgFormat.int16;
                BinaryPrimitives.WriteInt16BigEndian(new Span<byte>(data, cur + 1, 4), (short)val);
                pos = cur + 2;
            }
        }
    }
}