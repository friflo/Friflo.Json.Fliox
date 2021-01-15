// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{
    public interface IJsonCodec
    {
        StubType    CreateStubType  (Type type);
        object      Read            (JsonReader reader, object obj, StubType stubType);
        void        Write           (JsonWriter writer, object obj, StubType stubType);
    }
}