// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{
    public interface IJsonCodec
    {
        StubType    CreateStubType  (Type type);
        void        Write (JsonWriter writer, ref Var slot, StubType stubType);
        bool        Read  (JsonReader reader, ref Var slot, StubType stubType);
    }

}