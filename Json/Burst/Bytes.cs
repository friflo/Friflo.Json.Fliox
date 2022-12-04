// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
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
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial struct Bytes : IDisposable
    {
        public  int             start;
        public  int             end;
        public  byte[]          buffer;
        
        public  int             Len => end - start;

        /// called previously <see cref="ByteList"/> constructor
        public void InitBytes(int capacity) {
            if (IsCreated())
                return;
            buffer = AllocateBuffer(capacity);
        }
        
        public bool IsCreated() {
            return buffer != null;
        }

        /// <summary>
        /// Dispose all internal used arrays.
        /// Only required when running with JSON_BURST within Unity.
        /// was previous in <see cref="ByteList.Dispose()"/>
        /// </summary>
        public void Dispose() {
            if (!IsCreated())
                return;
            DebugUtils.UntrackAllocation(buffer);
            buffer = null;
        }
        
        /// was previous in <see cref="ByteList.Dispose(Untracked)"/>
        public void Dispose(Untracked _) {
            if (!IsCreated())
                return;
            buffer = null;
        }
        
        public Bytes SwapWithDefault() {
            Bytes ret   = this;
            this        = default;
            return ret;
        }

        /// called previously <see cref="ByteList"/> constructor
        public Bytes(int capacity) {
            start   = 0;
            end     = 0;
            buffer  = AllocateBuffer(capacity);
        }
        
        
        /* called previously <see cref="ByteList"/> constructor
        public Bytes(int capacity, AllocType allocType) {
            start   = 0;
            end     = 0;
            buffer  = AllocateBuffer(capacity);
        } */
        
        /// called previously <see cref="ByteList"/> constructor
        public Bytes (string str) {
            start   = 0;
            end     = 0;
            buffer  = AllocateBuffer(0);
            AppendStringUtf8(str);
        }
        
        /// <summary> <see cref="Bytes32.FromBytes"/> expect capacity + 32</summary>
        public Bytes (string str, Untracked _) {
            int byteLen =  utf8.GetByteCount(str);
           
            buffer = new byte[byteLen + 32];
#if JSON_BURST
            int byteLen = 0;
            unsafe {
                byte* arrPtr = (byte*)Unity.Collections.LowLevel.Unsafe.NativeListUnsafeUtility.GetUnsafePtr(buffer.array);
                fixed (char* strPtr = str) {
                    if (arrPtr != null)
                        Encoding.UTF8.GetBytes(strPtr, str.Length, arrPtr, buffer.array.Length);
                }
            }
#else
            utf8.GetBytes(str, 0, str.Length, buffer, 0);
#endif
            start = 0;
            end     = byteLen;
        }
        
        public Bytes (ref Bytes src) {
            start   = 0;
            end     = 0;
            buffer  = AllocateBuffer(src.Len);
            AppendBytes(ref src);
        }
        
        /// was previous in <see cref="ByteList"/> constructor
        private static byte[] AllocateBuffer(int size) {
            var result = new byte [size];
            DebugUtils.TrackAllocation(result);
            return result;
        }
        
        /// was previous in <see cref="ByteList.Resize"/>
        public void Resize(int size) {
            byte[] newArr = new byte[size];
            int len = size < buffer.Length ? size : buffer.Length;
            Buffer.BlockCopy (buffer, 0, newArr, 0, len);
            //  for (int i = 0; i < len; i++)
            //      newArr[i] = array[i];
            DebugUtils.UntrackAllocation(buffer);
            DebugUtils.TrackAllocation(newArr);
            buffer = newArr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            this.start  = 0; 
            this.end    = 0;
        }

        public void Set(ref Bytes source, int start, int end) {
            int l = source.Len;
            EnsureCapacityAbs(l);
            this.start = 0;
            this.end = l;
            var dst = buffer;
            var src = source.buffer;
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
            var str     = buffer;
            int end1    = end - subStr.Len;
            int start2  = subStr.start; 
            int end2    = subStr.end;
            var str2    = subStr.buffer;
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
        
        public bool IsIntegral() {
            int len = end - start;
            if (len == 0)
                return false;
            var str = buffer;
            byte c = str[0];
            if (len == 1) {
                return '0' <= c && c <= '9'; 
            }
            var begin = start;
            if (c == '-') {
                c = str[1];
                begin++;
            }
            // no leading 0
            if (c < '1' || c > '9')
                return false;
            for (int i = begin + 1; i < end; i++) {
                c = str[i];
                if ('0' <= c && c <= '9')
                    continue;
                return false;
            }
            return true;
        }
        
        public const int MinGuidLength = 36; // 12345678-1234-1234-1234-123456789abc
        public const int MaxGuidLength = 68; // {0x12345678,0x1234,0x1234,{0x12,0x34,0x12,0x34,0x56,0x78,0x9a,0xbc}}

        /// In case of Unity <paramref name="str"/> is not null. Otherwise null.
        public bool TryParseGuid(out Guid guid, out string str) {
            int len = end - start;
            if (len < MinGuidLength || MaxGuidLength < len) {
                str = null;
                guid = new Guid();
                return false;
            }
#if UNITY_5_3_OR_NEWER
            str = AsString();
            return Guid.TryParse(str, out guid);
#else
            str = null;
            Span<char> span = stackalloc char[len];
            var array   = buffer;
            for (int n = 0; n < len; n++)
                span[n] = (char)array[start + n];
            return Guid.TryParse(span, out guid);
#endif
        }
        
        public void AppendGuid(in Guid guid) {
#if UNITY_5_3_OR_NEWER
            var str = guid.ToString();
            AppendString(str);
#else
            Span<char> span = stackalloc char[MaxGuidLength];
            if (!guid.TryFormat(span, out int charsWritten))
                throw new InvalidOperationException("AppendGuid() failed");
            EnsureCapacity(charsWritten);
            var array   = buffer;
            for (int n = 0; n < charsWritten; n++)
                array[start + n] = (byte)span[n];
            end += charsWritten;
#endif
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
        public bool IsEqual(in Bytes value)
        {
            int len = end - start;
            if (len != value.end - value.start)
                return false;
            var     str     = buffer;
            int     valEnd  = start + len;
            int     i2      = value.start;
            var     str2    = value.buffer;
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

        /// <summary>deprecated: Use <see cref="Utf8String"/> instead </summary>
        public bool IsEqualArray(byte[] array) {
            int len = end - start;
#if UNITY_5_3_OR_NEWER
            if (len != array.Length)
                return false;
            int pos = 0;
            var buf = buffer;
            var endPos = end; 
            for (int n = start; n < endPos; n++) {
                if (buf[n] != array[pos++])
                    return false;
            }
            return true;
#else
            var span  = new ReadOnlySpan<byte>(buffer, start, len);
            var other = new ReadOnlySpan<byte>(array);
            return span.SequenceEqual(other);
#endif
        }

        public override bool Equals (object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual()");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use BytesHash and its Equality comparer");
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
            return AsString();
        }
        
        public Str128 ToStr128() {
            return AsString();
        }
#endif
        
        public string AsString() {
            return ToString(buffer, start, end - start);
        }
        
        public byte[] AsArray() {
            var len     = Len;
            var array   = new byte[len];
            var buf     =  buffer;
            for (int i = 0; i < len; i++) {
                array[i] = buf[i + start];
            }
            return array;
        }

        /// Note: Use <see cref="AsString"/> instead to simplify code navigation
        public override string ToString() {
            return ToString(buffer, start, end - start);
        }
        
        /**
         * Must not by called from Burst. Burst cant handle managed types
         */
        public static string ToString (byte[] data, int pos, int size)
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
            return Encoding.UTF8.GetString(data, pos, size);
#endif
        }
        
        public char[] GetChars (ref char[] dst, out int length)
        {
#if JSON_BURST
            return ToString();
#else
            int len             = end - start;
            int maxCharCount    = utf8.GetMaxCharCount(len);
            if (maxCharCount > dst.Length)
                dst = new char[maxCharCount];

            length = utf8.GetChars(buffer, start, len, dst, 0);
            return dst;
            /* unsafe {
                fixed (char* chars = dst) {
                    return new string(chars, 0, writtenChars);
                }
            } */
            /* ReadOnlySpan<char> span = new ReadOnlySpan<char>(dst, 0, writtenChars);
               return new string(span); */
            // return new string(dst, 0, writtenChars);
#endif
        }
        
        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

        /*
         * Must not by called from Burst. Burst cant handle managed types
         */
        public void AppendStringUtf8(string str) {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            int maxByteLen = end + utf8.GetMaxByteCount(str.Length);
            if (maxByteLen > buffer.Length) {
                DoubleSize(maxByteLen);
            }
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
            int byteLen = utf8.GetBytes(str, 0, str.Length, buffer, start);
#endif
            end += byteLen;
        }

        public void DoubleSize(int size) {
            var capacity = buffer.Length;
            if (size < 2 * capacity) {
                size = 2 * capacity;
            }
            Resize(size);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacityAbs(int size) {
            if (size <= buffer.Length)
                return;
            DoubleSize(size);
        }
        
        public void EnsureCapacity(int count) {
            EnsureCapacityAbs(end + count);
        }
        
        // ------------------------------ Append methods ------------------------------
        public void AppendString(string str)
        {
            int len = str.Length;
            if (end + len > buffer.Length) {
                DoubleSize(end + len);    
            }
            int pos = end;
            var buf = buffer;
            for (int n = 0; n < len; n++)
                buf[pos++] = (byte) str[ n ];
            end += len;
        }

        public void Set (string val)
        {
            AppendStringUtf8(val);
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

        public void AppendArray(byte[] str, int start, int end)
        {
            int strLen  = end - start;
            int thisEnd = this.end;
            EnsureCapacity(strLen);
            var buf     = buffer;
            for (int n = 0; n < strLen; n++)
                buf[thisEnd + n] = str[start+n];
            this.end += strLen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendChar(char c)
        {
            if (end >= buffer.Length) {
                DoubleSize(end + 1);
            }
            buffer[end++] = (byte)c;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendChar2(char c0, char c1)
        {
            if (end + 2 > buffer.Length) {
                DoubleSize(end + 2);
            }
            buffer[end++] = (byte)c0;
            buffer[end++] = (byte)c1;
        }
        
        public void AppendGuid (in Guid guid, char[] buf) {
#if UNITY_5_3_OR_NEWER
            AppendString(guid.ToString());
#else
            var dest = new Span<char>(buf);
            if (!guid.TryFormat(dest, out var len))
                throw new InvalidOperationException($"Guid.TryFormat failed: {guid}");
            EnsureCapacity(len);
            int thisEnd = end;
            for (int n = 0; n < len; n++) {
                buffer[thisEnd + n] = (byte)buf[n];
            }
            end += len;
#endif
        }
    }
}
