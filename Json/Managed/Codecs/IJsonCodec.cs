// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Prop;

namespace Friflo.Json.Managed.Codecs
{
    public interface IJsonCodec
    {
        NativeType CreateHandler(TypeResolver resolver, Type type);
        object  Read  (JsonReader reader, object obj, NativeType nativeType);
        void    Write (JsonWriter writer, object obj, NativeType nativeType);
    }
}