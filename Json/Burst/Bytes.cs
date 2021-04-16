// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
    using Str128 = Unity.Collections.FixedString128;
#else
    using Str32 = System.String;
    using Str128 = System.String;
    // ReSharper disable InconsistentNaming
    [assembly: CLSCompliant(true)]
#endif

namespace Friflo.Json.Burst
{
    public struct BytesConst {
        public static readonly int  notHashed = 0;
    }

    public interface IMapKey<K> where K : struct
    {
        bool    IsEqual(ref K other);
        bool    IsSet();
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial struct Bytes : IDisposable, IMapKey<Bytes>
    {

        public  int             hc; //      = notHashed;

        public  int             start;
        public  int             end;
        public  ByteList        buffer;
        
        public  int             Len => end - start;
        public  int             StartPos => start;
        public  int             EndPos => end;

        public void InitBytes(int capacity) {
            if (!buffer.IsCreated())
                buffer = new ByteList(capacity, AllocType.Persistent);
        }

        /// <summary>
        /// Dispose all internal used arrays.
        /// Only required when running with JSON_BURST within Unity. 
        /// </summary>
        public void Dispose() {
            if (buffer.IsCreated())
                buffer.Dispose();
        }
        
        public Bytes SwapWithDefault() {
            Bytes ret = this;
            this = default(Bytes);
            return ret;
        }

        public Bytes(int capacity) {
            hc = 0;
            start = 0;
            end = 0;
            buffer = new ByteList(capacity, AllocType.Persistent);
        }
        
        public Bytes(int capacity, AllocType allocType) {
            hc = 0;
            start = 0;
            end = 0;
            buffer = new ByteList(capacity, allocType);
        }
        
        public Bytes (string str) {
            hc = 0;
            start = 0;
            end = 0;
            buffer = new ByteList(0, AllocType.Persistent);
            FromString(str);
        }
        
        public Bytes (ref Bytes src) {
            hc =    src.hc;
            start = 0;
            end =   0;
            buffer = new ByteList(src.Len, AllocType.Persistent);
            AppendBytes(ref src);
        }

        public void SetDim (int start, int end)
        {
            this.start  = start; 
            this.end    = end;
            this.hc     = BytesConst.notHashed;
        }       

        public void SetStart (int start)
        {
            this.start  = start;
            this.hc     = BytesConst.notHashed;
        }

        public void SetEnd (int end)
        {
            this.end    = end;
            this.hc     = BytesConst.notHashed;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            this.start  = 0; 
            this.end    = 0;
            this.hc     = BytesConst.notHashed;
        }

        public void Set(ref Bytes source, int start, int end) {
            int l = source.Len;
            EnsureCapacityAbs(l);
            this.start = 0;
            this.end = l;
            this.hc = source.hc;
            var dst = buffer.array;
            var src = source.buffer.array;
            for (int n = 0; n < Len; n++)
                dst[n] = src[n];
        }

        public void Set(ref Bytes src)
        {
            Set (ref src, src.start, src.end);
        }
        
        public int IndexOf (Bytes subStr, int start)
        {
            if (start < this.start)
                start = this.start;
            ref var         str = ref buffer.array;
            int             end1        = end - subStr.Len;
            int             start2      = subStr.start; 
            int             end2        = subStr.end;
            ref var         str2    = ref subStr.buffer.array;
            for (int n = start; n <= end1; n++)
            {
                int off = n - start2;
                int i   = start2;
                for (; i < end2; i++)
                {
                    if (str[i + off] != str2[i])
                        break;      
                }
                if (i == end2)
                    return n;           
            }
            return -1;
        }

#if JSON_BURST
        public bool IsEqual32(in Str32 value) {
            int len = Len;
            if (len != value.Length)
                return false;
            ref var str = ref buffer.array;
            int valEnd  = start + len;
            int i2 = 0;
            // ref ByteList str2 = ref fix;
            for (int i = start; i < valEnd; i++) {
                if (str[i] != value[i2++])
                    return false;
            }
            return true;
        }
#else
        public bool IsEqual32 (in string cs) {
            return IsEqualString (cs);
        }
#endif

        public bool IsSet() {
            return buffer.IsCreated();
        }

        public bool IsEqual(ref Bytes value) {
            return IsEqualBytes(ref value);
        }

        public bool IsEqualBytes(ref Bytes value)
        {
            int len = Len;
            if (len != value.Len)
                return false;
            ref var str     = ref buffer.array;
            int     valEnd  = start + len;
            int     i2      = value.start;
            ref var str2 = ref value.buffer.array;
            for (int i = start; i < valEnd; i++ )
            {
                if (str[i] != str2[i2++])
                    return false;
            }
            return true;
        }

        public bool IsEqualString (string str) {
            return Utf8Utils.IsStringEqualUtf8(str, in this);
        }

        public bool IsEqualArray(byte[] array) {
#if JSON_BURST
            if (Len != array.Length)
                return false;
            int pos = 0;
            var buf = buffer.array;
            var endPos = end; 
            for (int n = start; n < endPos; n++) {
                if (buf[n] != array[pos++])
                    return false;
            }
            return true;
#else
            var span  = new ReadOnlySpan<byte>(buffer.array, start, Len);
            var other = new ReadOnlySpan<byte>(array);
            return span.SequenceEqual(other);
#endif
        }

        public override bool Equals (Object obj) {
            if (obj == null)
                return false;
            Bytes value = (Bytes)obj; // Bytes is a struct -> so unboxing -> boxing before allocates memory on the heap!
            return IsEqualBytes(ref value);
        }

        internal int GetHash()
        {
            return hc;
        }
        
        public int UpdateHashCode() {
            ref var str = ref buffer.array;
            int     h = Len;
            // Rotate by 3 bits and XOR the new value.
            for (int i = start; i < end; i++)
                h = (h << 3) | (h >> (29)) ^ str[i];
            return hc = Math.Abs(h);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            if (hc != BytesConst.notHashed)
                return hc;
            return hc = UpdateHashCode();
        }

#if JSON_BURST
        public Str32 ToStr32() {
            var ret = new Unity.Collections.FixedString32();
            ref var buf = ref buffer.array;
            for (int i = start; i < end; i++)
                ret.Add(buf[i]);
            return ret;
        }
        
        public Str128 ToStr128() {
            var ret = new Unity.Collections.FixedString128();
            ref var buf = ref buffer.array;
            for (int i = start; i < end; i++)
                ret.Add(buf[i]);
            return ret;
        }
#else
        public Str32 ToStr32() {
            return ToString();
        }
        
        public Str128 ToStr128() {
            return ToString();
        }
#endif

        public override string ToString() {
            return ToString(buffer, start, end - start);
        }
        
        /**
         * Must not by called from Burst. Burst cant handle managed types
         */
        public static string ToString (ByteList data, int pos, int size)
        {
#if JSON_BURST
            unsafe {
                sbyte* sbytePtr = (sbyte*)Unity.Collections.LowLevel.Unsafe.NativeListUnsafeUtility.GetUnsafePtr(data.array);
                return new string(sbytePtr, pos, size, Encoding.UTF8);
            }
#else
            /*
            sbyte[] sbyteData = (sbyte[]) (Array)data.array;
            unsafe {
                fixed (sbyte* sbytePtr = sbyteData)
                    return new string(sbytePtr, pos, size, Encoding.UTF8);
            } */
            return Encoding.UTF8.GetString(data.array, pos, size);
#endif
        }
        
        public string GetString (ref char[] dst)
        {
#if JSON_BURST
            return ToString();
#else
            int maxCharCount = utf8.GetMaxCharCount(Len);
            if (maxCharCount > dst.Length)
                dst = new char[maxCharCount];

            int writtenChars = utf8.GetChars(buffer.array, start, Len, dst, 0);
            /* unsafe {
                fixed (char* chars = dst) {
                    return new string(chars, 0, writtenChars);
                }
            } */
            /* ReadOnlySpan<char> span = new ReadOnlySpan<char>(dst, 0, writtenChars);
               return new string(span); */
            return new string(dst, 0, writtenChars);
#endif
        }
        
        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

        /*
         * Must not by called from Burst. Burst cant handle managed types
         */
        public void FromString(string str) {
            if (str == null)
                throw new NullReferenceException("FromString() - string parameter must not be null");
            int maxByteLen = utf8.GetMaxByteCount(str.Length);
            EnsureCapacity(maxByteLen);

#if JSON_BURST
            int byteLen = 0;
            unsafe {
                byte* arrPtr = (byte*)Unity.Collections.LowLevel.Unsafe.NativeListUnsafeUtility.GetUnsafePtr(buffer.array);
                fixed (char* strPtr = str) {
                    if (arrPtr != null)
                        byteLen = Encoding.UTF8.GetBytes(strPtr, str.Length, arrPtr, buffer.array.Length);
                }
            }
#else
            int byteLen = utf8.GetBytes(str, 0, str.Length, buffer.array, start);
#endif
            end += byteLen;
            hc = BytesConst.notHashed;
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacityAbs(int size) {
            if (size <= buffer. Count)
                return;
            if (size < 2 * buffer. Count)
                size = 2 * buffer. Count;

            buffer.Resize(size);
        }
        
        public void EnsureCapacity(int count) {
            EnsureCapacityAbs(end + count);
        }
        
        // ------------------------------ Append methods ------------------------------
        public void AppendString(string str)
        {
            AppendString (str, 0, str. Length);
        }

        public void AppendString(string str, int offset, int len)
        {
            EnsureCapacity(len);
            int pos = end;
            int strEnd = offset + len;
            var buf = buffer.array;
            for (int n = offset; n < strEnd; n++)
                buf[pos++] = (byte) str[ n ];
            end += len;
            hc = BytesConst.notHashed;
        }

        public void Set (string val)
        {
            FromString(val);
        }
        
#if JSON_BURST
        public void AppendStr128 (in Str128 str) {
            int strLen = str.Length;
            EnsureCapacity(Len + strLen);
            int curEnd = end;
            ref var buf = ref buffer.array;
            for (int n = 0; n < strLen; n++)
                buf[curEnd + n] = str[n];
            end += strLen;
            hc = BytesConst.notHashed;
        }

        public void AppendStr32 (in Str32 str) {
            int strLen = str.Length;
            EnsureCapacity(Len + strLen);
            int curEnd = end;
            ref var buf = ref buffer.array;
            for (int n = 0; n < strLen; n++)
                buf[curEnd + n] = str[n];
            end += strLen;
            hc = BytesConst.notHashed;
        }

#else
        public void AppendStr128 (in string str) {
            AppendString(str);
        }
        
        // Note: Prefer using AppendStr32 (ref string str)
        public void AppendStr32 (in string str) {
            AppendString(str);
        }
#endif

        public void AppendArray(ref ByteList str, int start, int end)
        {
            int strLen = end - start;
            ref var buf = ref buffer.array;
            ref var strArr = ref str.array;
            int thisEnd = this.end;
            EnsureCapacity(strLen);
            for (int n = 0; n < strLen; n++)
                buf[thisEnd + n] = strArr[start+n];
            this.end += strLen;
            hc = BytesConst.notHashed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendChar(char c)
        {
            EnsureCapacityAbs(end + 1);
            buffer.array[end++] = (byte)c;
            hc = BytesConst.notHashed;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendChar2(char c0, char c1)
        {
            EnsureCapacityAbs(end + 2);
            buffer.array[end++] = (byte)c0;
            buffer.array[end++] = (byte)c1;
            hc = BytesConst.notHashed;
        }
    }
}
