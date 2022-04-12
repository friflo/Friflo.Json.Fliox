// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    public readonly struct Utf8String {
        private   readonly  Utf8StringBuffer    buffer;
        private   readonly  int                 start;
        private   readonly  int                 len;

        public    override  string              ToString() => GetName();

        public Utf8String (Utf8StringBuffer buffer, int start, int len) {
            this.buffer = buffer;
            this.start  = start;
            this.len    = len;
        }
        
        public bool IsEqual (ref Bytes value) {
            var left   = new Span<byte> (buffer.Buf, start, len);
            var right  = new Span<byte> (value.buffer.array, value.start, value.Len);
            return left.SequenceEqual(right);
        }
        
        private string GetName() {
            var buf = buffer.Buf;
            if (buf == null)
                return null;
            return Utf8StringBuffer.Utf8.GetString(buffer.Buf, start, len);  
        }
    }
    
    public class Utf8StringBuffer {
        private   byte[]    buf = new byte[2];
        private   int       pos;
        
        internal  byte[]    Buf => buf;
        
        internal static readonly UTF8Encoding Utf8    = new UTF8Encoding(false);
        
        public Utf8String Add (Utf8StringBuffer buffer, ReadOnlySpan<char> value) {
            var len     = Utf8.GetByteCount(value);
            int destPos = Reserve(len);
            var dest    = new Span<byte>(buf, destPos, len);
            Utf8.GetBytes(value, dest);
            return new Utf8String(buffer, destPos, len);
        }
        
        private int Reserve (int len) {
            int curPos  = pos;
            int newLen  = curPos + len;
            int bufLen  = buf.Length;
            if (curPos + len > bufLen) {
                var doubledLen = 2 * bufLen; 
                if (newLen < doubledLen) {
                    newLen = doubledLen;
                }
                var newBuffer = new byte [newLen];
                Buffer.BlockCopy(buf, 0, newBuffer, 0, curPos);
                buf = newBuffer;
            }
            pos += len;
            return curPos;
        }
    }
}

#endif
