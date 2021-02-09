// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class ValueUtils
    {
        public static TVal CheckElse<TVal>(JsonReader reader, TypeMapper<TVal> mapper, out bool success) {
            switch (reader.parser.Event) {
                case JsonEvent.ValueNull:
                    const string msg = "requirement: null value must be handled by owner. Add missing JsonEvent.ValueNull case to its Mapper";
                    throw new InvalidOperationException(msg);
                    /*
                    if (!stubType.isNullable)
                        return JsonReader.ErrorIncompatible(reader, "primitive", stubType, ref parser);
                    value.SetNull(stubType.varType); // not necessary. null value us handled by owner.
                    return true;
                    */
                case JsonEvent.Error:
                    const string msg2 = "requirement: error must be handled by owner. Add missing JsonEvent.Error case to its Mapper";
                    throw new InvalidOperationException(msg2);
                    // return null;
                default:
                    return ReadUtils.ErrorIncompatible<TVal>(reader, mapper.DataTypeName(), mapper, out success);
            }
        }
    }
}