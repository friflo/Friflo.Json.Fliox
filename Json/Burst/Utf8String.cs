// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Burst
{
    public readonly struct Utf8String {
        private   readonly  Utf8Buffer  buffer;
        internal  readonly  int         start;
        internal  readonly  int         len;

        public    override  string      ToString()  => GetName();
        public              bool        IsNull      => buffer?.Buf == null;

        internal Utf8String (Utf8Buffer buffer, int start, int len) {
            this.buffer = buffer;
            this.start  = start;
            this.len    = len;
        }
        
#if UNITY_5_3_OR_NEWER
        public static bool ArraysEqual(byte[] left, int leftStart, byte[] right, int len) {
            var pos     = leftStart;
            for (int n = 0; n < len; n++) {
                if (left[pos++] != right[n])
                    return false;
            }
            return true;
        }
#else
        public ReadOnlySpan<byte> ReadOnlySpan () {
            return new ReadOnlySpan<byte> (buffer.Buf, start, len);
        }
#endif
        
        public bool IsEqual (ref Bytes value) {
#if UNITY_5_3_OR_NEWER
            if (len != value.Len)
                return false;
            return ArraysEqual(buffer.Buf, start, value.buffer.array, len);
#else
            var left   = ReadOnlySpan();
            var right  = new ReadOnlySpan<byte> (value.buffer.array, value.start, value.Len);
            return left.SequenceEqual(right);
#endif
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
        
#if UNITY_5_3_OR_NEWER
        public Utf8String GetOrAdd (string value) {
            var len         = Utf8.GetByteCount(value);
            var temp        = new byte[len];
            Utf8.GetBytes(value, 0, value.Length, temp, 0);
            foreach (var str in strings) {
                if (len == str.len) {
                    if (Utf8String.ArraysEqual(buf, str.start, temp, len))
                        return str;
                }
            }
            return Add(value);
        }
        
        public Utf8String Add (string value) {
            var len     = Utf8.GetByteCount(value);
            int destPos = Reserve(len);
            Utf8.GetBytes(value, 0, value.Length, buf, destPos);
            var utf8    = new Utf8String(this, destPos, len);
            strings.Add(utf8);
            return utf8;
        }
#else
        public Utf8String GetOrAdd (ReadOnlySpan<char> value) {
            var len         = Utf8.GetByteCount(value);
            Span<byte> temp = stackalloc byte[len];
            Utf8.GetBytes(value, temp);
            foreach (var str in strings) {
                if (temp.SequenceEqual(str.ReadOnlySpan()))
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
#endif
        
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


