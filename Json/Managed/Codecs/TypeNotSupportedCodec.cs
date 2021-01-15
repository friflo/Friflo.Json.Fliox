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
            if (type.IsPrimitive)
                return new StubType(type, Interface);
            return null;
        }

        public object Read(JsonReader reader, object obj, StubType stubType) {
            throw new NotSupportedException("Type not supported. type: " + stubType.type.FullName);
        }

        public void Write(JsonWriter writer, object obj, StubType stubType) {
            throw new NotSupportedException("Type not supported. type: " + stubType.type.FullName);
        }
    }
}