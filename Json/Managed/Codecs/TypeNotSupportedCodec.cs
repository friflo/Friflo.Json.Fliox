// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{

    
    public class TypeNotSupportedCodec : IJsonCodec
    {
        public static readonly TypeNotSupportedCodec Interface = new TypeNotSupportedCodec();

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