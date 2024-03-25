// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Burst
{
    public readonly struct Utf8String {
        private   readonly  Utf8Buffer  buffer;
        // todo - may add 64 bit alignment (multiple of 8) for start
        // start is currently not a multiple of 8. So equality checks with another byte[] starting at 0 is not CPU optimal.
        // Consider: in case of many string entries using alignment will degrade memory locality
        public    readonly  int         start;
        public    readonly  int         len;

        public    override  string      ToString()  => AsString();
        public              bool        IsNull      => buffer?.Buf == null;
        
        internal Utf8String (Utf8Buffer buffer, int start, int len) {
            this.buffer = buffer;
            this.start  = start;
            this.len    = len;
            // hashCode    = ComputeHash(buffer.Buf, start, start + len);
        }

        // using as resulted in 20% less performance - not convinced to use it
        private static int ComputeHash(byte[] array, int start, int end) {
            var result  = 0;
            for (int n = start; n < end; n++) {
                result = (result * 31) ^ array[n];
            }
            return result;
        }
        
        public void CopyTo(ref Bytes dest) {
            int l = len;
            dest.EnsureCapacityAbs(l);
            dest.start  = 0;
            dest.end    = l;
            var dst = dest.buffer;
            var src = buffer.Buf;
            for (int n = 0; n < l; n++)
                dst[n] = src[n + start];
        }

        public ReadOnlySpan<byte> ReadOnlySpan => new ReadOnlySpan<byte> (buffer.Buf, start, len);

        public bool IsEqual (in Utf8String value) {
            return ReadOnlySpan.SequenceEqual(value.ReadOnlySpan);
        }

        public bool IsEqual (in Bytes value) {
            if (len != value.Len)
                return false;
            var left   = ReadOnlySpan;
            var right  = value.AsSpan();
            return left.SequenceEqual(right);
        }
        
        public string AsString() {
            var buf = buffer?.Buf;
            if (buf == null)
                return null;
            return Utf8Buffer.Utf8.GetString(buf, start, len);  
        }
        
        public Bytes AsBytes () {
            return new Bytes { buffer = buffer.Buf, start = start, end = start + len };
        }
    }
    
    /// <summary>
    /// Using <see cref="IUtf8Buffer"/> instead of <see cref="Utf8Buffer"/> enables using
    /// instances of <see cref="Utf8Buffer"/> as private fields.
    /// This preserve the immutable behavior when using these fields. 
    /// </summary>
    public interface IUtf8Buffer
    {
        Utf8String  GetOrAdd    (string value);
        Utf8String  GetOrAdd    (ReadOnlySpan<char> value);
        Utf8String  Add         (string value);
        Utf8String  Add         (ReadOnlySpan<char> value);
    }
    
    public sealed class Utf8Buffer : IUtf8Buffer {
        private             byte[]              buf;
        private             int                 pos;
        private  readonly   List<Utf8String>    strings = new List<Utf8String>();
        /// <summary>Important! Is internal by intention </summary>
        internal            byte[]              Buf         => buf;
        public override     string              ToString()  => $"count: {strings.Count}";
        
        internal static readonly UTF8Encoding   Utf8    = new UTF8Encoding(false);

        // ReSharper disable once EmptyConstructor - find all instantiations
        public Utf8Buffer() {
            buf = new byte[32];
            Clear();
        }

        public void Clear() {
            pos = 0;
            strings.Clear();
        }
        
        public Utf8String GetOrAdd (string value) {
            return GetOrAdd(value.AsSpan());
        }
        
        public Utf8String GetOrAdd (ReadOnlySpan<char> value) {
            var len         = Utf8.GetByteCount(value);
            Span<byte> temp = stackalloc byte[len];
            Utf8.GetBytes(value, temp);
            foreach (var str in strings) {
                if (temp.SequenceEqual(str.ReadOnlySpan))
                    return str;
            }
            return Add(value);
        }
        
        public Utf8String Add (string value) {
            return Add(value.AsSpan());
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

        
        public Utf8String Add (Bytes bytes, bool reusable) {
            var len     = bytes.Len;
            int destPos = Reserve(len);
            Buffer.BlockCopy(bytes.buffer, bytes.start, buf, destPos, len);
            var utf8    = new Utf8String(this, destPos, len);
            if (!reusable) {
                return utf8;
            }
            strings.Add(utf8);
            return utf8;
        }
        
        public Bytes[] AsBytes() {
            // create a buffer copy to avoid leaking the internal buffer as a mutable array
            var buffer = new byte[pos];
            Buffer.BlockCopy(buf, 0, buffer, 0, pos);
            var bytes = new Bytes[strings.Count];
            for (int n = 0; n < strings.Count; n++) {
                var value   = strings[n];
                var start   = value.start;
                var str     = new Bytes { buffer = buffer, start = start, end = start + value.len };
                bytes[n]    = str;
            }
            return bytes;
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


