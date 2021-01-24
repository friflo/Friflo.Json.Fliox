// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Types
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PrimitiveType : StubType
    {
        
        public PrimitiveType(Type type, TypeMapper map)
            : base(type, map, IsPrimitiveNullable(type))
        {
        }

        public override void InitStubType(TypeStore typeStore) {
        }

        private static bool IsPrimitiveNullable(Type type) {
            return Nullable.GetUnderlyingType(type) != null;
        }
    }
    
    public class BigIntType : StubType
    {
        public BigIntType(Type type, TypeMapper map)
            : base(type, map, true)
        {
        }

        public override void InitStubType(TypeStore typeStore) {
        }
    }
    
    public class StringType : StubType
    {
        public StringType(Type type, TypeMapper map)
            : base(type, map, true) {
        }
        
        public override void InitStubType(TypeStore typeStore) {
        }
    }

}