// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Friflo.Json.Burst.Utils;

// JSON_BURST_TAG
using Str32 = System.String;
using Str128 = System.String;

// ReSharper disable InconsistentNaming
[assembly: CLSCompliant(true)]

namespace Friflo.Json.Burst
{
    [CLSCompliant(true)]
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
            DebugUtils.UntrackAllocationObsolete(buffer);
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
        
        // JSON_BURST_TAG
        /// <summary> <see cref="Bytes32.FromBytes"/> expect capacity + 32</summary>
        public Bytes (string str, Untracked _) {
            int byteLen =  utf8.GetByteCount(str);
           
            buffer = new byte[byteLen + 32];
            utf8.GetBytes(str, 0, str.Length, buffer, 0);
            start = 0;
            end     = byteLen;
        }
        
        public Bytes (in Bytes src) {
            start   = 0;
            end     = 0;
            buffer  = AllocateBuffer(src.Len);
            AppendBytes(src);
        }
        
        public Bytes(ReadOnlySpan<byte> value) {
            start   = 0;
            end     = value.Length;
            buffer  = AllocateBuffer(end);
            AppendBytesSpan(value);
        }
        
        /// was previous in <see cref="ByteList"/> constructor
        private static byte[] AllocateBuffer(int size) {
            var result = new byte [size];
            DebugUtils.TrackAllocationObsolete(result);
            return result;
        }
        
        /// was previous in <see cref="ByteList.Resize"/>
        public void Resize(int size) {
            byte[] newArr = new byte[size];
            int len = size < buffer.Length ? size : buffer.Length;
            Buffer.BlockCopy (buffer, 0, newArr, 0, len);
            //  for (int i = 0; i < len; i++)
            //      newArr[i] = array[i];
            DebugUtils.UntrackAllocationObsolete(buffer);
            DebugUtils.TrackAllocationObsolete(newArr);
            buffer = newArr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            this.start  = 0; 
            this.end    = 0;
        }

