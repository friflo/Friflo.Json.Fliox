// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Burst
{
    public readonly struct Utf8String {
        private   readonly  Utf8Buffer  buffer;
        private   readonly  int         start;
        private   readonly  int         len;

        public    override  string      ToString()  => GetName();
        public              bool        IsNull      => buffer?.Buf == null;

        internal Utf8String (Utf8Buffer buffer, int start, int len) {
            this.buffer = buffer;
            this.start  = start;
            this.len    = len;
        }
        
        public ReadOnlySpan<byte> ReadOnlySpan () {
            return new ReadOnlySpan<byte> (buffer.Buf, start, len);
        }
        
        internal Span<byte> Span () {
            return new Span<byte> (buffer.Buf, start, len);
        }
        
        public bool IsEqual (ref Bytes value) {
            var left   = Span();
            var right  = new Span<byte> (value.buffer.array, value.start, value.Len);
            return left.SequenceEqual(right);
        }
        
        private string GetName() {
            var buf = buffer?.Buf;
            if (buf == null)
                return null;
            return Utf8Buffer.Utf8.GetString(buffer.Buf, start, len);  
        }
    }
    
    public class Utf8Buffer {
        private             byte[]              buf = new byte[32];
        private             int                 pos;
        private  readonly   List<Utf8String>    strings = new List<Utf8String>();
        
        internal            byte[]              Buf         => buf;
        public override     string              ToString()  => $"count: {strings.Count}";
        
        internal static readonly UTF8Encoding   Utf8    = new UTF8Encoding(false);

        // ReSharper disable once EmptyConstructor - find all instantiations
        public Utf8Buffer() {}
        
        public Utf8String GetOrAdd (ReadOnlySpan<char> value) {
            var len         = Utf8.GetByteCount(value);
            Span<byte> span = stackalloc byte[len];
            Utf8.GetBytes(value, span);
            foreach (var str in strings) {
                if (span.SequenceEqual(str.Span()))
                    return str;
            }
            return Add(value);
        }
        
        public Utf8String Add (ReadOnlySpan<char> value) {
            var len     = Utf8.GetByteCount(value);
            int destPos = Reserve(len);
            var dest    = new Span<byte>(buf, destPos, len);
            Utf8.GetBytes(value, dest);
            var utf8    = new Utf8String(this, destPos, len);
            strings.Add(utf8);
            return utf8;
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
