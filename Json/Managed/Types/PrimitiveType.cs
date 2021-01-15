// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Types
{
    public class PrimitiveType : StubType
    {
        
        public PrimitiveType(Type type, IJsonCodec codec)
            : base(type, codec, IsPrimitiveNullable(type))
        {
        }

        public override void InitStubType(TypeStore typeStore) {
        }

        private static bool IsPrimitiveNullable(Type type) {
            return Nullable.GetUnderlyingType(type) != null;
        }
    }
    
    public class StringType : StubType
    {
        public StringType(Type type, IJsonCodec codec)
            : base(type, codec, true) {
        }
        
        public override void InitStubType(TypeStore typeStore) {
        }
    }

}