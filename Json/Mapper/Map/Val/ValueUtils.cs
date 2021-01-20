// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
                    const string msg = "requirement: null value must be handled by owner. Add missing JsonEvent.ValueNull case to its Mapper";
                    throw new InvalidOperationException(msg);
                    /*
                    if (!stubType.isNullable)
                        return reader.ErrorIncompatible("primitive", stubType, ref parser);
                    value.SetNull(stubType.varType); // not necessary. null value us handled by owner.
                    return true;
                    */
                case JsonEvent.Error:
                    const string msg2 = "requirement: error must be handled by owner. Add missing JsonEvent.Error case to its Mapper";
                    throw new InvalidOperationException(msg2);
                    // return null;
                default:
                    return reader.ErrorIncompatible(stubType.map.DataTypeName(), stubType, ref parser);
            }
        }
    }
}