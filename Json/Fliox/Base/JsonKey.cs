// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper;
using static System.Runtime.CompilerServices.MethodImplOptions;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    /// <summary>
    /// A struct optimized to store integers, strings or GUID's used for entity identifiers.<br/>
    /// E.g. <c>123, "123", "article" or "550e8400-e29b-11d4-a716-446655440000"</c><br/>
    /// Encoding of integers can by numbers e.g. <c>123</c> or strings e.g. <c>"123"</c>.<br/>
    /// <see cref="JsonKey"/> is used within the library for processing <i>arbitrary</i> entity identifiers.<br/>
    /// </summary>
    /// <remarks>
    /// The optimization goals are:<br/>
    /// - avoid heap allocations for the types mentioned above.<br/>
    /// - providing a performant lookup when used as key in a <see cref="Dictionary{TKey,TValue}"/> or <see cref="HashSet{T}"/><br/>
    /// <br/>
    /// Integers and GUID's are stored inside the struct. Strings with length less than 16 characters are also
    /// stored inside the struct to avoid heap allocations.<br/>
    /// A <see cref="JsonKey"/> can also represents a <c>null</c> value. It can be tested using <see cref="IsNull"/>.<br/>
    /// Size of <see cref="JsonKey"/> is 24 bytes<br/>
    /// </remarks>
    /// <seealso cref="ShortString"/>
    public readonly struct JsonKey
    {
        /// Store either on of the objects below
        /// <list type="bullet">
        ///     <item>           null               - a <i>null</i> reference</item>
        ///     <item><see cref="LONG"/>            - an <see cref="Int64"/> value</item>
        ///     <item><see cref="GUID"/>            - a <see cref="Guid"/> value</item>
        ///     <item><see cref="STRING_SHORT"/>    - a short string with length less than 16 bytes encoded a UTF-8</item>
        ///     <item><see cref="string"/> instance - an arbitrary string instance with length greater 15 bytes</item>
        /// </list>
        internal    readonly    object  keyObj;
        internal    readonly    long    lng;  // long  |  lower  64 bits for Guid  | lower  8 bytes for UTF-8 string
        internal    readonly    long    lng2; //          higher 64 bits for Guid  | higher 7 bytes for UTF-8 string + 1 byte length
        
                                         internal   Guid    Guid            => GuidUtils.LongLongToGuid(lng, lng2);
        [MethodImpl(AggressiveInlining)] public     bool    IsNull()        => keyObj == null;
        [MethodImpl(AggressiveInlining)] internal   int     GetShortLength()=> (int)(lng2 >> ShortStringUtils.ShiftLength) - 1;
                                         public     bool    IsLong()        => keyObj == LONG;
                                         public     bool    IsGuid()        => keyObj == GUID;
                                         public     bool    IsString()      => keyObj == STRING_SHORT || keyObj is string;
        
        public      override    string  ToString()  => GetString(); 

        public static readonly  IComparer<JsonKey>          Comparer    = new JsonKeyComparer();
        public static readonly  IEqualityComparer<JsonKey>  Equality    = new JsonKeyEqualityComparer();
        
        public static readonly  object  LONG            = new Type(JsonKeyType.LONG);
        public static readonly  object  GUID            = new Type(JsonKeyType.GUID);
        public static readonly  object  STRING_SHORT    = "STRING_SHORT"; // literal length must be <= 15 for IsEqual() 

        /// <summary>
        /// Calling this constructor should be the last option as it may force a string creation. <br/>
        /// Use alternative constructors if using a specific key type like <see cref="long"/> or <see cref="Guid"/>.
        /// </summary>
        public JsonKey (string value)
        {
            if (value == null) {
                this = default;
                return;
            }
            FormString(value, out keyObj, out lng, out lng2);
        }
        
        private static void FormString(string value, out object keyObj, out long lng, out long lng2) {
            if (long.TryParse(value, out long result)) {
                keyObj  = LONG;
                lng     = result;
                lng2    = 0;
                return;
            }
            var stringLength = value.Length;
            if (stringLength == Bytes.GuidLength && Guid.TryParse(value, out var guid)) {
                keyObj  = GUID;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            ShortStringUtils.StringToLongLong(value, out string str, out lng, out lng2);
            keyObj = str ?? STRING_SHORT;
        }
        
        public JsonKey (in Bytes bytes, in JsonKey oldKey)
            : this (bytes.AsSpan(), oldKey)
        { }
        
        public JsonKey (in ReadOnlySpan<byte> bytes, in JsonKey oldKey) {
            if (Bytes.IsIntegral(bytes)) {
                keyObj  = LONG;
                var error = new Bytes();
                lng     = ValueParser.ParseLong(bytes, ref error, out bool success);
                if (!success)
                    throw new InvalidOperationException("expect a valid integral type");
                lng2    = 0;
                return;
            }
            var bytesLen = bytes.Length;
            if (bytesLen == Bytes.GuidLength && Bytes.TryParseGuid(bytes, out var guid)) {
                keyObj  = GUID;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            if (ShortStringUtils.BytesToLongLong(bytes, out lng, out lng2)) {
                keyObj  = STRING_SHORT;
            } else {
                keyObj  = ShortString.GetString(bytes, (string)oldKey.keyObj, out lng, out lng2);
            }
        }
        
        public JsonKey (in ShortString value) {
            if (value.IsNull()) {
                this = default;
                return;
            }
            if (value.str != null) {
                FormString(value.str, out keyObj, out lng, out lng2);
                return;
            }
            Span<char> chars = stackalloc char[ShortString.MaxCharCount];
            ShortStringUtils.GetChars(value.lng, value.lng2, chars);
            int len             = value.GetShortLength();
            var readOnlySpan    = chars.Slice(0, len);
            if (MathExt.TryParseLong(readOnlySpan, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out lng)) {
                keyObj  = LONG;
                lng2    = 0;
                return;
            }
            keyObj  = STRING_SHORT;
            lng     = value.lng;
            lng2    = value.lng2;
        }

        public JsonKey (long value) {
            keyObj  = LONG;
            lng     = value;
            lng2    = 0;
        }
        
        public JsonKey (long? value) {
            keyObj  = value.HasValue ? LONG : null;
            lng     = value ?? 0;
            lng2    = 0;
        }
        
        public JsonKey (in Guid guid) {
            keyObj  = GUID;
            GuidUtils.GuidToLongLong(guid, out lng, out lng2);
        }
        
        public JsonKey (in Guid? guid) {
            var hasValue= guid.HasValue;
            keyObj  =  hasValue ? GUID : null;
            if (hasValue) {
                GuidUtils.GuidToLongLong(guid.Value, out lng, out lng2);
                return;
            }
            lng     = 0;
            lng2    = 0;
        }
        
        private static JsonKeyType GetKeyType(object obj) {
            if (obj == null)            return JsonKeyType.NULL;
            if (obj == LONG)            return JsonKeyType.LONG;
            if (obj == STRING_SHORT)    return JsonKeyType.STRING;
            if (obj == GUID)            return JsonKeyType.GUID;
            return JsonKeyType.STRING;
        }

        public int Compare(in JsonKey right) {
            // left = this
            var obj = keyObj;
            if (obj != right.keyObj) {
                if (obj is string) {
                    return new ShortString(this).Compare(new ShortString(right));
                }
                return GetKeyType(obj) - GetKeyType(right.keyObj);
            }
            if (obj == LONG) {
                long longDif = lng - right.lng;
                if (longDif < 0)
                    return -1;
                if (longDif > 0)
                    return +1;
                return 0;
            }
            if (obj == STRING_SHORT) {
                return new ShortString(this).Compare(new ShortString(right));
            }
            if (obj == GUID) {
                return Guid.CompareTo(right.Guid);
            }
            return 0; // same reference: : null or string instance
        }
        
        public bool IsEqual(in JsonKey other) {
            var obj = keyObj;
            if (obj != other.keyObj) {
                if (obj is string) {
                    // In case one obj field is STRING_SHORT and the other is a "long" string instance strings are
                    // not equal as "STRING_SHORT".Length == 12 and the other string instance has length > 15.
                    return (string)obj == (string)other.keyObj;
                }
                return false;
            }
            if (obj == LONG)            return lng == other.lng;
            if (obj == STRING_SHORT)    return lng == other.lng && lng2 == other.lng2;
            if (obj == GUID)            return lng == other.lng && lng2 == other.lng2;
            return true; // same reference: null or string instance
        }

        public int HashCode() {
            var obj = keyObj;
            if (obj == LONG)            return lng.GetHashCode();
            if (obj == STRING_SHORT)    return lng.GetHashCode() ^ lng2.GetHashCode();
            if (obj == GUID)            return lng.GetHashCode() ^ lng2.GetHashCode();
            if (obj is string)          return obj.GetHashCode();
            if (obj == null)            return 0;
            
            throw new InvalidOperationException("Invalid JsonKeyType"); 
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual() or JsonKey.Equality comparer");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use JsonKey.Equality comparer");
        }
        
        private string GetString() {
            if (keyObj == null) {
                return "null";
            }
            return $"'{AsString()}'";
        }

        /// <summary>Calling this method causes string instantiation. To avoid this use its <i>AppendTo</i> methods if possible.</summary> 
        public string AsString() {
            var obj = keyObj;
            if (obj == LONG)    return lng. ToString();
            if (obj == STRING_SHORT) {
                Span<char> chars    = stackalloc char[ShortString.MaxCharCount];
                var length          = ShortStringUtils.GetChars(lng, lng2, chars);
                var readOnlySpan    = chars.Slice(0, length);
                return readOnlySpan.ToString();
            }
            if (obj is string) {
                return (string)obj;
            }
            if (obj == GUID) return Guid.ToString();
            if (obj == null) return null;
            throw new InvalidOperationException("Invalid JsonKeyType"); 
        }
        
        public long AsLong() {
            if (keyObj == LONG)
                return lng;
            throw new InvalidOperationException($"cannot return JsonKey as long. {AsString()}");
        }
        
        public Guid AsGuid() {
            if (keyObj == GUID)
                return Guid;
            throw new InvalidOperationException($"cannot return JsonKey as Guid. {AsString()}");
        }

        public Guid? AsGuidNullable() {
            return keyObj == GUID ? Guid : default; 
        }
        
        public void AppendTo(ref Bytes dest, ref ValueFormat valueFormat) {
            var obj = keyObj;
            if (obj == LONG)            { valueFormat.AppendLong(ref dest, lng);    return; }
            if (obj == STRING_SHORT)    { dest.AppendShortString(lng, lng2);        return; }
            if (obj is string)          { dest.AppendString((string)obj);           return; }
            if (obj == GUID)            { dest.AppendGuid(Guid);                    return; }
            throw new InvalidOperationException("unexpected type in JsonKey.AppendTo()");
        }
        
        public JsonKeyEncoding GetEncoding() {
            var obj = keyObj;
            if (obj == null)            return JsonKeyEncoding.NULL;
            if (obj == LONG)            return JsonKeyEncoding.LONG;
            if (obj == STRING_SHORT)    return JsonKeyEncoding.STRING_SHORT;
            if (obj is string)          return JsonKeyEncoding.STRING;
            if (obj == GUID)            return JsonKeyEncoding.GUID;
            throw new InvalidOperationException("unexpected type in JsonKey.GetEncoding()");
        }
        
        public void ToBytes(ref Bytes dest) {
            dest.start  = 0;
            dest.end    = 0;
            var obj = keyObj;
            if (obj == STRING_SHORT)    { dest.AppendShortString(lng, lng2);        return; }
            if (obj == GUID)            { dest.AppendGuid(Guid);                    return; }
            throw new InvalidOperationException("unexpected type in JsonKey.ToBytes()");
        }
        
        public void AppendTo(StringBuilder sb) {
            var obj = keyObj;
            if (obj == LONG) {
                sb.Append(lng);
                return;
            }
            if (obj == STRING_SHORT) {
                Span<char> chars    = stackalloc char[ShortString.MaxCharCount];
                var len             = ShortStringUtils.GetChars(lng, lng2, chars);
                var readOnlyChars   = chars.Slice(0, len);
                sb.Append(readOnlyChars);
                return;
            }
            if (obj is string) {
                sb.Append(obj);
                return;
            }
            if (obj == GUID) {
#if UNITY_5_3_OR_NEWER || NETSTANDARD2_0
                var guidStr = Guid.ToString();
                sb.Append(guidStr);
#else
                Span<char> span = stackalloc char[Bytes.GuidLength];
                if (!Guid.TryFormat(span, out int charsWritten))
                    throw new InvalidOperationException("AppendGuid() failed");
                if (charsWritten != Bytes.GuidLength)
                    throw new InvalidOperationException($"Unexpected Guid length. Was: {charsWritten}");
                sb.Append(span);
#endif
                return;
            }
            throw new InvalidOperationException("unexpected type in JsonKey.AppendTo()");
        }
        
        private sealed class Type {
            private  readonly   JsonKeyType     type;
            public   override   string          ToString() => type.ToString();

            internal Type(JsonKeyType type) {
                this.type = type;    
            }
        }
    }
    
    // ReSharper disable InconsistentNaming
    public enum JsonKeyType {
        NULL    = 0,
        LONG    = 1,
        STRING  = 2,
        GUID    = 3,
    }
    
    public enum JsonKeyEncoding {
        NULL            = 0,
        /// <summary>Get value with <see cref="JsonKey.AsLong"/></summary>
        LONG            = 1,
        /// <summary>Get value with <see cref="JsonKey.AsString"/></summary>
        STRING          = 2,
        /// <summary>Get value with <see cref="JsonKey.AsString"/> or <see cref="JsonKey.ToBytes"/></summary>
        STRING_SHORT    = 3,
        /// <summary>Get value with <see cref="JsonKey.AsGuid"/> or <see cref="JsonKey.ToBytes"/></summary>
        GUID            = 4,
    }
}