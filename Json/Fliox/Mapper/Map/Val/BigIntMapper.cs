// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    internal sealed class BigIntMatcher : ITypeMatcher {
        public static readonly BigIntMatcher Instance = new BigIntMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(BigInteger))
                return new BigIntMapper (config, type);
            if (type == typeof(BigInteger?))
                return new NullableBigIntMapper (config, type);
            return null;
        }
        
        internal static BigInteger Read(TypeMapper mapper, ref Reader reader, out bool success) {
            ref var value = ref reader.parser.value;
            switch (reader.parser.Event) {
                case JsonEvent.ValueString:
                    if (value.Len > 0 && value.buffer[value.Len - 1] == 'n')
                        value.end--;
                    if (!BigInteger.TryParse(value.AsString(), out BigInteger ret))
                        return reader.ErrorMsg<BigInteger>("Failed parsing BigInt. value: ", value.AsString(), out success);
                    success = true;
                    return ret;
                case  JsonEvent.ValueNumber:
                    if (!BigInteger.TryParse(value.AsString(), out BigInteger ret2))
                        return reader.ErrorMsg<BigInteger>("Failed parsing BigInt. value: ", value.AsString(), out success);
                    success = true;
                    return ret2;
                case JsonEvent.ValueNull:
                    if (!mapper.isNullable) {
                        reader.ErrorIncompatible<BigInteger>(mapper.DataTypeName(), mapper, out success);
                        return default;
                    }
                    success = true;
                    return default;
                default:
                    reader.ErrorIncompatible<BigInteger>(mapper.DataTypeName(), mapper, out success);
                    return default;
            }
        }
    }
    
    internal sealed class BigIntMapper : TypeMapper<BigInteger>
    {
        public override StandardTypeId  StandardTypeId                  => StandardTypeId.BigInteger;
        public override string          StandardTypeName                => "BigInteger";
        public override string          DataTypeName()                  => "BigInteger";
        public override bool            IsNull(ref BigInteger value)    => false;

        public BigIntMapper(StoreConfig config, Type type) : base (config, type, false, false) { }

        public override void Write(ref Writer writer, BigInteger value) {
            writer.WriteString(value.ToString());
        }

        public override BigInteger Read(ref Reader reader, BigInteger slot, out bool success) {
            return BigIntMatcher.Read(this, ref reader, out success);
        }
    }
    
    internal sealed class NullableBigIntMapper : TypeMapper<BigInteger?>
    {
        public override StandardTypeId  StandardTypeId                  => StandardTypeId.BigInteger;
        public override string          StandardTypeName                => "BigInteger";
        public override string          DataTypeName()                  => "BigInteger?";
        public override bool            IsNull(ref BigInteger? value)   => !value.HasValue;

        public NullableBigIntMapper(StoreConfig config, Type type) : base (config, type, true, false) { }

        public override void Write(ref Writer writer, BigInteger? value) {
            if (value.HasValue)
                writer.WriteString(value.Value.ToString());
            else
                writer.AppendNull();
        }

        public override BigInteger? Read(ref Reader reader, BigInteger? slot, out bool success) {
            return BigIntMatcher.Read(this, ref reader, out success);
        }
    }
}
