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
                writer.IndentBegin();
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
                writer.IndentBegin();
                writer.bytes.AppendChar('"');
                writer.bytes.AppendBytes(ref field.nameBytes);
                writer.bytes.AppendChar('"');
                writer.bytes.AppendChar(':');
                writer.bytes.AppendChar(' ');
            } 
            firstMember = false;
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
    }
}