// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;

namespace Friflo.Json.Mapper.Map.Val
{
    public class BigIntMatcher : ITypeMatcher {
        public static readonly BigIntMatcher Instance = new BigIntMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(BigInteger))
                return null;
            return new BigIntMapper (config, type);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class BigIntMapper : TypeMapper<BigInteger>
    {
        public override string DataTypeName() { return "BigInteger"; }

        public BigIntMapper(StoreConfig config, Type type) : base (config, type, true, false) { }

        public override void Write(JsonWriter writer, BigInteger value) {
            WriteUtils.WriteString(writer, value.ToString());
        }

        public override BigInteger Read(ref Reader reader, BigInteger slot, out bool success) {
            ref var value = ref reader.parser.value;
            switch (reader.parser.Event) {
                case JsonEvent.ValueString:
                    if (value.Len > 0 && value.buffer.array[value.Len - 1] == 'n')
                        value.end--;
                    if (!BigInteger.TryParse(value.ToString(), out BigInteger ret))
                        return ReadUtils.ErrorMsg<BigInteger>(ref reader, "Failed parsing BigInt. value: ", value.ToString(), out success);
                    success = true;
                    return ret;
                case  JsonEvent.ValueNumber:
                    if (!BigInteger.TryParse(value.ToString(), out BigInteger ret2))
                        return ReadUtils.ErrorMsg<BigInteger>(ref reader, "Failed parsing BigInt. value: ", value.ToString(), out success);
                    success = true;
                    return ret2;
                default:
                    return ValueUtils.CheckElse(ref reader, this, out success);
            }
        }
    }
}
