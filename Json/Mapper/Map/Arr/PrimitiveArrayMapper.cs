// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class PrimitiveArrayMatcher : ITypeMatcher {
        public static readonly PrimitiveArrayMatcher Instance = new PrimitiveArrayMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type. IsArray) {
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                return Find(type);
            }
            return null;
        }

        class Query {
            public  ITypeMapper hit;
        }

        ITypeMapper Find(Type type) {
            Query query = new Query();
            if (Match<double>   (type, query)) return query.hit;
            if (Match<float>    (type, query)) return query.hit;
            if (Match<long>     (type, query)) return query.hit;
            if (Match<int>      (type, query)) return query.hit;
            if (Match<short>    (type, query)) return query.hit;
            if (Match<byte>     (type, query)) return query.hit;
            if (Match<bool>     (type, query)) return query.hit;
            //
            if (Match<double?>  (type, query)) return query.hit;
            if (Match<float?>   (type, query)) return query.hit;
            if (Match<long?>    (type, query)) return query.hit;
            if (Match<int?>     (type, query)) return query.hit;
            if (Match<short?>   (type, query)) return query.hit;
            if (Match<byte?>    (type, query)) return query.hit;
            if (Match<bool?>    (type, query)) return query.hit;
            //
            if (Match<string>   (type, query)) return query.hit;
            return null;
        }

        bool Match<T>(Type type, Query query) {
            Type elementType = type.GetElementType();
            if (typeof(T) != elementType)
                return false;
            query.hit = new PrimitiveArrayMapper<T>(type);
            return true;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PrimitiveArrayMapper<T> : CollectionMapper <T[], T>
    {
        public override string DataTypeName() { return "array"; }
        
        public PrimitiveArrayMapper(Type type) :
            base(type, typeof(T), 1, typeof(string), null) {
        }

        public override void Write(JsonWriter writer, T[] slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            T[] array = slot;
            writer.bytes.AppendChar('[');
            T elemVar;
            for (int n = 0; n < array.Length; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                elemVar = array[n];
                // if (elemVar.IsNull)
                if (EqualityComparer<T>.Default.Equals(elemVar, default))
                    WriteUtils.AppendNull(writer);
                else
                    elementType.Write(writer, elemVar);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }
        

        public override T[] Read(JsonReader reader, T[] slot, out bool success) {
            if (!ArrayUtils.StartArray(reader, slot, this, out success))
                return default;
            
            ref var parser = ref reader.parser;
            T[] array = slot;
            if (array == null)
                array = new T[ReadUtils.minLen];
            bool nullable = elementType.isNullable;

            int len = array.Length;
            int index = 0;
            T elemVar;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        elemVar = default;
                        elemVar = elementType.Read(reader, elemVar, out success);
                        if (!success)
                            return default;
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = ReadUtils.Inc(len));
                        array[index++] = elemVar;
                        break;
                    case JsonEvent.ValueNull:
                        if (!nullable) {
                            ReadUtils.ErrorIncompatible(reader, "array element", elementType, ref parser, out success);
                            return default;
                        }
                        array[index++] = default;
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        elemVar = default;
                        elemVar = elementType.Read(reader, elemVar, out success);
                        if (!success)
                            return default;
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = ReadUtils.Inc(len));
                        array[index++] = elemVar;
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
                        return ReadUtils.ErrorMsg<T[]>(reader, "unexpected state: ", ev, out success);
                }
            }
        }
    }
}
