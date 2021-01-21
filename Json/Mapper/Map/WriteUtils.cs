using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map
{
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
        
    }
}