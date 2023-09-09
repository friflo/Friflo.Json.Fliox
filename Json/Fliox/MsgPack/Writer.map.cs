// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;

#pragma warning disable CS3001 // CS3001 : Argument type 'ulong' is not CLS-compliant

// ReSharper disable CommentTypo
namespace Friflo.Json.Fliox.MsgPack
{
    public partial struct MsgWriter
    {
        // --- fixmap
        public int WriteMapFix() {
            return pos++;
        }
        
        public void WriteMapFixCount(int pos, int count) {
            target[pos] = (byte)((int)MsgFormat.fixmap | count);
        }
        
        // --- map16
        public int WriteMap16() {
            var cur = pos;
            pos += 3;
            return cur;
        }
        
        public void WriteMap16Count(int pos, int count) {
            target[pos]     = (byte)MsgFormat.map16;
            target[pos + 1] = (byte)(count << 8);
            target[pos + 2] = (byte) count;
        }
        
        // --- write key
        public bool AddKey(bool exists) {
            return writeNil || exists;
        }
        
        public void WriteKey(int keyLen, ulong key, ref int count) {
            count++;
            var cur     = pos;
            pos         = cur + 1 + keyLen;
            var data    = Reserve(1 + 8);
            WriteKeyFix(data, cur, keyLen, key);
        }
        
        private static void WriteKeyFix(byte[]data, int cur, int keyLen, ulong key) {
            data[cur] = (byte)((int)MsgFormat.fixstr | keyLen);
            BinaryPrimitives.WriteUInt64LittleEndian(new Span<byte>(data, cur + 1, 8), key);
        }
        
        private static void WriteKeySpan(byte[]data, int cur, ReadOnlySpan<byte> key) {
            var keyLen  = key.Length;
            if (keyLen <= 15) {
                data[cur] = (byte)((int)MsgFormat.fixstr | keyLen);
                var target = new Span<byte>(data, cur + 1, keyLen);
                key.CopyTo(target);
                return;
            }
            if (keyLen <= 255) {
                data[cur + 0] = (byte)MsgFormat.str8;
                data[cur + 1] = (byte)keyLen;
                var target = new Span<byte>(data, cur + 2, keyLen);
                key.CopyTo(target);
            }
        }
    }
}