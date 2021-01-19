// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper
{
    public class JsonWriter : IDisposable
    {
        public readonly     TypeCache   typeCache;
        public              Bytes       bytes;
        public              ValueFormat format;
        private             Bytes       strBuf;

        public              Bytes       @null = new Bytes("null");
        public              Bytes       discriminator = new Bytes("\"$type\":\"");

        public          ref Bytes Output => ref bytes;

        public JsonWriter(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
        }
        
        public void Dispose() {
            @null.Dispose();
            discriminator.Dispose();
            format.Dispose();
            strBuf.Dispose();
            bytes.Dispose();
        }

        public static bool WriteNull(JsonWriter writer, ref Var slot) {
            if (slot.IsNull) {
                writer.bytes.AppendBytes(ref writer.@null);
                return true;
            }
            return false;
        } 

        public void Write(Object obj) { 
            bytes.InitBytes(128);
            strBuf.InitBytes(128);
            format.InitTokenFormat();
            StubType objType = typeCache.GetType(obj.GetType());
            bytes.Clear();
            Var slot = new Var();
            slot.Obj = obj;
            objType.map.Write(this, ref slot, objType);
        }
        
        public void Write<T>(ref Var value) { 
            bytes.InitBytes(128);
            strBuf.InitBytes(128);
            format.InitTokenFormat();
            StubType objType = typeCache.GetType(typeof(T));
            bytes.Clear();
            objType.map.Write(this, ref value, objType);
        }

        public void WriteKey(PropField field) {
            bytes.AppendChar('\"');
            field.AppendName(ref bytes);
            bytes.AppendString("\":");
        }

        public void WriteString(String str) {
            bytes.AppendChar('\"');
            strBuf.Clear();
            strBuf.FromString(str);
            JsonSerializer.AppendEscString(ref bytes, ref strBuf);
            bytes.AppendChar('\"');
        }

    }
}