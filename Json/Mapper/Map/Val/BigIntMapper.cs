// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Numerics;
using Friflo.Json.Burst;

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

        public BigIntMapper(StoreConfig config, Type type) : base (config, type, false, false) { }

        public override void Write(ref Writer writer, BigInteger value) {
            writer.WriteString(value.ToString());
        }

        public override BigInteger Read(ref Reader reader, BigInteger slot, out bool success) {
            ref var value = ref reader.parser.value;
            switch (reader.parser.Event) {
                case JsonEvent.ValueString:
                    if (value.Len > 0 && value.buffer.array[value.Len - 1] == 'n')
                        value.end--;
                    if (!BigInteger.TryParse(value.ToString(), out BigInteger ret))
                        return reader.ErrorMsg<BigInteger>("Failed parsing BigInt. value: ", value.ToString(), out success);
                    success = true;
                    return ret;
                case  JsonEvent.ValueNumber:
                    if (!BigInteger.TryParse(value.ToString(), out BigInteger ret2))
                        return reader.ErrorMsg<BigInteger>("Failed parsing BigInt. value: ", value.ToString(), out success);
                    success = true;
                    return ret2;
                default:
                    return reader.HandleEvent(this, out success);
            }
        }
    }
}
