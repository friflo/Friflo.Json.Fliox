// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;


namespace Friflo.Json.Managed.Codecs
{
    public class DateTimeCodec : IJsonCodec
    {
        public static readonly DateTimeCodec Interface = new DateTimeCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(DateTime))
                return null;
            return new PrimitiveType (typeof(DateTime), Interface);
        }
        
        public void Write (JsonWriter writer, object obj, StubType stubType) {
            DateTime value = (DateTime) obj;
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            writer.bytes.AppendChar('\"');
        }

        public Object Read(JsonReader reader, Object obj, StubType stubType) {
            ref var value = ref reader.parser.value;
            switch (reader.parser.Event) {
                case JsonEvent.ValueString:
                    if (DateTime.TryParse(value.ToString(), out DateTime ret))
                        return ret;
                    return reader.ErrorNull("Failed parsing DateTime. value: ", value.ToString());
                default:
                    return PrimitiveCodec.CheckElse(reader, stubType);
            }
        }
    }
}
