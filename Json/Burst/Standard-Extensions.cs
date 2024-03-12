// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;   // required for NETSTANDARD2_0
using System.Globalization;

/*
 * File contains extension methods to maintain compatibility to netstandard2.0.
 * This enables compatibility for
 * - .NET Framework 4.6.2 or higher
 * - Unity with: Project Settings > Player > Configuration > Api Compatibility Level: .NET Framework (untested)
 *
 * See: https://github.com/friflo/Friflo.Json.Fliox/blob/main/docs/compatibility.md
 */

// ReSharper disable CheckNamespace
namespace System.Collections.Generic
{
    public static class Helper
    {
        public static HashSet<T> CreateHashSet<T>(int capacity) {
#if NET_2_0 || NETSTANDARD2_0
            return new HashSet<T>();
#else
            return new HashSet<T>(capacity);
#endif
        }
        
        public static HashSet<T> CreateHashSet<T>(int capacity, IEqualityComparer<T> comparer) {
#if NET_2_0 || NETSTANDARD2_0
            return new HashSet<T>(comparer);
#else
            return new HashSet<T>(capacity, comparer);
#endif
        }
        
        public static T First<T>(this HashSet<T> hashSet) {
            // ReSharper disable once GenericEnumeratorNotDisposed - HashSet<T>.Dispose() does nothing
            var enumerator = hashSet.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }
    }
}

namespace System.Text
{
    public static class StandardTextExtensions
    {
        public static StringBuilder AppendLF(this StringBuilder sb) {
            sb.Append('\n');
            return sb;
        }

        public static StringBuilder AppendLF(this StringBuilder sb, string value)
        {
            sb.Append(value);
            return sb.Append('\n');
        }
        
        public static unsafe StringBuilder Append(this StringBuilder stringBuilder, ReadOnlySpan<char> value) {
            fixed (char*  charPtr   = &value[0]) {
                return stringBuilder.Append(charPtr, value.Length);
            }
        }
        
#if NETSTANDARD2_0
        public static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars) {
            if (bytes.Length == 0) {
                return 0;
            }
            fixed (byte*  bytesPtr  = &bytes[0])
            fixed (char*  charPtr   = &chars[0]) {
                return encoding.GetChars(bytesPtr, bytes.Length, charPtr, chars.Length);
            }
        }
        
        public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes) {
            if (chars.Length == 0) {
                return 0;
            }
            fixed (byte*  bytesPtr  = &bytes[0])
            fixed (char*  charPtr   = &chars[0]) {
                return encoding.GetBytes(charPtr, chars.Length, bytesPtr, bytes.Length);
            }
        }
        
        public static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> chars) {
            if (chars.Length == 0) {
                return 0;
            }
            fixed (char*  charPtr   = &chars[0]) {
                return encoding.GetByteCount(charPtr, chars.Length);
            }
        }
#endif
    }
}

namespace System
{
    public static class MathExt
    {
        public static bool TryParseDouble(ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out double result) {
#if NETSTANDARD2_0
            var str = span.ToString();  // NETSTANDARD2_0_ALLOC
            return double.TryParse(str, style, provider, out result);
#else
            return double.TryParse(span, style, provider, out result);
#endif
        }
        
        public static bool TryParseFloat(ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out float result) {
#if NETSTANDARD2_0
            var str = span.ToString();  // NETSTANDARD2_0_ALLOC
            return float.TryParse(str, style, provider, out result);
#else
            return float.TryParse(span, style, provider, out result);
#endif
        }
        
        public static bool TryParseLong(ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out long result) {
#if NETSTANDARD2_0
            var str = span.ToString();  // NETSTANDARD2_0_ALLOC
            return long.TryParse(str, style, provider, out result);
#else
            return long.TryParse(span, style, provider, out result);
#endif
        }
        
        public static bool TryParseInt(ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out int result) {
#if NETSTANDARD2_0
            var str = span.ToString();  // NETSTANDARD2_0_ALLOC
            return int.TryParse(str, style, provider, out result);
#else
            return int.TryParse(span, style, provider, out result);
#endif
        }
        
        // --- NON_CLS
        internal static bool TryParseULong(ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out ulong result) {
#if NETSTANDARD2_0
            var str = span.ToString();  // NETSTANDARD2_0_ALLOC
            return ulong.TryParse(str, style, provider, out result);
#else
            return ulong.TryParse(span, style, provider, out result);
#endif
        }
        
        internal static bool TryParseUInt(ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out uint result) {
#if NETSTANDARD2_0
            var str = span.ToString();  // NETSTANDARD2_0_ALLOC
            return uint.TryParse(str, style, provider, out result);
#else
            return uint.TryParse(span, style, provider, out result);
#endif
        }
    }
}

#if NET_2_0 || NETSTANDARD2_0

namespace System.Collections.Generic
{
    public static class StandardCollectionExtensions
    {
        public static bool TryAdd<TKey,TValue>(this IDictionary<TKey,TValue> dictionary, TKey key, TValue value) {
            if (dictionary.ContainsKey(key))
                return false;
            
            dictionary.Add(key, value);
            return true;
        }
        
        public static bool Remove<TKey,TValue>(this IDictionary<TKey,TValue> dictionary, TKey key, out TValue value) {
            if (dictionary.TryGetValue(key, out value)) {
                dictionary.Remove(key);
                return true;
            }
            return false;
        }

        public static int EnsureCapacity<TKey,TValue>(this Dictionary<TKey,TValue> dictionary, int capacity) {
            return 0;
        }
        
        public static int EnsureCapacity<T>(this HashSet<T> hashSet, int capacity) {
            return 0;
        }
        
        public static bool TryPeek<T>(this Stack<T> stack, out T result) {
            if (stack.Count > 0) {
                result = stack.Peek();
                return true;
            }
            result = default;
            return false;
        }
        
        public static bool TryPop<T>(this Stack<T> stack, out T result) {
            if (stack.Count == 0) {
                result = default;
                return false;
            } 
            result = stack.Pop();
            return true;
        }
    }
}

namespace System.Collections.Concurrent
{
    public static class StandardConcurrentExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue) {
            while (queue.TryDequeue(out T _)) { }
        }
    }
}

namespace System.Linq
{
    public static class StandardLinqExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) {
            return new HashSet<T>(source, null);
            /*var hashSet = new HashSet<T>();
            foreach (var element in source) {
                hashSet.Add(element);
            }
            return hashSet;*/
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
            /*
            var hashSet = new HashSet<T>(comparer);
            foreach (var element in source) {
                hashSet.Add(element);
            }
            return hashSet;
            */
        }
    }
}

namespace System
{
    public static class StandardSystemExtensions
    {
        // don't use this method. Use T[] from ArraySegment<T>.Array directly to
        // - improve performance
        // - enable access to array index operator this[]
        public static void CopyTo<T>(this ArraySegment<T> segment, T[] destination, int destinationIndex) {
            throw new InvalidOperationException("don't use this method. Use T[] from ArraySegment<T>.Array directly");
        }
    }
}
#endif
