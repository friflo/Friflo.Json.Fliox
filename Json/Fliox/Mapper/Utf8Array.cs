// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Linq;

namespace Friflo.Json.Fliox.Mapper
{
    public readonly struct Utf8Array {
        public  readonly    byte[]  array;
        
        public  override    string  ToString() => AsString();
        public              string  AsString() => array == null ? "null" : Encoding.UTF8.GetString(array, 0, array.Length);
        
        private static readonly Utf8Array Null = new Utf8Array("null");

        public Utf8Array(byte[] array) {
            this.array  = array;
        }
        
        /// <summary> Prefer using <see cref="Utf8Array(byte[])"/> </summary>
        public Utf8Array(string value) {
            array  = Encoding.UTF8.GetBytes(value);;
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
    }
}