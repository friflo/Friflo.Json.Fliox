// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Val
{
    public class BigIntMapper : IJsonMapper
    {
        public static readonly BigIntMapper Interface = new BigIntMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(BigInteger))
                return null;
            return new BigIntType (typeof(BigInteger), Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            BigInteger value = (BigInteger) slot.Obj;
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString(value.ToString());
            writer.bytes.AppendChar('\"');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            ref var value = ref reader.parser.value;
            switch (reader.parser.Event) {
                case JsonEvent.ValueString:
                    if (value.Len > 0 && value.buffer.array[value.Len - 1] == 'n')
                        value.end--;
                    if (!BigInteger.TryParse(value.ToString(), out BigInteger ret))
                        return reader.ErrorNull("Failed parsing BigInt. value: ", value.ToString());
                    slot.Obj = ret;
                    return true;
                case  JsonEvent.ValueNumber:
                    if (!BigInteger.TryParse(value.ToString(), out BigInteger ret2))
                        return reader.ErrorNull("Failed parsing BigInt. value: ", value.ToString());
                    slot.Obj = ret2;
                    return true;
                default:
                    return ValueUtils.CheckElse(reader, ref slot, stubType);
            }
        }
    }
}
