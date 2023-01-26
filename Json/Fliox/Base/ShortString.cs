// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;
using static System.StringComparison;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    /// <summary>
    /// A struct optimized to store strings with the focus on minimizing memory allocations<br/>
    /// In contrast to <see cref="JsonKey"/> it supports to check strings with <see cref="StringStartsWith"/><br/>
    /// <br/> 
    /// It is intended to be used for <i>descriptive names</i> like database, container, message and command names.<br/>
    /// <see cref="StringStartsWith"/> is optimized to enable filtering names by using a prefix - e.g. <c>'std.*'</c>
    /// used for authorization and subscriptions filters.  
    /// </summary>
    /// <remarks>
    /// The main optimization goal is to avoid allocations for the types mentioned above.<br/>
    /// Strings with length less than 15 characters are stored inside the struct to avoid heap allocations.<br/>
    /// A <see cref="ShortString"/> can also represents a <c>null</c> value. It can be tested using <see cref="IsNull"/>.<br/>
    /// </remarks>
    /// /// <seealso cref="JsonKey"/>
    public readonly struct ShortString
    {
        internal    readonly    bool        notNull;
        internal    readonly    string      str;
        internal    readonly    long        lng;  // long  |  lower  64 bits for Guid  | lower  8 bytes for UTF-8 string
        [Browse(Never)]
        internal    readonly    long        lng2; //          higher 64 bits for Guid  | higher 7 bytes for UTF-8 string + 1 byte length
        
        public      override    string      ToString()  => GetString(); 

        public static readonly  ShortStringEqualityComparer  Equality    = new ShortStringEqualityComparer();

        /// <summary>
        /// Calling this constructor should be the last option as it may force a string creation. <br/>
        /// Use alternative constructors if using a specific key type like <see cref="long"/> or <see cref="Guid"/>.
        /// </summary>
        public ShortString (string value)
        {
            if (value == null) {
                this = default;
                return;
            }
            notNull    = true;
            ShortStringUtils.StringToLongLong(value, out str, out lng, out lng2);
        }
        
        public ShortString (ref Bytes bytes, in ShortString oldKey) {
            notNull    = true;
            if (ShortStringUtils.BytesToLongLong(bytes, out lng, out lng2)) {
                str     = null;
            } else {
                str     = GetString(bytes, oldKey.str);
                lng     = 0;
                lng2    = 0;
            }
        }
        
        public ShortString (in JsonKey jsonKey)
        {
            notNull = true;
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
        
        public bool IsNull() => !notNull;
        
        internal const int MaxCharCount = 16; // Encoding.UTF8.GetMaxCharCount(15);
        
        public static int StringCompare(in ShortString left, in ShortString right)
        {
            if (!left.notNull)  throw new ArgumentException("expect left != null");
            if (!right.notNull) throw new ArgumentException("expect right != nul");
            
            if (right.str != null) {
                if (left.str != null) {
                    return string.Compare(left.str, right.str, Ordinal);
                }
                Span<char> leftChars   = stackalloc char[MaxCharCount];
                var leftCount          = ShortStringUtils.GetChars(left.lng, left.lng2, leftChars);
                
                ReadOnlySpan<char> leftReadOnly     = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char> rightReadOnly    = right.str.AsSpan();
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            }
            // case: right.str == null
            if (left.str != null) {
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = ShortStringUtils.GetChars(right.lng, right.lng2, rightChars);
                
                ReadOnlySpan<char> leftReadOnly     = left.str.AsSpan();
                ReadOnlySpan<char> rightReadOnly    = rightChars.Slice(0, rightCount);
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            } else {
                Span<char> leftChars    = stackalloc char[MaxCharCount];
                var leftCount           = ShortStringUtils.GetChars(left.lng, left.lng2, leftChars);
                
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = ShortStringUtils.GetChars(right.lng, right.lng2, rightChars);

                ReadOnlySpan<char>  leftReadOnly    = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char>  rightReadOnly   = rightChars. Slice(0, rightCount);
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            }
        }
        
        public static bool StringStartsWith(in ShortString left, in ShortString right)
        {
            if (!left.notNull) throw new ArgumentException("expect left != null");
            if (!right.notNull) throw new ArgumentException("expect right != nul");
            
            if (right.str != null) {
                if (left.str != null) {
                    return left.str.StartsWith(right.str, Ordinal);
                }
                // --- case: only left is short string
                Span<char> leftChars   = stackalloc char[MaxCharCount];
                var leftCount          = ShortStringUtils.GetChars(left.lng, left.lng2, leftChars);
                
                ReadOnlySpan<char> leftReadOnly     = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char> rightReadOnly    = right.str.AsSpan();
                return leftReadOnly.StartsWith(rightReadOnly, Ordinal);
            }
            // --- case: right.str == null  =>  only right is short string
            int rightLength = (int)(right.lng2 >> ShortStringUtils.ShiftLength);
            if (rightLength == 0) {
                return true;    // early out for right: ""
            }
            if (left.str != null) {
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = ShortStringUtils.GetChars(right.lng, right.lng2, rightChars);
                
                ReadOnlySpan<char> leftReadOnly     = left.str.AsSpan();
                ReadOnlySpan<char> rightReadOnly    = rightChars.Slice(0, rightCount);
                return leftReadOnly.StartsWith(rightReadOnly, Ordinal);
            }
            // --- case: left and right are short strings
            int leftLength = (int)(left.lng2 >> ShortStringUtils.ShiftLength);
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
            if (notNull != other.notNull)
                return false;
            
            if (notNull) {
                if (str == null && other.str == null) {
                    return lng == other.lng && lng2 == other.lng2;
                }
                // In case one str field is null and the other is set strings are not equal as one value is a
                // short string (str == null) with length <= 15 and the other a string instance with length > 15.
                return str == other.str;
            }
            return true;
        }

        public int HashCode() {
            if (notNull) {
                return str?.GetHashCode() ?? lng.GetHashCode() ^ lng2.GetHashCode();
            }
            return 0;
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual() or ShortString.Equality comparer");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use ShortString.Equality comparer");
        }
        
        private string GetString() {
            if (!notNull) {
                return "null";
            }
            return $"'{AsString()}'";
        }

        /// <summary>Calling this method causes string instantiation. To avoid this use its <i>AppendTo</i> methods if possible.</summary> 
        public string AsString() {
            if (notNull) {
                if (str != null) {
                    return str;
                }
                Span<char> chars    = stackalloc char[MaxCharCount];
                var length          = ShortStringUtils.GetChars(lng, lng2, chars);
                var readOnlySpan    = chars.Slice(0, length);
                return new string(readOnlySpan);
            }
            return null;
        }
        
        public void AppendTo(ref Bytes dest) {
            if (notNull) {
                if (str != null) {
                    dest.AppendString(str);
                    return;
                }
                dest.AppendShortString(lng, lng2);
                return;
            }
            throw new InvalidOperationException($"ShortString is null");
        }
        
        public void AppendTo(StringBuilder sb) {
            if (notNull) {
                if (str != null) {
                    sb.Append(str);
                    return;
                }
                Span<char> chars    = stackalloc char[MaxCharCount];
                var len             = ShortStringUtils.GetChars(lng, lng2, chars);
                var readOnlyChars   = chars.Slice(0, len);
                sb.Append(readOnlyChars);
                return;
            }
            throw new InvalidOperationException($"ShortString is null");
        }
    }
}