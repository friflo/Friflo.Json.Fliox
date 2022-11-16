// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Friflo.Json.Burst;

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
        // array & Array are not public to prevent potential side effects by application code mutating array elements
        private  readonly   byte[]  array;                                          // can be null
        internal            byte[]  Array               => array ?? Null;           // never null
        
        public              int     Length      => array?.Length ?? Null.Length;    // always > 0
        public   override   string  ToString()  => AsString();
        public              string  AsString()  => array == null ? "null" : Encoding.UTF8.GetString(array, 0, array.Length);
        
        public  ArraySegment<byte>      AsArraySegment()        => new ArraySegment<byte>(Array, 0, Array.Length);
#if !UNITY_5_3_OR_NEWER
//      public  ReadOnlyMemory<byte>    AsReadOnlyMemory()      => new ReadOnlyMemory<byte>(Array, 0, Array.Length);
#endif
        public  ByteArrayContent        AsByteArrayContent()    => new ByteArrayContent(Array); // todo hm. dependency System.Net.Http 
        public  byte[]                  AsByteArray()           => array.ToArray();
        
        private static readonly byte[] Null =  {(byte)'n', (byte)'u', (byte)'l', (byte)'l'};

        public JsonValue(byte[] array) {
            if (array == null) {
                this.array = null;
                return;
            }
            if (array.Length == Null.Length && array.SequenceEqual(Null)) {
                this.array = null;
                return;
            } 
            this.array  = array;
        }
        
        /// <summary> Prefer using <see cref="JsonValue(byte[])"/> </summary>
        public JsonValue(string value) {
            if (value == null) {
                array = null;
                return;
            }
            if (value == "null") {
                array = null;
                return;
            } 
            array  = Encoding.UTF8.GetBytes(value);
        }
        
        public bool IsNull() {
            return array == null;
        }

        public bool IsEqual (in JsonValue value) {
            return Array.SequenceEqual(value.Array);
        }
        
        /// <summary>Use for testing only</summary>
        public bool IsEqualReference (in JsonValue value) {
            return ReferenceEquals(array, value.array);
        }
        
        public static async Task<JsonValue> ReadToEndAsync(Stream input) {
            byte[] buffer = new byte[16 * 1024];                // todo performance -> cache
            using (MemoryStream ms = new MemoryStream()) {      // todo performance -> cache
                int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0) {
                    ms.Write(buffer, 0, read);
                }
                var array = ms.ToArray(); 
                return new JsonValue(array);
            }
        }
    }
    
    public static class JsonValueExtensions {
    
        public static void AppendArray(this ref Bytes bytes, in JsonValue array) {
            AppendArray (ref bytes, array, 0, array.Array.Length);
        }
        
        public static void AppendArray(this ref Bytes bytes, in JsonValue array, int offset, int len) {
            bytes.EnsureCapacity(len);
            int pos     = bytes.end;
            int arrEnd  = offset + len;
            ref var buf = ref bytes.buffer.array;
            var src = array.Array;
            for (int n = offset; n < arrEnd; n++)
                buf[pos++] = src[n];
            bytes.end += len;
            bytes.hc = BytesConst.notHashed;
        }
        
        public static async Task WriteAsync(this Stream stream, JsonValue array, int offset, int count) {
            await stream.WriteAsync(array.Array, offset, count).ConfigureAwait(false);
        }
        
        public static void Write(this Stream stream, in JsonValue array, int offset, int count) {
            stream.Write(array.Array, offset, count);
        }
    }
}