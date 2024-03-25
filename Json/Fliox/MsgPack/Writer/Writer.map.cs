// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;

// #pragma warning disable CS3001 // CS3001 : Argument type 'ulong' is not CLS-compliant

// ReSharper disable CommentTypo
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public partial struct MsgWriter
    {
        // --- fixmap
        public int WriteMapFixBegin() {
            return pos++;
        }
        
        public void WriteMapFixEnd(int arrayPos, int count) {
            target[arrayPos] = (byte)((int)MsgFormat.fixmap | count);
        }
        
        public void WriteMapDynEnd(int arrayPos, int count)
        {
            switch (count)
            {
                case >= 0 and <= 15:
                    target[arrayPos] = (byte)((int)MsgFormat.fixmap | count);
                    return;
                case >= 0 and <= ushort.MaxValue:
                    target[arrayPos] = (byte)MsgFormat.map16;
                    SetLength16(arrayPos + 1, count);
                    return;
                case >= 0 and <= int.MaxValue:
                    target[arrayPos] = (byte)MsgFormat.map32;
                    SetLength32(arrayPos + 1, count);
                    return;
                default:
                    throw new InvalidOperationException("unexpected count");
            }
        }
        
        // --- map32
        public int WriteMap32Begin() {
            var cur = pos;
            pos = cur + 5;
            return cur;
        }
        
        public void WriteMap32End(int arrayPos, int count) {
            target[arrayPos] = (byte)MsgFormat.map32;
            BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(target, arrayPos + 1, 4), count);
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
        
        public void WriteKey(int keyLen, long key, ref int count) {
            count++;
            var cur     = pos;
            var data    = Reserve(1 + 8);
            WriteKeyFix(data, cur, keyLen, key);
            pos         = cur + 1 + keyLen;
        }
        
        public void WriteKey(ReadOnlySpan<byte> key, ref int count) {
            count++;
            var data    = Reserve(4 + key.Length);
            var cur     = pos;
            WriteKeySpan(data, ref cur, key);
            pos         = cur;
        }
        
        private static void WriteKeyFix(byte[]data, int cur, int keyLen, long key) {
            data[cur] = (byte)((int)MsgFormat.fixstr | keyLen);
            BinaryPrimitives.WriteInt64LittleEndian(new Span<byte>(data, cur + 1, 8), key);
        }
        
        private static void WriteKeySpan(byte[]data, ref int cur, ReadOnlySpan<byte> key) {
            var keyLen  = key.Length;
            if (keyLen <= 15) {
                data[cur] = (byte)((int)MsgFormat.fixstr | keyLen);
                var target = new Span<byte>(data, cur + 1, keyLen);
                key.CopyTo(target);
                cur += 1 + keyLen;
                return;
            }
            if (keyLen <= 255) {
                data[cur + 0] = (byte)MsgFormat.str8;
                data[cur + 1] = (byte)keyLen;
                var target = new Span<byte>(data, cur + 2, keyLen);
                key.CopyTo(target);
                cur += 2 + keyLen;
                return;
            }
            throw new NotSupportedException($"expect keyLen <= 255. was {keyLen}");
        }
    }
}