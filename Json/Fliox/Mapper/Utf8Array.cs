// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Mapper
{
    public readonly struct Utf8Array {
        // array is internal to prevent potential side effects by application code mutating array elements
        internal readonly   byte[]  array;
        
        public              int     Length      => array.Length;
        public   override   string  ToString()  => AsString();
        public              string  AsString()  => array == null ? "null" : Encoding.UTF8.GetString(array, 0, array.Length);
        
        private static readonly Utf8Array Null = new Utf8Array("null");

        public Utf8Array(byte[] array) {
            this.array  = array;
        }
        
        /// <summary> Prefer using <see cref="Utf8Array(byte[])"/> </summary>
        public Utf8Array(string value) {
            array  = Encoding.UTF8.GetBytes(value);
        }
        
        public bool IsNull() {
            if (array == null)
                return true;
            return IsEqual(Null);
        }

        public bool IsEqual (Utf8Array value) {
            if (array == null) {
                return value.array == null;
            }
            if (value.array == null)
                return false;
            return array.SequenceEqual(value.array);
        }
        
        /// <summary>Use for testing only</summary>
        public bool IsEqualReference (Utf8Array value) {
            return ReferenceEquals(array, value.array);
        }
    }
    
    public static class Utf8ArrayExtensions {
    
        public static void AppendArray(this ref Bytes bytes, Utf8Array array) {
            AppendArray (ref bytes, array, 0, array.array.Length);
        }
        
        public static void AppendArray(this ref Bytes bytes, Utf8Array array, int offset, int len) {
            bytes.EnsureCapacity(len);
            int pos     = bytes.end;
            int arrEnd  = offset + len;
            ref var buf = ref bytes.buffer.array;
            var src = array.array;
            for (int n = offset; n < arrEnd; n++)
                buf[pos++] = src[n];
            bytes.end += len;
            bytes.hc = BytesConst.notHashed;
        }
        
        public static async Task WriteAsync(this Stream stream, Utf8Array array, int offset, int count) {
            await stream.WriteAsync(array.array, offset, count);
        }
        
        public static void Write(this Stream stream, Utf8Array array, int offset, int count) {
            stream.Write(array.array, offset, count);
        }
    }
}