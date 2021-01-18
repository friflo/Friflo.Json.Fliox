// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map
{
    public interface IJsonMapper
    {
        StubType    CreateStubType  (Type type);
        void        Write (JsonWriter writer, ref Var slot, StubType stubType);
        bool        Read  (JsonReader reader, ref Var slot, StubType stubType);
    }

}