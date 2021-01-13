// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public interface IJsonCodec
    {
        NativeType CreateHandler(TypeResolver resolver, Type type);
        ConstructorInfo GetConstructor(Type type, Type keyType, Type elementType);
        object  Read  (JsonReader reader, object obj, NativeType nativeType);
        void    Write (JsonWriter writer, object obj, NativeType nativeType);
    }
}