        public void Set(in Bytes source)
        {
            int l = source.Len;
            EnsureCapacityAbs(l);
            this.start = 0;
            this.end = l;
            var dst = buffer;
            var src = source.buffer;
            for (int n = 0; n < Len; n++)
                dst[n] = src[n];
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
        
        public static bool IsIntegral(in ReadOnlySpan<byte> bytes) {
            int len = bytes.Length;
            if (len == 0)
                return false;
            byte c = bytes[0];
            if (len == 1) {
                return '0' <= c && c <= '9'; 
            }
            var begin = 0;
            if (c == '-') {
                c = bytes[1];
                begin++;
            }
            // no leading 0
            if (c < '1' || c > '9')
                return false;
            for (int i = begin + 1; i < len; i++) {
                c = bytes[i];
                if ('0' <= c && c <= '9')
                    continue;
                return false;
            }
            return true;
        }
        
        public const int    GuidLength     = 36; // 12345678-1234-1234-1234-123456789abc

        /// <summary>
        /// JSON DateTime are serialized as ISO 8601 UTC - suffix Z for Zulu. E.g<br/>
        /// <c>2023-07-09T09:27:24Z</c><br/>
        /// <c>2023-07-09T09:27:24.1Z</c><br/>
        /// <c>2023-07-09T09:27:24.123456Z</c><br/>
        /// See: https://en.wikipedia.org/wiki/ISO_8601
        /// </summary>
        public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFZ";
        public const int    DateTimeLength = 30;

        public static bool TryParseGuid(in ReadOnlySpan<byte> bytes, out Guid guid) {
            int len = bytes.Length;
            if (len != GuidLength) {
                guid = new Guid();
                return false;
            }
#if NETSTANDARD2_0
            unsafe {
                fixed (byte* ptr = bytes) {
                    // GetString(ReadOnlySpan<byte> bytes) not available in netstandard2.0
                    var str = Encoding.UTF8.GetString(ptr, bytes.Length);
                    return Guid.TryParse(str, out guid);
                }
            }
#else
            Span<char> span = stackalloc char[len];
            for (int n = 0; n < len; n++) {
                span[n] = (char)bytes[n];
            }
            return Guid.TryParse(span, out guid);
#endif
        }
        
        public void AppendGuid(in Guid guid) {
#if UNITY_5_3_OR_NEWER || NETSTANDARD2_0
            var str = guid.ToString();
            AppendString(str);
#else
            Span<char> span = stackalloc char[GuidLength];
            if (!guid.TryFormat(span, out int charsWritten))
                throw new InvalidOperationException("AppendGuid() failed");
            EnsureCapacity(charsWritten);
            var array   = buffer;
            var bufEnd  = end;
            for (int n = 0; n < charsWritten; n++) {
                array[bufEnd + n] = (byte)span[n];
            }
            end += charsWritten;
#endif
        }


        public bool IsEqual32 (string cs) {
            return IsEqualString (cs);
        }
        
        public bool IsEqual(in Bytes value)
        {
            /*   perf notes: 500_000_000 "abc" == "123"  SequenceEqual: 2.5 sec,  for loop: 1.3 sec
            var span  = new ReadOnlySpan<byte>(buffer, start, end - start);
            var other = new ReadOnlySpan<byte>(value.buffer, value.start, value.end - value.start);
            return span.SequenceEqual(other);
            */
            var leftStart   = start; 
            var rightStart  = value.start;
            int len         = end - leftStart;
            if (len != value.end - rightStart) {
                return false;
            }
            var leftBuffer  = buffer;
            var rightBuffer = value.buffer;
            
            for (int n = 0; n < len; n++) {
                if (leftBuffer[leftStart + n] != rightBuffer[rightStart + n]) {
                    return false;
                }
            }
            return true;
        }

        public bool IsEqualString (string str) {
            return Utf8Utils.IsStringEqualUtf8(str, in this);
        }

        /// <summary>deprecated: Use <see cref="Utf8String"/> instead </summary>
        public bool IsEqualArray(byte[] array) {
            int len = end - start;
            if (len != array.Length) {
                return false;
            }
            var buf         = buffer;
            var bufStart    = start;
            for (int n = 0; n < len; n++) {
                if (buf[bufStart + n] != array[n]) {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals (object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual()");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use BytesHash and its Equality comparer");
        }

        // JSON_BURST_TAG
        public Str32 ToStr32() {
            return AsString();
        }
        
        // JSON_BURST_TAG
        public Str128 ToStr128() {
            return AsString();
        }
        
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
            return Encoding.UTF8.GetString(data, pos, size);
        }
        
        public char[] GetChars (ref char[] dst, out int length)
        {
            int len             = end - start;
            int maxCharCount    = utf8.GetMaxCharCount(len);
            if (maxCharCount > dst.Length)
                dst = new char[maxCharCount];

            length = utf8.GetChars(buffer, start, len, dst, 0);
            return dst;
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
            int byteLen = utf8.GetBytes(str, 0, str.Length, buffer, start);
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
        
        public unsafe void AppendShortString(long lng, long lng2)
        {
            int len = ShortStringUtils.GetLength(lng2);
            if (end + len > buffer.Length) {
                DoubleSize(end + len);
            }
            Span<byte> bytes  = stackalloc byte[ShortStringUtils.ByteCount];
            fixed (byte*  bytesPtr  = &bytes[0]) {
                var bytesLongPtr    = (long*)bytesPtr;
                bytesLongPtr[0]     = lng;
                bytesLongPtr[1]     = lng2;
            }
            // var target  = new Span<byte>(buffer, end, buffer.Length - end);
            // var source  = bytes.Slice(0, len);
            // source.CopyTo(target);
            var buf     = buffer;
            var bufEnd  = end;
            for (int n = 0; n < len; n++) {
                buf[bufEnd + n] = bytes[n];
            }
            end = bufEnd + len;
        }

        public void Set (string val)
        {
            AppendStringUtf8(val);
        }

        // JSON_BURST_TAG
        public void AppendStr128 (string str) {
            AppendString(str);
        }
        
        // Note: Prefer using AppendStr32 (ref string str)
        // JSON_BURST_TAG
        public void AppendStr32 (string str) {
            AppendString(str);
        }

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
        
        public void AppendGuid (in Guid guid, Span<char> buf) {
#if UNITY_5_3_OR_NEWER || NETSTANDARD2_0
            AppendString(guid.ToString());
#else
            if (!guid.TryFormat(buf, out var len))
                throw new InvalidOperationException($"Guid.TryFormat failed: {guid}");
            EnsureCapacity(len);
            int thisEnd = end;
            for (int n = 0; n < len; n++) {
                buffer[thisEnd + n] = (byte)buf[n];
            }
            end += len;
#endif
        }
        
        public static bool TryParseDateTime(in ReadOnlySpan<byte> bytes, out DateTime dateTime) {
            int len = bytes.Length;
            if (len > DateTimeLength) {
                dateTime = new DateTime();
                return false;
            }
#if NETSTANDARD2_0
            unsafe {
                fixed (byte* ptr = bytes) {
                    // GetString(ReadOnlySpan<byte> bytes) not available in netstandard2.0
                    var str = Encoding.UTF8.GetString(ptr, bytes.Length);
                    var success = DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dateTime);
                    return success;
                }
            }
#else
            Span<char> span = stackalloc char[len];
            for (int n = 0; n < len; n++) {
                span[n] = (char)bytes[n];
            }
            var success = DateTime.TryParse(span, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dateTime);
            return success;
#endif
        }
        
        public void AppendDateTime (in DateTime dateTime, Span<char> buf) {
            var utc = dateTime.ToUniversalTime();
#if UNITY_5_3_OR_NEWER || NETSTANDARD2_0
            AppendString(utc.ToString(Bytes.DateTimeFormat));
#else
            if (!utc.TryFormat(buf, out var len, DateTimeFormat)) {
                throw new InvalidOperationException($"DateTime.TryFormat failed: {dateTime}");
            }
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
