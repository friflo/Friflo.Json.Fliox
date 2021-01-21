// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Utils
{
    public static class ArrayUtils {
        public static bool StartArray(JsonReader reader, ref Var slot, StubType stubType, out bool success) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (stubType.isNullable) {
                        slot.Obj = null;
                        success = true;
                        return false;
                    }
                    ReadUtils.ErrorIncompatible(reader, stubType.map.DataTypeName(), stubType, ref reader.parser);
                    success = false;
                    return false;
                case JsonEvent.ArrayStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    ReadUtils.ErrorIncompatible(reader, stubType.map.DataTypeName(), stubType, ref reader.parser);
                    return false;
            }
        }

    }
}