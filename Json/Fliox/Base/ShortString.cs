// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper;
using static Friflo.Json.Burst.Utils.ShortStringUtils;
using static System.StringComparison;
using static System.Runtime.CompilerServices.MethodImplOptions;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    /// <summary>
    /// A struct optimized to store strings with the focus on minimizing memory allocations<br/>
    /// In contrast to <see cref="JsonKey"/> it supports to check strings with <see cref="StartsWith"/><br/>
    /// <br/>
    /// It is intended to be used for <i>stable and descriptive names</i> like:
    /// database, container, message, command, user, client and group names.<br/>
    /// <see cref="StartsWith"/> is optimized to enable filtering names by using a prefix - e.g. <c>"std.*"</c>
    /// used for authorization and subscriptions filters.
    /// </summary>
    /// <remarks>
    /// The main optimization goal is to avoid string allocations.<br/>
    /// Strings with length less than 16 characters are stored inside the struct to avoid heap allocations.<br/>
    /// A <see cref="ShortString"/> can also represents a <c>null</c> value. It can be tested using <see cref="IsNull"/>.<br/>
    /// Size of <see cref="ShortString"/> is 24 bytes<br/>
    /// </remarks>
    /// <seealso cref="JsonKey"/>
    public readonly struct ShortString
    {
        /// <summary>is not null in case a <see cref="ShortString"/> is represented by a <see cref="string"/> instance.</summary>
        internal    readonly    string  str;
        /// <summary>
        /// bytes[0..7] - lower 8 UTF-8 bytes of a short string
        /// </summary>
        internal    readonly    long    lng;
        /// <summary>
        /// bytes[0..6] - higher 7 UTF-8 bytes of a short string.<br/>
        /// byte [7]
        /// <list type="bullet">
        ///   <item>0:          <see cref="ShortString"/> represents a null string</item>
        ///   <item>greater 0:  short string length + 1</item>
        ///   <item>-128:       using a <see cref="string"/> instance</item>
        /// </list>
        /// </summary>
        internal    readonly    long    lng2; // higher 7 bytes for UTF-8 string + 1 byte length / NULL
        
        
        [MethodImpl(AggressiveInlining)] public     bool    IsNull()        => lng2 == ShortStringUtils.IsNull;
        [MethodImpl(AggressiveInlining)] internal   int     GetShortLength()=> (int)(lng2 >> ShiftLength) - 1;
        public override                             string  ToString()      => GetString();

        public static readonly  IEqualityComparer<ShortString>  Equality    = new ShortStringEqualityComparer();
        public static readonly  IComparer<ShortString>          Comparer    = new ShortStringComparer();

        public ShortString (string value)
        {
            if (value == null) {
                this = default;
                return;
            }
            StringToLongLong(value, out str, out lng, out lng2);
        }
        
        public ShortString (in Bytes bytes, string reuseStr)
            : this(bytes.AsSpan(), reuseStr)
        { }
        
        public ShortString (in ReadOnlySpan<byte> bytes, string reuseStr) {
            if (BytesToLongLong(bytes, out lng, out lng2)) {
                str     = null;
            } else {
                str     = GetString(bytes, reuseStr, out lng, out lng2);
            }
        }
        
        public ShortString (in JsonKey jsonKey)
        {
            var obj = jsonKey.keyObj;
            if (obj == null) {
                this = default;
                return;
            }
            if (obj == JsonKey.STRING_SHORT) {
                str     = null;
                lng     = jsonKey.lng;
                lng2    = jsonKey.lng2;
                return;
            }
            if (obj == JsonKey.LONG) {
                // TODO optimize avoid string creation
                var longStr = jsonKey.lng.ToString();
                StringToLongLong(longStr, out str, out lng, out lng2);
                return;
            }
            if (obj == JsonKey.GUID) {
                // TODO optimize avoid string creation
                var guid    = GuidUtils.LongLongToGuid(jsonKey.lng, jsonKey.lng2);
                var guidStr = guid.ToString();
                StringToLongLong(guidStr, out str, out lng, out lng2);
                return;
            }
            if (obj is string value) {
                str     = value;
                lng     = 0;
                lng2    = IsString;
                return;
            }
            throw new InvalidOperationException("unhanded case");
        }
        
        internal static string GetString(in ReadOnlySpan<byte> bytes, string reuseStr, out long lng, out long lng2) {
            lng             = 0;
            lng2            = IsString;
            int len         = bytes.Length;
            var maxCount    = Encoding.UTF8.GetMaxCharCount(len);
            Span<char> dest = stackalloc char[maxCount];
            int strLen      = Encoding.UTF8.GetChars(bytes, dest);
            var newSpan     = dest.Slice(0, strLen);
            var oldSpan     = reuseStr.AsSpan();
            if (newSpan.SequenceEqual(oldSpan)) {
                return reuseStr;
            }
            return newSpan.ToString();
        }
        
        internal const int MaxCharCount = 16; // Encoding.UTF8.GetMaxCharCount(15);
        
        public int Compare(in ShortString right)
        {
            // left = this
            if (right.str != null) {
                if (str != null) {
                    return string.CompareOrdinal(str, right.str);
                }
                if (lng2 == ShortStringUtils.IsNull) {
                    return -1;
                }
                Span<char> leftChars   = stackalloc char[MaxCharCount];
                var leftCount          = GetChars(lng, lng2, leftChars);
                
                ReadOnlySpan<char> leftReadOnly     = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char> rightReadOnly    = right.str.AsSpan();
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            }
            // case: right.str == null
            if (str != null) {
                if (right.lng2 == ShortStringUtils.IsNull) {
                    return +1;
                }
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = GetChars(right.lng, right.lng2, rightChars);
                
                ReadOnlySpan<char> leftReadOnly     = str.AsSpan();
                ReadOnlySpan<char> rightReadOnly    = rightChars.Slice(0, rightCount);
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            } else {
                // case: left and right are short strings
                if (lng2 == ShortStringUtils.IsNull) {
                    return right.lng2 == ShortStringUtils.IsNull ? 0 : -1;  
                }
                if (right.lng2 == ShortStringUtils.IsNull) {
                    return +1;
                }
                // TODO could perform comparison based on lng & lng2 similar to StringStartsWith()
                Span<char> leftChars    = stackalloc char[MaxCharCount];
                var leftCount           = GetChars(lng, lng2, leftChars);
                
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = GetChars(right.lng, right.lng2, rightChars);

                ReadOnlySpan<char>  leftReadOnly    = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char>  rightReadOnly   = rightChars. Slice(0, rightCount);
                return leftReadOnly.CompareTo(rightReadOnly, Ordinal);
            }
        }
        
        public bool StartsWith(in ShortString right)
        {
            // left = this
            if (IsNull())       throw new NullReferenceException();
            if (right.IsNull()) throw new ArgumentNullException(nameof(right));
            
            if (right.str != null) {
                if (str != null) {
                    return str.StartsWith(right.str, Ordinal);
                }
                // --- case: only left is short string
                Span<char> leftChars   = stackalloc char[MaxCharCount];
                var leftCount          = GetChars(lng, lng2, leftChars);
                
                ReadOnlySpan<char> leftReadOnly     = leftChars.Slice(0, leftCount);
                ReadOnlySpan<char> rightReadOnly    = right.str.AsSpan();
                return leftReadOnly.StartsWith(rightReadOnly, Ordinal);
            }
            // --- case: right.str == null  =>  only right is short string
            int rightLength = right.GetShortLength();
            if (rightLength == 0) {
                return true;    // early out for right: ""
            }
            if (str != null) {
                Span<char> rightChars   = stackalloc char[MaxCharCount];
                var rightCount          = GetChars(right.lng, right.lng2, rightChars);
                
                ReadOnlySpan<char> leftReadOnly     = str.AsSpan();
                ReadOnlySpan<char> rightReadOnly    = rightChars.Slice(0, rightCount);
                return leftReadOnly.StartsWith(rightReadOnly, Ordinal);
            }
            // --- case: left and right are short strings
            int leftLength = GetShortLength();
            if (rightLength > leftLength) {
                return false;
            }
            if (rightLength < 8) {
                long mask0  = 0x00ff_ffff_ffff_ffff >> (8 * (    7 - rightLength));
                return  (lng & mask0)  == (right.lng & mask0);
            } else {
                long mask8  = 0x00ff_ffff_ffff_ffff >> (8 * (8 + 7 - rightLength));
                return   lng           ==  right.lng &&
                        (lng2 & mask8) == (right.lng2 & mask8);
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
            return readOnlySpan.ToString();
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
            if (IsNull()) {
                sb.Append("null");
                return;
            }
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