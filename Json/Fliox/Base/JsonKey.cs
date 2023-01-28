// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    /// <summary>
    /// A struct optimized to store strings representing integers, strings or GUID's<br/>
    /// E.g. <c>"12345", "article" or "550e8400-e29b-11d4-a716-446655440000"</c><br/>
    /// It is intended to be used for <i>arbitrary</i> identifiers like entity, user or client id's.<br/>
    /// </summary>
    /// <remarks>
    /// The optimization goals are:<br/>
    /// - avoid heap allocations for the types mentioned above.<br/>
    /// - providing a performant lookup when used as key in a <see cref="Dictionary{TKey,TValue}"/> or <see cref="HashSet{T}"/><br/>
    /// <br/>
    /// Integers and GUID's are stored inside the struct. Strings with length less than 15 characters are also
    /// stored inside the struct to avoid heap allocations.<br/>
    /// A <see cref="JsonKey"/> can also represents a <c>null</c> value. It can be tested using <see cref="IsNull"/>.<br/>
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
        internal    readonly    object  obj;
        internal    readonly    long    lng;  // long  |  lower  64 bits for Guid  | lower  8 bytes for UTF-8 string
        internal    readonly    long    lng2; //          higher 64 bits for Guid  | higher 7 bytes for UTF-8 string + 1 byte length
        
        public                  bool    IsNull()    => obj == null;
        internal                Guid    Guid        => GuidUtils.LongLongToGuid(lng, lng2);
        public      override    string  ToString()  => GetString(); 

        public static readonly  JsonKeyComparer         Comparer    = new JsonKeyComparer();
        public static readonly  JsonKeyEqualityComparer Equality    = new JsonKeyEqualityComparer();
        
        public static readonly  object  LONG            = new Type(JsonKeyType.LONG);
        public static readonly  object  GUID            = new Type(JsonKeyType.GUID);
        public static readonly  object  STRING_SHORT    = new string("STRING_SHORT"); // literal length must be <= 15 for IsEqual() 

        /// <summary>
        /// Calling this constructor should be the last option as it may force a string creation. <br/>
        /// Use alternative constructors if using a specific key type like <see cref="long"/> or <see cref="Guid"/>.
        /// </summary>
        public JsonKey (string value)
        {
            if (value == null) {
                obj     = null;
                lng     = 0;
                lng2    = 0;
                return;
            }
            if (long.TryParse(value, out long result)) {
                obj     = LONG;
                lng     = result;
                lng2    = 0;
                return;
            }
            var stringLength = value.Length;
            if (stringLength == Bytes.GuidLength && Guid.TryParse(value, out var guid)) {
                obj     = GUID;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            ShortStringUtils.StringToLongLong(value, out string str, out lng, out lng2);
            obj = str ?? STRING_SHORT;
        }
        
        public JsonKey (ref Bytes bytes, ref ValueParser valueParser, in JsonKey oldKey) {
            if (bytes.IsIntegral()) {
                obj     = LONG;
                var error = new Bytes();
                lng     = valueParser.ParseLong(ref bytes, ref error, out bool success);
                if (!success)
                    throw new InvalidOperationException("expect a valid integral type");
                lng2    = 0;
                return;
            }
            var bytesLen = bytes.end - bytes.start;
            if (bytesLen == Bytes.GuidLength && bytes.TryParseGuid(out var guid, out _)) { // out value not null in Unity. Otherwise null
                obj     = GUID;
                GuidUtils.GuidToLongLong(guid, out lng, out lng2);
                return;
            }
            if (ShortStringUtils.BytesToLongLong(bytes, out lng, out lng2)) {
                obj     = STRING_SHORT;
            } else {
                obj     = ShortString.GetString(bytes, (string)oldKey.obj, out lng, out lng2);
            }
        }

        public JsonKey (long value) {
            obj     = LONG;
            lng     = value;
            lng2    = 0;
        }
        
        public JsonKey (long? value) {
            obj     = value.HasValue ? LONG : null;
            lng     = value ?? 0;
            lng2    = 0;
        }
        
        public JsonKey (in Guid guid) {
            obj     = GUID;
            GuidUtils.GuidToLongLong(guid, out lng, out lng2);
        }
        
        public JsonKey (in Guid? guid) {
            var hasValue= guid.HasValue;
            obj     =  hasValue ? GUID : null;
            if (hasValue) {
                GuidUtils.GuidToLongLong(guid.Value, out lng, out lng2);
                return;
            }
            lng     = 0;
            lng2    = 0;
        }
        
        private static JsonKeyType GetKeyType(object obj) {
            var thisObj = obj;
            if (thisObj == null)            return JsonKeyType.NULL;
            if (thisObj == LONG)            return JsonKeyType.LONG;
            if (thisObj == STRING_SHORT)    return JsonKeyType.STRING;
            if (thisObj == GUID)            return JsonKeyType.GUID;
            return JsonKeyType.STRING;
        }

        public int Compare(in JsonKey right) {
            // left = this
            var thisObj = obj;
            if (thisObj != right.obj) {
                if (thisObj is string) {
                    return new ShortString(this).Compare(new ShortString(right));
                }
                return GetKeyType(thisObj) - GetKeyType(right.obj);
            }
            if (thisObj == LONG) {
                long longDif = lng - right.lng;
                if (longDif < 0)
                    return -1;
                if (longDif > 0)
                    return +1;
                return 0;
            }
            if (thisObj == STRING_SHORT) {
                return new ShortString(this).Compare(new ShortString(right));
            }
            if (thisObj == GUID) {
                return Guid.CompareTo(right.Guid);
            }
            return 0; // same reference: : null or string instance
        }
        
        public bool IsEqual(in JsonKey other) {
            var thisObj = obj;
            if (thisObj != other.obj) {
                if (thisObj is string) {
                    // In case one obj field is STRING_SHORT and the other is a "long" string instance strings are
                    // not equal as "STRING_SHORT".Length == 12 and the other string instance has length > 15.
                    return (string)thisObj == (string)other.obj;
                }
                return false;
            }
            if (thisObj == LONG)            return lng == other.lng;
            if (thisObj == STRING_SHORT)    return lng == other.lng && lng2 == other.lng2;
            if (thisObj == GUID)            return lng == other.lng && lng2 == other.lng2;
            return true; // same reference: null or string instance
        }

        public int HashCode() {
            var thisObj = obj;
            if (thisObj == LONG)            return lng.GetHashCode();
            if (thisObj == STRING_SHORT)    return lng.GetHashCode() ^ lng2.GetHashCode();
            if (thisObj == GUID)            return lng.GetHashCode() ^ lng2.GetHashCode();
            if (thisObj is string)          return thisObj.GetHashCode();
            if (thisObj == null)            return 0;
            
            throw new InvalidOperationException("Invalid JsonKeyType"); 
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual() or JsonKey.Equality comparer");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use JsonKey.Equality comparer");
        }
        
        private string GetString() {
            if (obj == null) {
                return "null";
            }
            return $"'{AsString()}'";
        }

        /// <summary>Calling this method causes string instantiation. To avoid this use its <i>AppendTo</i> methods if possible.</summary> 
        public string AsString() {
            var thisObj = obj;
            if (thisObj == LONG)    return lng. ToString();
            if (thisObj == STRING_SHORT) {
                Span<char> chars    = stackalloc char[ShortString.MaxCharCount];
                var length          = ShortStringUtils.GetChars(lng, lng2, chars);
                var readOnlySpan    = chars.Slice(0, length);
                return new string(readOnlySpan);
            }
            if (thisObj is string) {
                return (string)thisObj;
            }
            if (thisObj == GUID) return Guid.ToString();
            if (thisObj == null) return null;
            throw new InvalidOperationException("Invalid JsonKeyType"); 
        }
        
        public long AsLong() {
            if (obj == LONG)
                return lng;
            throw new InvalidOperationException($"cannot return JsonKey as long. {AsString()}");
        }
        
        public Guid AsGuid() => Guid;
        
        public Guid? AsGuidNullable() {
            return obj == GUID ? Guid : default; 
        }
        
        public void AppendTo(ref Bytes dest, ref ValueFormat valueFormat) {
            var thisObj = obj;
            if (thisObj == LONG)            { valueFormat.AppendLong(ref dest, lng);    return; }
            if (thisObj == STRING_SHORT)    { dest.AppendShortString(lng, lng2);        return; }
            if (thisObj is string)          { dest.AppendString((string)thisObj);       return; }
            if (thisObj == GUID)            { dest.AppendGuid(Guid);                    return; }
            throw new InvalidOperationException("unexpected type in JsonKey.AppendTo()");
        }
        
        public void AppendTo(StringBuilder sb) {
            var thisObj = obj;
            if (thisObj == LONG) {
                sb.Append(lng);
                return;
            }
            if (thisObj == STRING_SHORT) {
                Span<char> chars    = stackalloc char[ShortString.MaxCharCount];
                var len             = ShortStringUtils.GetChars(lng, lng2, chars);
                var readOnlyChars   = chars.Slice(0, len);
                sb.Append(readOnlyChars);
                return;
            }
            if (thisObj is string) {
                sb.Append(thisObj);
                return;
            }
            if (thisObj == GUID) {
#if UNITY_5_3_OR_NEWER
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
}