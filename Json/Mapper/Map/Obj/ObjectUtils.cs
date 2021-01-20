// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Obj
{
    public static class ObjectUtils
    {
        public static bool StartObject(JsonReader reader, ref Var slot, StubType stubType, out bool success) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (stubType.isNullable) {
                        slot.Obj = null;
                        success = true;
                        return false;
                    }
                    reader.ErrorIncompatible(stubType.map.DataTypeName(), stubType, ref reader.parser);
                    success = false;
                    return false;
                case JsonEvent.ObjectStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    reader.ErrorIncompatible(stubType.map.DataTypeName(), stubType, ref reader.parser);
                    // reader.ErrorNull("Expect { or null. Got Event: ", ev);
                    return false;
            }
        }
        
    }
}