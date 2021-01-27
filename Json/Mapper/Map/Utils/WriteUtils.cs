// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class WriteUtils
    {
        public static void WriteKey(JsonWriter writer, PropField field) {
            ref Bytes bytes = ref writer.bytes;
            bytes.AppendChar('\"');
            field.AppendName(ref bytes);
            bytes.AppendString("\":");
        }

        public static void WriteString(JsonWriter writer, String str) {
            ref Bytes bytes = ref writer.bytes;
            ref Bytes strBuf = ref writer.strBuf;
            bytes.AppendChar('\"');
            strBuf.Clear();
            strBuf.FromString(str);
            JsonSerializer.AppendEscString(ref bytes, ref strBuf);
            bytes.AppendChar('\"');
        }

        public static void AppendNull(JsonWriter writer) {
            writer.bytes.AppendBytes(ref writer.@null);
        }
        
        public static int IncLevel(JsonWriter writer) {
            if (writer.level++ < writer.maxDepth)
                return writer.level;
            throw new InvalidOperationException($"JsonParser: maxDepth exceeded. maxDepth: {writer.maxDepth}");
        }

        public static void DecLevel(JsonWriter writer, int expectedLevel) {
            if (writer.level-- != expectedLevel)
                throw new InvalidOperationException($"Unexpected level in Write() end. Expect {expectedLevel}, Found: {writer.level + 1}");
        }
        
    }
}