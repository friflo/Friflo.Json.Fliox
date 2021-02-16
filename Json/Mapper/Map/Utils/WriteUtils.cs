// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Reflect;

#if JSON_BURST
    using Friflo.Json.Burst.Utils;
#endif

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
            if (writer.pretty)
                IndentBegin(ref writer);
            bytes.AppendBytes(ref writer.discriminator);
            writer.typeCache.AppendDiscriminator(ref bytes, mapper);
            bytes.AppendChar('\"');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteMemberKey(ref Writer writer, PropField field, ref bool firstMember) {
            if (!writer.pretty) {
                if (firstMember)
                    writer.bytes.AppendBytes(ref field.firstMember);
                else
                    writer.bytes.AppendBytes(ref field.subSeqMember);
            } else {
                writer.bytes.AppendChar(firstMember ? '{' : ',');
                IndentBegin(ref writer);
                writer.bytes.AppendChar('"');
                writer.bytes.AppendBytes(ref field.nameBytes);
                writer.bytes.AppendChar('"');
                writer.bytes.AppendChar(':');
                writer.bytes.AppendChar(' ');
            } 
            firstMember = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDelimiter(ref Writer writer, int pos) {
            if (pos > 0) {
                writer.bytes.EnsureCapacityAbs(writer.bytes.end + 1);
                writer.bytes.buffer.array[writer.bytes.end++] = (byte)',';
            }
            if (writer.pretty)
                IndentBegin(ref writer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteArrayEnd(ref Writer writer) {
            if (writer.pretty)
                IndentEnd(ref writer);
            writer.bytes.EnsureCapacityAbs(writer.bytes.end + 1);
            writer.bytes.buffer.array[writer.bytes.end++] = (byte)']';
        }
        
        public static void IndentBegin(ref Writer writer) {
            int level = writer.level;
            writer.bytes.EnsureCapacityAbs(writer.bytes.end + level + 1);
            writer.bytes.buffer.array[writer.bytes.end++] = (byte)'\n';
            for (int n = 0; n < level; n++)
                writer.bytes.buffer.array[writer.bytes.end++] = (byte)'\t';
        }
        
        public static void IndentEnd(ref Writer writer) {
            int level = writer.level - 1;
            writer.bytes.EnsureCapacityAbs(writer.bytes.end + level + 1);
            writer.bytes.buffer.array[writer.bytes.end++] = (byte)'\n';
            for (int n = 0; n < level; n++)
                writer.bytes.buffer.array[writer.bytes.end++] = (byte)'\t';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteObjectEnd(ref Writer writer, bool emptyObject) {
            if (writer.pretty)
                IndentEnd(ref writer);

            if (emptyObject) {
                writer.bytes.EnsureCapacityAbs(writer.bytes.end + 2);
                writer.bytes.buffer.array[writer.bytes.end++] = (byte)'{';
                writer.bytes.buffer.array[writer.bytes.end++] = (byte)'}';
            } else {
                writer.bytes.EnsureCapacityAbs(writer.bytes.end + 1);
                writer.bytes.buffer.array[writer.bytes.end++] = (byte)'}';
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString(ref Writer writer, String str) {
            JsonSerializer.AppendEscString(ref writer.bytes, in str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendNull(ref Writer writer) {
            writer.bytes.AppendBytes(ref writer.@null);
            FlushFilledBuffer(ref writer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FlushFilledBuffer(ref Writer writer) {
            if (writer.bytes.end < 4096)
                return;
            Flush(ref writer);
        }
        
        public static void Flush(ref Writer writer) {
            switch (writer.outputType) {
                case OutputType.ByteList:
                    return; // ByteList mode does not support streaming
                case OutputType.ByteWriter:
#if JSON_BURST
                    NonBurstWriter.WriteNonBurst(writer.writerHandle, ref writer.bytes.buffer, writer.bytes.end);
#else
                    writer.bytesWriter.Write(ref writer.bytes.buffer, writer.bytes.end);
#endif                    
                    writer.bytes.Clear();
                    break;
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