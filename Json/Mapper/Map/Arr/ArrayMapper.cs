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
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!type.IsArray)
                return null;
            Type elementType = type.GetElementType();
            int rank = type.GetArrayRank();
            if (rank > 1)
                return null; // todo implement multi dimensional array support
            if (ReflectUtils.IsAssignableFrom(typeof(Object), elementType)) {
                ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                // ReSharper disable once ExpressionIsAlwaysNull
                object[] constructorParams = {config, type, elementType, constructor};
                // new ArrayMapper<T>(config, type, elementType, constructor);
                var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(ArrayMapper<>), new[] {elementType}, constructorParams);
                return (TypeMapper) newInstance;
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

        public ArrayMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(ref Writer writer, TElm[] slot) {
            int startLevel = WriteUtils.IncLevel(ref writer);
            var arr = slot;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                WriteUtils.WriteDelimiter(ref writer, n);
                
                var elemVar = arr[n];
                if (elementType.IsNull(ref elemVar)) {
                    writer.AppendNull();
                } else {
                    elementType.Write(ref writer, elemVar);
                    WriteUtils.FlushFilledBuffer(ref writer);
                }
            }
            WriteUtils.WriteArrayEnd(ref writer);
            WriteUtils.DecLevel(ref writer, startLevel);
        }

        public override TElm[] Read(ref Reader reader, TElm[] slot, out bool success) {
            if (!ArrayUtils.StartArray(ref reader, this, out success))
                return default;
            
            int startLen;
            int len;
            TElm[] array = default;

            if (EqualityComparer<TElm[]>.Default.Equals(slot, default)) {
                startLen = 0;
                len = ReadUtils.minLen;
            }
            else {
                array = slot;
                startLen = len = array.Length;
            }
            
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        TElm elemVar;
                        if (index < startLen) {
                            elemVar = array[index];
                            elemVar = ObjectUtils.Read(ref reader, elementType, ref elemVar, out success);
                            if (!success)
                                return default;
                            array[index] = elemVar;
                        } else {
                            elemVar = default;
                            elemVar = ObjectUtils.Read(ref reader, elementType, ref elemVar, out success);
                            if (!success)
                                return default;
                            if (index >= len)
                                array = CopyArray(array, len = ReadUtils.Inc(len));
                            array[index] = elemVar;
                        }
                        index++;
                        break;
                    case JsonEvent.ValueNull:
                        if (!ArrayUtils.IsNullable(ref reader, this, elementType, out success))
                            return default;
                        if (index >= len)
                            array = CopyArray(array, len = ReadUtils.Inc(len));
                        array[index++] = default;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = CopyArray(array, index);
                        success = true;
                        return array;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<TElm[]>(ref reader, "unexpected state: ", ev, out success);
                }
            }
        }
    }
}
