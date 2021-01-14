// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;


namespace Friflo.Json.Managed.Codecs
{
    public class DateTimeCodec : IJsonCodec
    {
        public static readonly DateTimeCodec Interface = new DateTimeCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(DateTime))
                return null;
            return new PrimitiveType (typeof(DateTime), Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            DateTime value = (DateTime) obj;
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            writer.bytes.AppendChar('\"');
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            ref var value = ref reader.parser.value;
            switch (reader.parser.Event) {
                case JsonEvent.ValueString:
                    if (DateTime.TryParse(value.ToString(), out DateTime ret))
                        return ret;
                    return reader.ErrorNull("Failed parsing DateTime. value: ", reader.parser.value.ToString());
                default:
                    return PrimitiveType.CheckElse(reader, nativeType);
            }
        }
    }
}
