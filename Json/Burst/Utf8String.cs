// Copyright (c) Ullrich Praetz. All rights reserved.
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

        // ReSharper disable once UnusedMember.Local
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
            dest.hc     = BytesConst.notHashed;
            var dst = dest.buffer.array;
            var src = buffer.Buf;
            for (int n = 0; n < l; n++)
                dst[n] = src[n + start];
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
        
        public bool IsEqual (in Utf8String value) {
            throw new NotImplementedException();
        }
#else
        public ReadOnlySpan<byte> ReadOnlySpan => new ReadOnlySpan<byte> (buffer.Buf, start, len);

        public bool IsEqual (in Utf8String value) {
            return ReadOnlySpan.SequenceEqual(value.ReadOnlySpan);
        }
#endif
        public bool IsEqual (ref Bytes value) {
            if (len != value.Len)
                return false;
#if UNITY_5_3_OR_NEWER
            return ArraysEqual(buffer.Buf, start, value.buffer.array, len);
#else
            var left   = ReadOnlySpan;
            var right  = new ReadOnlySpan<byte> (value.buffer.array, value.start, value.Len);
            return left.SequenceEqual(right);
#endif
        }
        
        public string AsString() {
            var buf = buffer?.Buf;
            if (buf == null)
                return null;
            return Utf8Buffer.Utf8.GetString(buf, start, len);  
        }
    }
    
    /// <summary>
    /// Using <see cref="IUtf8Buffer"/> instead of <see cref="Utf8Buffer"/> enables using
    /// instances of <see cref="Utf8Buffer"/> as private fields.
    /// This preserve the immutable behavior when using these fields. 
    /// </summary>
    public interface IUtf8Buffer
    {
#if UNITY_5_3_OR_NEWER
        Utf8String  GetOrAdd    (string value);
        Utf8String  Add         (string value);

#else
        Utf8String  GetOrAdd    (ReadOnlySpan<char> value);
        Utf8String  Add         (ReadOnlySpan<char> value);
#endif
    }
    
    public sealed class Utf8Buffer : IUtf8Buffer {
        private             byte[]              buf;
        private             int                 pos;
        private  readonly   List<Utf8String>    strings = new List<Utf8String>();
        
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
                if (temp.SequenceEqual(str.ReadOnlySpan))
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
        
        public Utf8String Add (Bytes bytes) {
            var len     = bytes.Len;
            int destPos = Reserve(len);
            Buffer.BlockCopy(bytes.buffer.array, bytes.start, buf, destPos, len);
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


