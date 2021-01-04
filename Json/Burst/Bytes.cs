// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Utils;

#if JSON_BURST
	using Str32 = Unity.Collections.FixedString32;
	using Str128 = Unity.Collections.FixedString128;
#else
	using Str32 = System.String;
	using Str128 = System.String;
#endif

namespace Friflo.Json.Burst
{
	public struct BytesConst {
		public static readonly int 	notHashed = 0;
	}
	
	public struct Bytes : IDisposable
	{

		public	int				hc; //		= notHashed;

		public	int				start;
		public	int				end;
		public	ByteList		buffer;
		
		public	int				Len => end - start;
		public	int				Start => start;
		public	int				End => end;


		private static readonly	int		upper2Lower = 'a' - 'A';  // 97 - 65 = 32

		public void InitBytes(int capacity) {
			if (!buffer.IsCreated())
				buffer = new ByteList(capacity);
		}

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
			buffer = new ByteList(capacity);
		}
		
		public Bytes (String str) {
			hc = 0;
			start = 0;
			end = 0;
			buffer = new ByteList(0);
			FromString(str);
		}
		
		public Bytes(ByteList array) {
			hc = 0;
			start = 0;
			end = array.Length;
			buffer = array;
		}

		public void SetDim (int start, int end)
		{
			this.start	= start; 
			this.end	= end;
			this.hc 	= BytesConst.notHashed;
		}		

		public void SetStart (int start)
		{
			this.start 	= start;
			this.hc 	= BytesConst.notHashed;
		}

		public void SetEnd (int end)
		{
			this.end 	= end;
			this.hc 	= BytesConst.notHashed;
		}
		
		public byte Get (int idx)
		{
			if (idx < 0 || idx >= Len)
				throw new IndexOutOfRangeException();
			return buffer.array[start + idx];
		}
	
