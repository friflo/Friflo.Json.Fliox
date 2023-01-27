// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using static Friflo.Json.Burst.Utils.ShortStringUtils;
using static System.StringComparison;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    /// <summary>
    /// A struct optimized to store strings with the focus on minimizing memory allocations<br/>
    /// In contrast to <see cref="JsonKey"/> it supports to check strings with <see cref="StringStartsWith"/><br/>
    /// <br/>
    /// It is intended to be used for <i>stable and descriptive names</i> like database, container, message and command names.<br/>
    /// <see cref="StringStartsWith"/> is optimized to enable filtering names by using a prefix - e.g. <c>"std.*"</c>
    /// used for authorization and subscriptions filters.
    /// </summary>
    /// <remarks>
    /// The main optimization goal is to avoid allocations for the types mentioned above.<br/>
    /// Strings with length less than 15 characters are stored inside the struct to avoid heap allocations.<br/>
    /// A <see cref="ShortString"/> can also represents a <c>null</c> value. It can be tested using <see cref="IsNull"/>.<br/>
    /// </remarks>
    /// <seealso cref="JsonKey"/>
    public readonly struct ShortString
    {
        internal    readonly    string      str;
        internal    readonly    long        lng;  // lower  8 bytes for UTF-8 string
        internal    readonly    long        lng2; // higher 7 bytes for UTF-8 string + 1 byte length / NULL
        
        public                  bool        IsNull()    => str == null && lng2 == IsNULL;
        public      override    string      ToString()  => GetString(); 

        public static readonly  ShortStringEqualityComparer Equality    = new ShortStringEqualityComparer();
        public static readonly  ShortStringComparer         Comparer    = new ShortStringComparer();

        public ShortString (string value)
        {
            if (value == null) {
                this = default;
                return;
            }
            StringToLongLong(value, out str, out lng, out lng2);
        }
        
        public ShortString (ref Bytes bytes, in ShortString oldKey) {
            if (BytesToLongLong(bytes, out lng, out lng2)) {
                str     = null;
            } else {
                str     = GetString(bytes, oldKey.str);
                lng     = 0;
                lng2    = 0;
            }
        }
        
        public ShortString (in JsonKey jsonKey)
        {
            str     = jsonKey.str;
            lng     = jsonKey.lng;
            lng2    = jsonKey.lng2;
        }
        
        internal static string GetString(in Bytes bytes, string oldKey) {
            int len         = bytes.end - bytes.start;
            var src         = new ReadOnlySpan<byte>(bytes.buffer, bytes.start, len);
            var maxCount    = Encoding.UTF8.GetMaxCharCount(len);
            Span<char> dest = stackalloc char[maxCount];
            int strLen      = Encoding.UTF8.GetChars(src, dest);
            var newSpan     = dest.Slice(0, strLen);
            var oldSpan     = oldKey.AsSpan();
            if (newSpan.SequenceEqual(oldSpan)) {
                return oldKey;
            }
            return newSpan.ToString();
        }
        
        internal const int MaxCharCount = 16; // Encoding.UTF8.GetMaxCharCount(15);
        
        public static int StringCompare(in ShortString left, in ShortString right)
        {
            if (right.str != null) {
                if (left.str != null) {
                    return string.Compare(left.str, right.str, Ordinal);
                }
                if (left.lng2 == IsNULL) {
                    return -1;
                }
                Span<char> leftChars   = stackalloc char[MaxCharCount];
                var leftCount          = GetChars(left.lng, left.lng2, leftChars);
                
                ReadOnlySpan<char> leftReadOnly     = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char> rightReadOnly    = right.str.AsSpan();
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            }
            // case: right.str == null
            if (left.str != null) {
                if (right.lng2 == IsNULL) {
                    return +1;
                }
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = GetChars(right.lng, right.lng2, rightChars);
                
                ReadOnlySpan<char> leftReadOnly     = left.str.AsSpan();
                ReadOnlySpan<char> rightReadOnly    = rightChars.Slice(0, rightCount);
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            } else {
                // case: left and right are short strings
                if (left.lng2 == IsNULL) {
                    return right.lng2 == IsNULL ? 0 : -1;  
                }
                if (right.lng2 == IsNULL) {
                    return +1;
                }
                // TODO could perform comparison based on lng & lng2 similar to StringStartsWith()
                Span<char> leftChars    = stackalloc char[MaxCharCount];
                var leftCount           = GetChars(left.lng, left.lng2, leftChars);
                
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = GetChars(right.lng, right.lng2, rightChars);

                ReadOnlySpan<char>  leftReadOnly    = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char>  rightReadOnly   = rightChars. Slice(0, rightCount);
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            }
        }
        
        public static bool StringStartsWith(in ShortString left, in ShortString right)
        {
            if (left.IsNull())  throw new ArgumentException("expect left != null");
            if (right.IsNull()) throw new ArgumentException("expect right != null");
            
            if (right.str != null) {
                if (left.str != null) {
                    return left.str.StartsWith(right.str, Ordinal);
                }
                // --- case: only left is short string
                Span<char> leftChars   = stackalloc char[MaxCharCount];
                var leftCount          = GetChars(left.lng, left.lng2, leftChars);
                
                ReadOnlySpan<char> leftReadOnly     = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char> rightReadOnly    = right.str.AsSpan();
                return leftReadOnly.StartsWith(rightReadOnly, Ordinal);
            }
            // --- case: right.str == null  =>  only right is short string
            int rightLength = GetLength(right.lng2);
            if (rightLength == 0) {
                return true;    // early out for right: ""
            }
            if (left.str != null) {
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = GetChars(right.lng, right.lng2, rightChars);
                
                ReadOnlySpan<char> leftReadOnly     = left.str.AsSpan();
                ReadOnlySpan<char> rightReadOnly    = rightChars.Slice(0, rightCount);
                return leftReadOnly.StartsWith(rightReadOnly, Ordinal);
            }
            // --- case: left and right are short strings
            int leftLength = GetLength(left.lng2);
            if (rightLength > leftLength) {
                return false;
            }
            if (rightLength < 8) {
                long mask0  = 0x00ff_ffff_ffff_ffff >> (8 * (    7 - rightLength));
                return  (left.lng & mask0)  == (right.lng & mask0);
            } else {
                long mask8  = 0x00ff_ffff_ffff_ffff >> (8 * (8 + 7 - rightLength));
                return   left.lng           ==  right.lng &&
                        (left.lng2 & mask8) == (right.lng2 & mask8);
            }
        }
        
        public bool IsEqual(in ShortString other) {
            if (str == null && other.str == null) {
                return lng == other.lng && lng2 == other.lng2;
            }
            // In case one str field is null and the other is set strings are not equal as one value is a
            // short string (str == null) with length <= 15 and the other a string instance with length > 15.
            return str == other.str;
        }

        public int HashCode() {
            return str?.GetHashCode() ?? lng.GetHashCode() ^ lng2.GetHashCode();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual() or ShortString.Equality comparer");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use ShortString.Equality comparer");
        }
        
        private string GetString() {
            if (IsNull()) {
                return "null";
            }
            return $"'{AsString()}'";
        }

        /// <summary>Calling this method causes string instantiation. To avoid this use its <i>AppendTo</i> methods if possible.</summary> 
        public string AsString() {
            if (str != null) {
                return str;
            }
            if (IsNull()) {
                return null;
            }
            Span<char> chars    = stackalloc char[MaxCharCount];
            var length          = GetChars(lng, lng2, chars);
            var readOnlySpan    = chars.Slice(0, length);
            return new string(readOnlySpan);
        }
        
        public void AppendTo(ref Bytes dest) {
            if (IsNull()) throw new InvalidOperationException("ShortString is null");
            if (str != null) {
                dest.AppendString(str);
                return;
            }
            dest.AppendShortString(lng, lng2);
        }
        
        public void AppendTo(StringBuilder sb) {
            if (IsNull()) throw new InvalidOperationException("ShortString is null");
            if (str != null) {
                sb.Append(str);
                return;
            }
            Span<char> chars    = stackalloc char[MaxCharCount];
            var len             = GetChars(lng, lng2, chars);
            var readOnlyChars   = chars.Slice(0, len);
            sb.Append(readOnlyChars);
        }
    }
}