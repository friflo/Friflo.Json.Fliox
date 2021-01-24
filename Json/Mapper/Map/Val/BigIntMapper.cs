// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Val
{
    public class BigIntMatcher : ITypeMatcher {
        public static readonly BigIntMatcher Instance = new BigIntMatcher();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(BigInteger))
                return null;
            return new BigIntType (typeof(BigInteger), BigIntMapper.Interface);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class BigIntMapper : ITypeMapper
    {
        public static readonly BigIntMapper Interface = new BigIntMapper();
        
        public string DataTypeName() { return "BigInteger"; }

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
                        return ReadUtils.ErrorMsg(reader, "Failed parsing BigInt. value: ", value.ToString());
                    slot.Obj = ret;
                    return true;
                case  JsonEvent.ValueNumber:
                    if (!BigInteger.TryParse(value.ToString(), out BigInteger ret2))
                        return ReadUtils.ErrorMsg(reader, "Failed parsing BigInt. value: ", value.ToString());
                    slot.Obj = ret2;
                    return true;
                default:
                    return ValueUtils.CheckElse(reader, ref slot, stubType);
            }
        }
    }
}
