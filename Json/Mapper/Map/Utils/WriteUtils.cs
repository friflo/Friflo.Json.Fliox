// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class WriteUtils
    {

        public static void WriteDiscriminator(ref Writer writer, TypeMapper mapper) {
            ref var bytes = ref writer.bytes;
            bytes.AppendChar('{');
            bytes.AppendBytes(ref writer.discriminator);
            writer.typeCache.AppendDiscriminator(ref bytes, mapper);
            bytes.AppendChar('\"');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteMemberKey(ref Writer writer, PropField field, ref bool firstMember) {
            if (firstMember)
                writer.bytes.AppendBytes(ref field.firstMember);
            else
                writer.bytes.AppendBytes(ref field.subSeqMember);
            firstMember = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteObjectEnd(ref Writer writer, bool emptyObject) {
            if (emptyObject)
                writer.bytes.AppendChar2('{', '}');
            else
                writer.bytes.AppendChar('}');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString(ref Writer writer, String str) {
            JsonSerializer.AppendEscString(ref writer.bytes, ref str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendNull(ref Writer writer) {
            writer.bytes.AppendBytes(ref writer.@null);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FlushFullBuffer(ref Writer writer) {
            if (writer.bytes.end <= 4096)
                return;
            Flush(ref writer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Flush(ref Writer writer) {
            switch (writer.outputType) {
                case OutputType.ByteList:
                    throw new InvalidOperationException("Cant flush in mode ByteList");
#if !JSON_BURST
                case OutputType.ByteWriter:
                    writer.bytesWriter.Write(ref writer.bytes.buffer, writer.bytes.end);
                    break;
#endif
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncLevel(ref Writer writer) {
            if (writer.level++ < writer.maxDepth)
                return writer.level;
            throw new InvalidOperationException($"JsonParser: maxDepth exceeded. maxDepth: {writer.maxDepth}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecLevel(ref Writer writer, int expectedLevel) {
            if (writer.level-- != expectedLevel)
                throw new InvalidOperationException($"Unexpected level in Write() end. Expect {expectedLevel}, Found: {writer.level + 1}");
        }


    }
}