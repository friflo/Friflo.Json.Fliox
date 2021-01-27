// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class ArrayMatcher : ITypeMatcher {
        public static readonly ArrayMatcher Instance = new ArrayMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, ResolverConfig config) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                if (ReflectUtils.IsAssignableFrom(typeof(Object), elementType)) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    object[] constructorParams = {type, elementType, constructor};
                    // new ArrayMapper<T>(type, elementType, constructor);
                    var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(ArrayMapper<>), new[] {elementType}, constructorParams);
                    return (TypeMapper) newInstance;
                }
            }
            return null;
        }
    }
    
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ArrayMapper<TElm> : CollectionMapper <TElm[], TElm>
    {
        public override string DataTypeName() { return "array"; }

        public ArrayMapper(Type type, Type elementType, ConstructorInfo constructor) :
            base(type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, TElm[] slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            var arr = slot;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                // elemVar.Set(arr.GetValue(n), elementType.varType, elementType.isNullable);
                var elemVar = arr[n];
                
                // if (elemVar.IsNull)
                if (EqualityComparer<TElm>.Default.Equals(elemVar, default))
                    WriteUtils.AppendNull(writer);
                else
                    elementType.Write(writer, elemVar);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }

        public override TElm[] Read(JsonReader reader, TElm[] slot, out bool success) {
            if (!ArrayUtils.StartArray(reader, this, out success))
                return default;
            
            ref var parser = ref reader.parser;
            int startLen;
            int len;
            TElm[] array;
            // if (slot.Obj == null) {
            if (EqualityComparer<TElm[]>.Default.Equals(slot, default)) {
                startLen = 0;
                len = ReadUtils.minLen;
                array = new TElm[len];
            }
            else {
                array = slot;
                startLen = len = array.Length;
            }

            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        // array of string, bool, int, long, float, double, short, byte are handled via primitive array codecs
                        ReadUtils.ErrorIncompatible<TElm[]>(reader, "array element", elementType, ref parser, out success);
                        return default;
                    case JsonEvent.ValueNull:
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = ReadUtils.Inc(len));
                        if (!elementType.isNullable) {
                            ReadUtils.ErrorIncompatible<TElm[]>(reader, "array element", elementType, ref parser, out success);
                            return default;
                        }
                        array[index++] = default;
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        TElm elemVar;
                        if (index < startLen) {
                            elemVar = array[index];
                            elemVar = elementType.Read(reader, elemVar, out success);
                            if (!success)
                                return default;
                            array[index] = elemVar;
                        } else {
                            elemVar = default;
                            elemVar = elementType.Read(reader, elemVar, out success);
                            if (!success)
                                return default;
                            if (index >= len)
                                array = Arrays.CopyOf(array, len = ReadUtils.Inc(len));
                            array[index] = elemVar;
                        }
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        success = true;
                        return array;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<TElm[]>(reader, "unexpected state: ", ev, out success);
                }
            }
        }
    }
}
