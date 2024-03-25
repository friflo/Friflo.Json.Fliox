// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;


// ReSharper disable CommentTypo
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public partial struct MsgWriter
    {
        public int WriteArray(int length) {
            switch (length) {
                case <= 15: {
                    var data        = Reserve(1);
                    data[pos++]     = (byte)((int)MsgFormat.fixarray | length);
                    return length;
                }
                case <= ushort.MaxValue: {
                    var data        = Reserve(3);
                    var cur         = pos;
                    pos             = cur + 3; 
                    data[cur]       = (byte)MsgFormat.array16;
                    data[cur + 1]   = (byte)(length >> 8);
                    data[cur + 2]   = (byte)length;
                    return length;
                }
               default: {
                    var data = Reserve(5);
                    var cur     = pos;
                    pos         = cur + 5; 
                    data[cur]   = (byte)MsgFormat.array32;
                    BinaryPrimitives.WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), length);
                    return length;
                }
            }
        }
        
        // --- array32
        public int WriteArrayFixStart() {
            var cur = pos;
            pos = cur + 1;
            return cur;
        }
        
        public void WriteArrayFixEnd(int mapPos, int count) {
            target[mapPos] = (byte)((int)MsgFormat.fixarray | count);
        }
        
        public void WriteArrayDynEnd(int mapPos, int count)
        {
            switch (count)
            {
                case >= 0 and <= 15:
                    target[mapPos] = (byte)((int)MsgFormat.fixarray | count);
                    return;
                case >= 0 and <= ushort.MaxValue:
                    target[mapPos] = (byte)MsgFormat.array16;
                    SetLength16(mapPos + 1, count);
                    return;
                case >= 0 and <= int.MaxValue:
                    target[mapPos] = (byte)MsgFormat.array32;
                    SetLength32(mapPos + 1, count);
                    return;
                default:
                    throw new InvalidOperationException("unexpected count");
            }
        }
        
        private void SetLength16(int start, int count) {
            Reserve(2);
            Buffer.BlockCopy(target, start, target, start + 2, pos - start); // move +2 bytes
            BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(target, start, 2), (ushort)count);
            pos += 2;
        }
        
        private void SetLength32(int start, int count) {
            Reserve(4);
            Buffer.BlockCopy(target, start, target, start + 4, pos - start); // move +4 bytes
            BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(target, start, 4), count);
            pos += 4;
        }
        
        public int WriteArray32Start() {
            var cur = pos;
            pos = cur + 5;
            return cur;
        }
        
        public void WriteArray32End(int mapPos, int count) {
            target[mapPos] = (byte)MsgFormat.array32;
            BinaryPrimitives.WriteInt32BigEndian (new Span<byte>(target, mapPos + 1, 4), count);
        }
    }
}