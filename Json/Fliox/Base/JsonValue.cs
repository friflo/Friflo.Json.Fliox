// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Friflo.Json.Burst;

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertToAutoProperty
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    /// <summary>
    /// <see cref="JsonValue"/> instances contain <b>immutable</b> JSON values. <br/>
    /// A JSON value can be an object, an array, a string, a number, a boolean or null. <br/>
    /// To ensure immutability when creating a <see cref="JsonValue"/> with <see cref="JsonValue(byte[])"/>
    /// the passed array must not be changed subsequently.
    /// </summary>
    public readonly struct JsonValue {
        /// not public to prevent potential side effects by application code mutating array elements
        private  readonly   byte[]      array;              // can be null - default struct value
        public   readonly   int         start;              // > 0 if using an InstancePool
        private  readonly   int         count;              // can be 0    - default struct value
        
        /// <summary>Used internally where callers guarantee not mutating the array</summary>
        internal            byte[]      Array               => array ?? Null;                       // never null
        /// <summary><b>Attention!</b> Callers must guarantee not mutating the array. Otherwise undefined behavior.</summary>
        public              byte[]      MutableArray        => array ?? Null;                       // never null
        public              int         Count               => array != null ? count : Null.Length; // always > 0
        public              bool        IsNull()            => array == null;

        public   override   string      ToString()          => AsString();
        public              string      AsString()          => array == null ? "null" : Encoding.UTF8.GetString(array, start, count);
        
        /// <summary><b>Attention!</b> Callers must guarantee not mutating the array. Otherwise undefined behavior.</summary>
        public  ArraySegment<byte>      AsMutableArraySegment() => new ArraySegment<byte>   (Array, start, Count);
        public  ReadOnlyMemory<byte>    AsReadOnlyMemory()      => new ReadOnlyMemory<byte> (Array, start, Count);
        public  ByteArrayContent        AsByteArrayContent()    => new ByteArrayContent     (Array, start, Count); // todo may remove dependency System.Net.Http
        public  ReadOnlySpan<byte>      AsReadOnlySpan()        => new ReadOnlySpan<byte>   (Array, start, Count);
        
        /// <summary>Create a <see cref="byte"/> array copy</summary>
        public  byte[]                  AsByteArray() {
            var result = new byte[Count];
            Buffer.BlockCopy(Array, start, result, 0, Count);
            return result;
        }

        private static readonly byte[] Null =  Encoding.UTF8.GetBytes("null");
        
        public JsonValue(byte[] array, int start, int count) {
            this.array  = array ?? throw new ArgumentNullException(nameof(array));
            this.start  = start;
            this.count  = count;
        }
        
        public JsonValue(byte[] array, int count) {
            this.array  = array ?? throw new ArgumentNullException(nameof(array));
            this.count  = count;
            start       = 0;
        }
        
        public JsonValue(in JsonValue value, byte[] array, int offset) {
            this.array  = array ?? throw new ArgumentNullException(nameof(array));
            count       = value.count;
            start       = offset;
            Buffer.BlockCopy(value.Array, value.start, array, offset, value.count);
        }
        
        /// <summary>
        /// <b>Attention!</b> <see cref="Bytes"/> are commonly reused. <br/>
        /// So the created <see cref="JsonValue"/> need to be processed before the passed <paramref name="value"/> is reused.
        /// </summary>
        public JsonValue(in Bytes value) {
            array   = value.buffer ?? throw new ArgumentNullException(nameof(value), "value array == null");
            start   = value.start;
            count   = value.Len;
        }
        
        /// <summary>create a copy of the given <paramref name="value"/> </summary>
        public JsonValue(in JsonValue value) {
            if (!value.IsNull()) {
                count   = value.count;
                start   = 0;
                array   = new byte[count];
                Buffer.BlockCopy(value.array, value.start, array, 0, count);
                return;
            }
            this = default;
        }
        
        public JsonValue(byte[] array) {
            if (array == null) {
                this.array  = null;
                count       = Null.Length;
                start       = 0;
                return;
            }
            if (array.Length == Null.Length && array.SequenceEqual(Null)) {
                this.array  = null;
                count       = Null.Length;
                start       = 0;
                return;
            } 
            this.array  = array;
            count       = array.Length;
            start       = 0;
        }
        
        /// <summary> Prefer using <see cref="JsonValue(byte[])"/> </summary>
        public JsonValue(string value) {
            if (value == null) {
                array   = null;
                count   = Null.Length;
                start   = 0;
                return;
            }
            if (value == "null") {
                array   = null;
                count   = Null.Length;
                start   = 0;
                return;
            }
            array   = Encoding.UTF8.GetBytes(value);
            count   = array.Length;
            start   = 0;
        }
        
        public bool IsEqual (in JsonValue value) {
            var span1   = new Span<byte>(Array, start, count);
            var span2   = new Span<byte>(value.Array, value.start, value.count);
            return span1.SequenceEqual(span2);
        }
        
        public bool IsEqual (in Bytes value) {
            var val1 = new Span<byte>(array, start, count);
            var val2 = value.AsSpan();
            return val1.SequenceEqual(val2);
        }
        
        /// <summary>Use for testing only</summary>
        public bool IsEqualReference (in JsonValue value) {
            return ReferenceEquals(array, value.array);
        }
        
        public static async Task<JsonValue> ReadToEndAsync(Stream input, int length) {
            byte[]  buffer  = new byte[length];
            int     pos     = 0;
            int     read;
            while ((read = await input.ReadAsync(buffer, pos, length - pos).ConfigureAwait(false)) > 0) {
                pos += read;
            }
            if (length != pos) throw new InvalidOperationException($"Expect length {length}, was: {pos}");
            return new JsonValue(buffer);
        }
        
        /// <summary>
        /// Copy the given <paramref name="src"/> array to <paramref name="dst"/> <br/>
        /// The <paramref name="dst"/> array is reused if big enough. Otherwise a new array is created<br/>
        /// The <paramref name="src"/> array remains unchanged.
        /// </summary>
        public static void Copy(ref JsonValue dst, in JsonValue src) {
            if (src.IsNull()) {
                dst = default;
                return;
            }
            if (dst.start != 0) throw new InvalidOperationException("Expect start = 0");
            var dstArray    = dst.array;
            var count       = src.Count;
            if (dstArray == null || dstArray.Length < count) {
                dstArray = new byte[count];
            }
            Buffer.BlockCopy(src.array, src.start, dstArray, 0, count);
            dst = new JsonValue(dstArray, count);
        }
        
        /// <summary>
        /// Copy the bytes of the <see cref="JsonValue"/> to the given <paramref name="target"/> array.<br/>
        /// A new array is created if the Length given target array is too small.
        /// </summary>
        public void CopyTo(ref byte[] target) {
            if (Count > target.Length) {
                target = new byte[Count];
            }
            Buffer.BlockCopy(Array, start, target, 0, Count);
        }
    }
    
    public static class JsonValueExtensions {
    
        public static void AppendArray(this ref Bytes bytes, in JsonValue array) {
            AppendArray (ref bytes, array, array.start, array.Count);
        }
        
        public static void AppendArray(this ref Bytes bytes, in JsonValue array, int offset, int len) {
            if (bytes.end + len > bytes.buffer.Length) {
                bytes.DoubleSize(bytes.end + len);    
            }
            Buffer.BlockCopy(array.Array, offset, bytes.buffer, bytes.end, len);
            bytes.end += len;
        }
        
        public static async Task WriteAsync(this Stream stream, JsonValue array) {
            await stream.WriteAsync(array.Array, array.start, array.Count).ConfigureAwait(false);
        }
        
        public static void Write(this Stream stream, in JsonValue array) {
            stream.Write(array.Array, array.start, array.Count);
        }
        
        public static void InitParser(ref this Utf8JsonParser parser, in JsonValue json) {
            parser.InitParser (json.Array, json.start, json.Count);
        }
    }
}