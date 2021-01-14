// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Types
{
    public class PrimitiveType : NativeType
    {
        public readonly bool nullable;
        
        public PrimitiveType(Type type, IJsonCodec codec)
            : base(type, codec) {
            nullable = nullable = Nullable.GetUnderlyingType(type) != null;
        }

    }
}