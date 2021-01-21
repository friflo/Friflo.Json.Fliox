// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map
{

#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TypeNotSupportedMapper : IJsonMapper
    {
        public static readonly TypeNotSupportedMapper Interface = new TypeNotSupportedMapper();
        
        public string DataTypeName() { return "unsupported type"; }

        public StubType CreateStubType(Type type) {
            return new NotSupportedType(type, "Type not supported");
        }
        
        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            NotSupportedType specific = (NotSupportedType) stubType;
            throw new NotSupportedException(specific.msg + ". Type: " + stubType.type);
        }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            NotSupportedType specific = (NotSupportedType) stubType;
            throw new NotSupportedException(specific.msg + ". Type: " + stubType.type);
        }
    }
}