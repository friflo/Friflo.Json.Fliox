// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Map.Arr
{
    public static class ArrayUtils {
        public static StubType CreatePrimitiveHandler(Type type, Type itemType, IJsonMapper jsonCodec) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                if (elementType == itemType) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    return new CollectionType(type, elementType, jsonCodec, type.GetArrayRank(), null, constructor);
                }
            }
            return null;
        }

        public static bool ArraysElse<T>(JsonReader reader, ref Var slot, StubType stubType, T[] array, int index, int len) {
            switch (reader.parser.Event) {
                case JsonEvent.ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf(array, index);
                    slot.Obj = array;
                    return true;
                case JsonEvent.Error:
                    return false;
                default:
                    ref JsonParser parser = ref reader.parser ;
                    CollectionType collection = (CollectionType)stubType; 
                    return reader.ErrorIncompatible("array element", collection.ElementType , ref parser);
            }
        }
        
        public static bool StartArray(JsonReader reader, ref Var slot, StubType stubType, out bool success) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (stubType.isNullable) {
                        slot.Obj = null;
                        success = true;
                        return false;
                    }
                    reader.ErrorIncompatible("array", stubType, ref reader.parser);
                    success = false;
                    return false;
                case JsonEvent.ArrayStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    reader.ErrorIncompatible("array", stubType, ref reader.parser);
                    return false;
            }
        }

    }
}