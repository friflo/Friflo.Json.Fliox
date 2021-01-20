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

        public void Write(Object obj) { 
            StubType stubType = typeCache.GetType(obj.GetType());
            Var valueVar = new Var();
            valueVar.Obj = obj;
            WriteStart(stubType, ref valueVar);
        }
        
        public void Write<T>(T value) {
            StubType stubType = typeCache.GetType(typeof(T));
            Var valueVar = new Var();
            valueVar.Set (value, stubType.varType, stubType.isNullable);
            WriteStart(stubType, ref valueVar);
        }
        
        public void Write<T>(ref Var valueVar) { 
            StubType stubType = typeCache.GetType(typeof(T));
            WriteStart(stubType, ref valueVar);
        }
        
        private void WriteStart(StubType stubType, ref Var valueVar) {
            bytes.InitBytes(128);
            strBuf.InitBytes(128);
            format.InitTokenFormat();
            bytes.Clear();
            if (valueVar.IsNull)
                bytes.AppendBytes(ref @null);
            else
                stubType.map.Write(this, ref valueVar, stubType);
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