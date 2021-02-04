// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Reflect;

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
            bytes.AppendBytes(ref field.nameBytes);
            bytes.AppendChar2('\"', ':');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString(JsonWriter writer, String str) {
            JsonSerializer.AppendEscString(ref writer.bytes, ref str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendNull(JsonWriter writer) {
            writer.bytes.AppendBytes(ref writer.@null);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncLevel(JsonWriter writer) {
            if (writer.level++ < writer.maxDepth)
                return writer.level;
            throw new InvalidOperationException($"JsonParser: maxDepth exceeded. maxDepth: {writer.maxDepth}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecLevel(JsonWriter writer, int expectedLevel) {
            if (writer.level-- != expectedLevel)
                throw new InvalidOperationException($"Unexpected level in Write() end. Expect {expectedLevel}, Found: {writer.level + 1}");
        }
        
    }
}