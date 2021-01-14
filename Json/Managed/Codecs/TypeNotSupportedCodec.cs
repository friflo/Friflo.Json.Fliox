// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{
    public class TypeNotSupported : NativeType {
        public TypeNotSupported(Type type) : 
            base(type, TypeNotSupportedCodec.Resolver) {
        }

        public override object CreateInstance() {
            throw new NotSupportedException("Type not supported" + type.FullName);
        }
    }
    
    public class TypeNotSupportedCodec : IJsonCodec
    {
        public static readonly TypeNotSupportedCodec Resolver = new TypeNotSupportedCodec();

        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type.IsPrimitive)
                return new NativeType(type, Resolver);
            return null;
        }

        public object Read(JsonReader reader, object obj, NativeType nativeType) {
            throw new NotSupportedException("Type not supported. type: " + nativeType.type.FullName);
        }

        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            throw new NotSupportedException("Type not supported. type: " + nativeType.type.FullName);
        }
    }
}