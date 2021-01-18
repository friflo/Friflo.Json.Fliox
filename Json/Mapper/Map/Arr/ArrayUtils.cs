// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public static class ArrayUtils {
        public static StubType CreatePrimitiveHandler(Type type, Type itemType, IJsonMapper map) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                if (elementType == itemType) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    return new CollectionType(type, elementType, map, type.GetArrayRank(), null, constructor);
                }
            }
            return null;
        }
        
        public static StubType CreatePrimitiveList(Type type, Type itemType, IJsonMapper map) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
            if (args != null) {
                Type elementType = args[0];
                if (itemType != elementType)
                    return null;
                ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
                return new CollectionType  (type, elementType, map, 1, null, constructor);
            }
            return null;
        }

        public static bool ArrayElse<T>(JsonReader reader, ref Var slot, StubType stubType, T[] array, int index, int len) {
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
        
        public static bool ListElse(JsonReader reader, ref Var slot, StubType stubType, IList list) {
            switch (reader.parser.Event) {
                case JsonEvent.ArrayEnd:
                    slot.Obj = list;
                    return true;
                case JsonEvent.Error:
                    return false;
                default:
                    CollectionType collection = (CollectionType)stubType; 
                    return reader.ErrorIncompatible("array element", collection.ElementType , ref reader.parser);
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