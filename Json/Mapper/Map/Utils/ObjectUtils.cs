// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class ObjectUtils
    {
        public static bool StartObject<T>(JsonReader reader, TypeMapper<T> mapper, out bool success) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (mapper.isNullable) {
                        success = true;
                        return false;
                    }
                    ReadUtils.ErrorIncompatible<T>(reader, mapper.DataTypeName(), mapper, ref reader.parser, out success);
                    success = false;
                    return false;
                case JsonEvent.ObjectStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    ReadUtils.ErrorIncompatible<T>(reader, mapper.DataTypeName(), mapper, ref reader.parser, out success);
                    // reader.ErrorNull("Expect { or null. Got Event: ", ev);
                    return false;
            }
        }
        
    }
}