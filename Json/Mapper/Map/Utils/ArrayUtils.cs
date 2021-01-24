// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class ArrayUtils {
        public static bool StartArray<TVal, TElm>(JsonReader reader, TVal slot, CollectionMapper<TVal, TElm> map, out bool success) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (map.isNullable) {
                        success = true;
                        return false;
                    }
                    ReadUtils.ErrorIncompatible(reader, map.DataTypeName(), map, ref reader.parser, out success);
                    return default;
                case JsonEvent.ArrayStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    ReadUtils.ErrorIncompatible(reader, map.DataTypeName(), map, ref reader.parser, out success);
                    return false;
            }
        }

    }
}