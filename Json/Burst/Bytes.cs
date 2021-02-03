// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
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


        private static readonly int     upper2Lower = 'a' - 'A';  // 97 - 65 = 32

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
        
        public Bytes (String str) {
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
        
        public Bytes(ByteList array) {
            hc = 0;
            start = 0;
            end = array.Count;
            buffer = array;
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
        
        public byte Get (int idx)
        {
            if (idx < 0 || idx >= Len)
                throw new IndexOutOfRangeException();
            return buffer.array[start + idx];
        }
    
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
        
        public void Trim ()
        {
            ref var str = ref buffer.array;
            int n = start;
            for (; n < end; n++)
            {
                int c = str[n];
                if (c != ' ' && c != '\t')
                    break;
            }
            start = n;
            n = end - 1;
            for (; n >= start; n--)
            {
                int c = str[n];
                if (c != ' ' && c != '\t')
                    break;
            }
            end = n;
            this.hc = BytesConst.notHashed;
        }
        
        public void TrimFence (char c)
        {
            if (Len < 2)
                throw new InvalidOperationException("Expected len >= 2");
            ref var str = ref buffer.array;
            if (str[start] != c || str[end - 1] != c)
                throw new InvalidOperationException("Expected fence char not on start or end");
            start++;
            end--;
            this.hc = BytesConst.notHashed;
        }
    
        public void ToLowerCase()
        {
            ref var str = ref buffer.array;
            for (int i = start; i < end; i++)
            {
                int c = str[i];
                if ('A' <= c && c <= 'Z')
                    str[i] = (byte) (c + upper2Lower);
            }
            this.hc = BytesConst.notHashed;
        }
    
        public void ToUpperCase()
        {
            ref var str = ref buffer.array;
            for (int i = start; i < end; i++)
            {
                int c = str[i];
                if ('a' <= c && c <= 'z')
                    str[i] = (byte) (c - upper2Lower);
            }       
            this.hc = BytesConst.notHashed;
        }

        public bool StartsWith(Bytes value)
        {
            if (Len < value.Len)
                return false;
            ref var str     = ref buffer.array;
            ref var str2    = ref value.buffer.array;
            int     valEnd  = start + value.Len;
            int     i2 = value.start;
            for (int i = start; i < valEnd; i++ )
            {
                if (str[i] != str2[i2++])
                    return false;
            }
            return true;
        }

        public bool EndsWith(Bytes value)
        {
            if (Len < value.Len)
                return false;
            ref var str     = ref buffer.array;
            ref var str2    = ref value.buffer.array;
            int     valEnd     = value.end;
            int     i       = this.end - value.Len;
            for (int i2 = value.start; i2 < valEnd; i2++ )
            {
                if (str[i++] != str2[i2])
                    return false;
            }
            return true;
        }

        public int LastIndexOf (byte c, int start)
        {       
            if (start >= this.end)
                start = this.end - 1;
            ref var str = ref buffer.array;
            for (int n = start; n >= 0; n--)
            {
                if (str[n] == c)
                    return n;
            }
            return -1;
        }
        
        public int IndexOf (int c, int start)
        {       
            if (start < this.start)
                start = this.start;
            ref var str = ref buffer.array;
            for (int n = start; n < end; n++)
            {
                if (str[n] == c)
                    return n;
            }
            return -1;
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

        public int CompareTo(Bytes path)
        {
            ref var str = ref buffer.array;
            int     len2 = path.Len;
            int     minLen = Len <= len2 ? Len : len2;
            ref var str2 = ref path.buffer.array;
            int start2 = path.start;
            for (int n = 0; n < minLen; n++)
            {
                int dif = str[start + n] - str2[start2 + n];
                if (dif != 0)
                    return dif;
            }
            return Len - len2;
        }

#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
        public bool IsEqual32 (Str32 value) {
            return IsEqual32Ref(ref value);
        }

        public bool IsEqual32Ref(ref Str32 value) {
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
        public bool IsEqual32Ref(ref String cs) {
            return IsEqualString(cs);
        }
        
        public bool IsEqual32 (String cs) {
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

        public bool IsEqualManagedArray (byte[] bytes, int start, int length)
        {
            int len = Len;
            if (len != length - start)
                return false;
            ref var str     = ref buffer.array;
            int bytesEnd    = start + len;
            for (int i = start; i < bytesEnd; i++ )
            {
                if (str[i] != bytes[start++])
                    return false;
            }
            return true;
        }

        public bool IsEqualString (String str) {
            return Utf8Utils.IsStringEqualUtf8Ref(str, ref this);
        }
/*
#if JSON_BURST
        private bool IsEqualNativeArray(String str, int end, ref ByteList temp) {

            unsafe {
                byte* arrPtr = (byte*) Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(temp.array);
                fixed (char* strPtr = str) {
                    Encoding.UTF8.GetBytes(strPtr, str.Length, arrPtr, temp.array.Length);
                }
                var tempArr = temp.array;
                var bufArr = buffer.array;
                for (int i = 0; i < end; i++) {
                    if (tempArr[i] != bufArr[i])
                        return false;
                }
            }
            return true;
        }
#endif 
        
        // Expensive! Allocates memory on the heap. Use IsEqualString(String, ref Bytes) instead
        public bool IsEqualString (String str) {
            if (str == null)
                return false;
            int byteLen = Encoding.UTF8.GetByteCount(str);
            if (Len != byteLen)
                return false;
            Bytes temp = new Bytes(byteLen); // expensive! Allocates memory on the heap
#if JSON_BURST
            bool ret = IsEqualNativeArray(str, byteLen, ref temp.buffer);
#else
            Encoding.UTF8.GetBytes(str, 0, str.Length, temp.buffer.array, 0);
            bool ret = IsEqualManagedArray(temp.buffer.array, 0, byteLen);
#endif
            temp.Dispose();
            return ret;
        }
        
        public bool IsEqualString(String str, ref Bytes temp) {
            if (str == null)
                return false;
            int byteLen = Encoding.UTF8.GetByteCount(str);
            if (Len != byteLen)
                return false;
            temp.Clear();
            temp.EnsureCapacityAbs(byteLen);
#if JSON_BURST
            return IsEqualNativeArray(str, byteLen, ref temp.buffer);
#else
            Encoding.UTF8.GetBytes(str, 0, str.Length, temp.buffer.array, 0);
            return IsEqualManagedArray(temp.buffer.array, 0, byteLen);
#endif
        } */

        public int LastIndexOf (int c)
        {
            ref var str = ref buffer.array;
            for (int n = end - 1; n >= start; n--)
            {
                if (str[n] == c)
                    return n;
            }
            return -1;      
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

        public int Split (char separator, params Bytes [] param)
        {
            int     count   = 0;
            ref var str         = ref buffer.array;
            int     lclStart    = this.start;
            int     lclEnd      = this.end;
            for (int i = lclStart; i < lclEnd; i++)
            {
                if (str[i] == separator)
                {
                    if (count++ >= param. Length)
                        continue;
                    param[count-1].Set(ref this, lclStart, i);
                    lclStart = i + 1;
                }
            }
            if (lclStart < lclEnd && count++ < param. Length)
                param[count-1].Set(ref this, lclStart, lclEnd);
            return count;       
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

        public override String ToString() {
            return ToString(buffer, start, end - start);
        }
        
        /**
         * Must not by called from Burst. Burst cant handle managed types
         */
        public static String ToString (ByteList data, int pos, int size)
        {
#if JSON_BURST
            unsafe {
                sbyte* sbytePtr = (sbyte*)Unity.Collections.LowLevel.Unsafe.NativeListUnsafeUtility.GetUnsafePtr(data.array);
                return new String(sbytePtr, pos, size, Encoding.UTF8);
            }
#else
            /*
            sbyte[] sbyteData = (sbyte[]) (Array)data.array;
            unsafe {
                fixed (sbyte* sbytePtr = sbyteData)
                    return new String(sbytePtr, pos, size, Encoding.UTF8);
            } */
            return Encoding.UTF8.GetString(data.array, pos, size);
#endif
        }

        /*
         * Must not by called from Burst. Burst cant handle managed types
         */
        public void FromString(String str) {
            if (str == null) {
                start   = 0; 
                end = -1;
                hc  = BytesConst.notHashed;
                return;
            }
            int maxByteLen = Encoding.UTF8.GetMaxByteCount(str.Length);
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
            int byteLen = Encoding.UTF8.GetBytes(str, 0, str.Length, buffer.array, start);
#endif
            end += byteLen;
            hc = BytesConst.notHashed;
        }

        public static void CopyBytes (ByteList src, int srcPos, ByteList dst, int dstPos, int length)
        {
#if JSON_BURST
            /* unsafe {
                byte* srcPtr = (byte*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(src.array);
                byte* dstPtr = (byte*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(dst.array);
                Buffer.MemoryCopy(srcPtr + srcPos, dstPtr + dstPos, dst.Length, length);
            } */
            Unity.Collections.NativeArray<byte>.Copy(src.array, srcPos, dst.array, dstPos, length);
#else
            Buffer.BlockCopy (src.array, srcPos, dst.array, dstPos, length);
#endif          
        }
        
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
        public void AppendString(String str)
        {
            AppendString (str, 0, str. Length);
        }

        public void AppendString(String str, int offset, int len)
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

        public void Set (String val)
        {
            FromString(val);
        }
        
#if JSON_BURST
        public void AppendStr128 (ref Str128 str) {
            int strLen = str.Length;
            EnsureCapacity(Len + strLen);
            int curEnd = end;
            ref var buf = ref buffer.array;
            for (int n = 0; n < strLen; n++)
                buf[curEnd + n] = str[n];
            end += strLen;
            hc = BytesConst.notHashed;
        }

        // Note: Prefer using AppendStr32 (ref Str32 str)
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
        public void AppendStr32 (Str32 str) {
            AppendStr32Ref(ref str);
        }

        public void AppendStr32Ref (ref Str32 str) {
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
        public void AppendStr128 (ref String str) {
            AppendString(str);
        }
        
        // Note: Prefer using AppendStr32 (ref String str)
        public void AppendStr32 (String str) {
            AppendString(str);
        }
        
        public void AppendStr32Ref (ref String str) {
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

        public void AppendChar(char c)
        {
            EnsureCapacity(1);
            buffer.array[end++] = (byte)c;
            hc = BytesConst.notHashed;
        }
        
        public void AppendChar2(char c0, char c1)
        {
            EnsureCapacity(2);
            buffer.array[end++] = (byte)c0;
            buffer.array[end++] = (byte)c1;
            hc = BytesConst.notHashed;
        }
        
        public void AppendChar(char c, int count)
        {
            int newEnd = Len + count;
            ref var buf = ref buffer.array;
            EnsureCapacity(count);
            for (int n = end; n < newEnd; n++)
                buf[n] = (byte)c;
            end = newEnd;
            hc = BytesConst.notHashed;
        }

        public void AppendReplace (Bytes src, Bytes target, Bytes replacement)
        {
            EnsureCapacity(src.Len);
            int             strStart = src.StartPos;
            ref ByteList    srcBuf = ref src.buffer;
            while (true)
            {
                int idx = src.IndexOf(target, strStart);
                if (idx != -1)
                {
                    AppendArray (ref srcBuf, strStart, idx);
                    AppendBytes (ref replacement);              
                    strStart = idx + target.Len;
                    continue;
                }
                AppendArray (ref srcBuf, strStart, src.EndPos);
                hc = BytesConst.notHashed;
                return;
            }       
        }
    }
}
