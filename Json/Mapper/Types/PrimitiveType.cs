// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Types
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PrimitiveType : StubType
    {
        
        public PrimitiveType(Type type, ITypeMapper map)
            : base(type, map, IsPrimitiveNullable(type), GetExpectedEvent(type))
        {
        }

        public override void InitStubType(TypeStore typeStore) {
        }

        private static bool IsPrimitiveNullable(Type type) {
            return Nullable.GetUnderlyingType(type) != null;
        }
        
        private static JsonEvent? GetExpectedEvent(Type type) {
            if (type == typeof(string))
                return JsonEvent.ValueString;
            
            if (type == typeof(bool)  || type == typeof(bool?))
                return JsonEvent.ValueBool;

            if (type == typeof(long)    || type == typeof(long?) ||
                type == typeof(int)     || type == typeof(int?) ||
                type == typeof(short)   || type == typeof(short?) ||
                type == typeof(byte)    || type == typeof(byte?) ||
                type == typeof(bool)    || type == typeof(bool?) ||
                type == typeof(double)  || type == typeof(double?) ||
                type == typeof(float)   || type == typeof(float?))
                return JsonEvent.ValueNumber;
            
            return null;
        }
    }
    
    public class BigIntType : StubType
    {
        public BigIntType(Type type, ITypeMapper map)
            : base(type, map, true, JsonEvent.ValueString)
        {
        }

        public override void InitStubType(TypeStore typeStore) {
        }
    }
    
    public class StringType : StubType
    {
        public StringType(Type type, ITypeMapper map)
            : base(type, map, true, JsonEvent.ValueString) {
        }
        
        public override void InitStubType(TypeStore typeStore) {
        }
    }

}