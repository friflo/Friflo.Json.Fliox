// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;


namespace Friflo.Json.Managed.Codecs
{
    public class BigIntCodec : IJsonCodec
    {
        public static readonly BigIntCodec Interface = new BigIntCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(BigInteger))
                return null;
            return new BigIntType (typeof(BigInteger), Interface);
        }
        
        public void Write (JsonWriter writer, object obj, StubType stubType) {
            BigInteger value = (BigInteger) obj;
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString(value.ToString());
            writer.bytes.AppendChar('\"');
        }

        public Object Read(JsonReader reader, Object obj, StubType stubType) {
            ref var value = ref reader.parser.value;
            switch (reader.parser.Event) {
                case JsonEvent.ValueString:
                    if (value.Len > 0 && value.buffer.array[value.Len - 1] == 'n')
                        value.end--;
                    if (BigInteger.TryParse(value.ToString(), out BigInteger ret))
                        return ret;
                    return reader.ErrorNull("Failed parsing BigInt. value: ", reader.parser.value.ToString());
                case  JsonEvent.ValueNumber:
                    if (BigInteger.TryParse(value.ToString(), out BigInteger ret2))
                        return ret2;
                    return reader.ErrorNull("Failed parsing BigInt. value: ", reader.parser.value.ToString());
                default:
                    return PrimitiveCodec.CheckElse(reader, stubType);
            }
        }
    }
}
