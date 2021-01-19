// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Val
{
    public static class ValueUtils
    {
        public static bool CheckElse(JsonReader reader, ref Var value, StubType stubType) {
            ref JsonParser parser = ref reader.parser;
            switch (parser.Event) {
                case JsonEvent.ValueNull:
                    if (stubType.isNullable) {
                        value.SetNull(stubType.varType);
                        return true;
                    }
                    return reader.ErrorIncompatible("primitive", stubType, ref parser);
                case JsonEvent.Error:
                    return false;
                default:
                    return reader.ErrorIncompatible("primitive", stubType, ref parser);
            }
        }
    }
}