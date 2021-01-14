// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed
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

        public void InitWriter() {
            bytes.Clear();
        }

        public void Write(Object obj) {
            bytes.InitBytes(128);
            strBuf.InitBytes(128);
            format.InitTokenFormat();
            NativeType objType = typeCache.GetType(obj.GetType());
            objType.codec.Write(this, obj, objType);
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