// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;

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
        
        public void WriteMapInt32(int keyLen , ulong key, long val) {
            var cur     = pos;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(1 + 8 + 8);       // key: 1 + 8,  val: 8
            WriteKey(data, cur, keyLen, key);
            WriteLong(data, cur + keyLen + 1, val);
        }
        
        public void WriteMapInt32(ReadOnlySpan<byte> key, int val) {
            var cur     = pos;
            var keyLen  = key.Length;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(2 + keyLen + 8);  // key: 2 + keyLen,  val: 8
            WriteKey(data, cur, key);
            WriteLong(data, cur + keyLen + 1, val);
        }
        
        public void WriteMapByte(int keyLen , ulong key, byte val) {
            var cur     = pos;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(1 + 8 + 2);       // key: 1 + 8,  val: 2
            WriteKey(data, cur, keyLen, key);
            WriteByte(data, cur, val);
        }
        
        private void WriteByte(byte[]data, int cur, byte val)
        {
            switch (val)
            {
                case >= (int)sbyte.MaxValue:
                    data[cur]   = (byte)MsgFormat.int8;
                    data[cur]   = (byte)val;
                    pos = cur + 1;
                    return;
                default:
                    data[cur]   = val;
                    pos = cur + 1;
                    return;
            }
        }
        
        private void WriteLong(byte[]data, int cur, long val)
        {
            switch (val)
            {
                case > int.MaxValue:
                    data[cur]       = (byte)MsgFormat.int64;
                    BinaryPrimitives.WriteInt64BigEndian (new Span<byte>(data, cur + 1, 8), val);
                    pos = cur + 9;
                    return;
                case > short.MaxValue:
                    data[cur]       = (byte)MsgFormat.int32;
                    BinaryPrimitives.WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), (int)val);
                    pos = cur + 5;
                    return;
                case > byte.MaxValue:
                    data[cur]       = (byte)MsgFormat.int16;
                    BinaryPrimitives.WriteInt16BigEndian (new Span<byte>(data, cur + 1, 2), (short)val);
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
                    BinaryPrimitives.WriteInt16BigEndian (new Span<byte>(data, cur + 1, 2), (short)val);
                    pos = cur + 3;
                    return;
                case >= int.MinValue:
                    data[cur]       = (byte)MsgFormat.int32;
                    BinaryPrimitives.WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), (int)val);
                    pos = cur + 5;
                    return;
                case >= long.MinValue:
                    data[cur]       = (byte)MsgFormat.int64;
                    BinaryPrimitives.WriteInt64BigEndian (new Span<byte>(data, cur + 1, 8), val);
                    pos = cur + 9;
                    return;
            }
        }
    }
}