		public void Clear()
		{
			this.start	= 0; 
			this.end	= 0;
			this.hc 	= BytesConst.notHashed;
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
				throw new FrifloException("Expected len >= 2. len: " + Len);
			ref var str = ref buffer.array;
			if (str[start] != c || str[end - 1] != c)
				throw new FrifloException("Expected char '" + c + "'");
			start++;
			end--;
			this.hc	= BytesConst.notHashed;
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
			this.hc	= BytesConst.notHashed;
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

		public bool StartsWith(Bytes cs)
		{
			if (this.Len < cs.Len)
				return false;
			ref var str = ref buffer.array;
			ref var str2 = ref cs.buffer.array;
			int		end = start + cs.Len;
			int		i2 = cs.start;
			for (int i = start; i < end; i++ )
			{
				if (str[i] != str2[i2++])
					return false;
			}
			return true;
		}

		public bool EndsWith(Bytes suffix)
		{
			if (this.Len < suffix.Len)
				return false;
			ref var	str		= ref buffer.array;
			ref var	str2	= ref suffix.buffer.array;
			int		end		= suffix.end;
			int		i		= this.end - suffix.Len;
			for (int i2 = suffix.start; i2 < end; i2++ )
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
			ref var 		str = ref buffer.array;
			int				end1 		= end - subStr.Len;
			int				start2 		= subStr.start; 
			int				end2 		= subStr.end;
			ref var			str2	= ref subStr.buffer.array;
			for (int n = start; n <= end1; n++)
			{
				int off	= n - start2;
				int i 	= start2;
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
			ref var	str = ref buffer.array;
			int		len2 = path.Len;
			int		minLen = Len <= len2 ? Len : len2;
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
		public bool IsEqual32(Str32 str2) {
			return IsEqual32(ref str2);
		}

		public bool IsEqual32(ref Str32 str2) {
			int len = this.Len;
			if (len != str2.Length)
				return false;
			ref var str = ref buffer.array;
			int end = start + len;
			int i2 = 0;
			// ref ByteList str2 = ref fix;
			for (int i = start; i < end; i++) {
				if (str[i] != str2[i2++])
					return false;
			}
			return true;
		}
#else
		public bool IsEqual32(ref String cs) {
			return IsEqualString(cs);
		}
		
		public bool IsEqual32(String cs) {
			return IsEqualString (cs);
		}
#endif

		public bool IsEqualBytes(Bytes cs)
		{
			int len = this.Len;
			if (len != cs.Len)
				return false;
			ref var str = ref buffer.array;
			int		end = start + len;
			int		i2 = cs.start;
			ref var str2 = ref cs.buffer.array;
			for (int i = start; i < end; i++ )
			{
				if (str[i] != str2[i2++])
					return false;
			}
			return true;
		}

		public bool IsEqualManagedArray (byte[] bytes, int start, int length)
		{
			int len = this.Len;
			if (len != length - start)
				return false;
			ref var str = ref buffer.array;
			int end = start + len;
			for (int i = start; i < end; i++ )
			{
				if (str[i] != bytes[start++])
					return false;
			}
			return true;
		}

		public bool IsEqualString (String str) {
			return Utf8Utils.IsStringEqualUtf8(str, ref this);
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

		public override bool Equals (Object obj)
		{
			Bytes cs = (Bytes)obj;
			return IsEqualBytes(cs);
		}

		internal int GetHash()
		{
			return hc;
		}

		public override int GetHashCode()
		{
			if (hc != BytesConst.notHashed)
				return hc;
			ref var str = ref buffer.array;
			int		h = Len;
	        // Rotate by 3 bits and XOR the new value.
			for (int i = start; i < end; i++)
				h = (h << 3) | (h >> (29)) ^ str[i];
			return hc = Math.Abs(h);
		}

		public int Split (char separator, params Bytes [] param)
		{
			int		count	= 0;
			ref var str	= ref buffer.array;
			int		start	= this.start;
			int		end		= this.end;
			for (int i = start; i < end; i++)
			{
				if (str[i] == separator)
				{
					if (count++ >= param. Length)
						continue;
					param[count-1].Set(ref this, start, i);
					start = i + 1;
				}
			}
			if (start < end && count++ < param. Length)
				param[count-1].Set(ref this, start, end);
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
	        sbyte[] sbyteData = (sbyte[]) (Array)data.array;
	        unsafe {
		        fixed (sbyte* sbytePtr = sbyteData)
			        return new String(sbytePtr, pos, size, Encoding.UTF8);
	        }
#endif
        }

		/*
		 * Must not by called from Burst. Burst cant handle managed types
		 */
		public void FromString(String str) {
			if (str == null) {
				start	= 0; 
				end	= -1;
				hc 	= BytesConst.notHashed;
				return;
			}
			int byteLen = Encoding.UTF8.GetByteCount(str);
			EnsureCapacity(byteLen);
#if JSON_BURST
			unsafe {
				byte* arrPtr = (byte*)Unity.Collections.LowLevel.Unsafe.NativeListUnsafeUtility.GetUnsafePtr(buffer.array);
				fixed (char* strPtr = str) {
					if (arrPtr != null)
						Encoding.UTF8.GetBytes(strPtr, str.Length, arrPtr, buffer.array.Length);
				}
			}
#else
			Encoding.UTF8.GetBytes(str, 0, str.Length, buffer.array, start);
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
			if (size <= buffer. Length)
				return;
			if (size < 2 * buffer. Length)
				size = 2 * buffer. Length;

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
		public void AppendFixed128 (Str128 str) {
			int strLen = str.Length;
			EnsureCapacity(Len + strLen);
			int curEnd = end;
			ref var buf = ref buffer.array;
			for (int n = 0; n < strLen; n++)
				buf[curEnd + n] = str[n];
			end += strLen;
			hc = BytesConst.notHashed;
        }

		// Note: Prefer using AppendFixed32 (ref Str32 str)
		public void AppendFixed32(Str32 str) {
			AppendFixed32(ref str);
		}

		public void AppendFixed32 (ref Str32 str) {
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
		public void AppendFixed128 (ref String str) {
			AppendString(str);
		}
		
		// Note: Prefer using AppendFixed32 (ref String str)
		public void AppendFixed32 (String str) {
			AppendString(str);
		}
		
		public void AppendFixed32 (ref String str) {
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
		
		public void AppendBytes(ref Bytes ca)
		{
			int		curEnd = end;
			int		caLen = ca.Len;
			int		newEnd = curEnd + caLen;
			ref var buf = ref buffer.array;
			EnsureCapacity(caLen);
			int		n2 = ca.Start;
			ref var str2 = ref ca.buffer.array;
			for (int n = curEnd; n < newEnd; n++)
				buf[n] = str2[n2++];
			end = newEnd;
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
			int 			strStart = src.Start;
			ref ByteList	srcBuf = ref src.buffer;
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
				AppendArray (ref srcBuf, strStart, src.End);
				hc = BytesConst.notHashed;
				return;
			}		
		}
	}
}
