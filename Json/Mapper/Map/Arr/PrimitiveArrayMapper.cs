// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class PrimitiveArrayMatcher : ITypeMatcher {
        public static readonly PrimitiveArrayMatcher Instance = new PrimitiveArrayMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type. IsArray) {
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                return Find(config, type);
            }
            return null;
        }

        class Query {
            public TypeMapper   hit;
            public StoreConfig  config;
        }

        TypeMapper Find(StoreConfig  config, Type type) {
            Query query = new Query();
            query.config = config;
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
            
            // new PrimitiveArrayMapper<T>(type);
            object[] constructorParams = {query.config, type };
            query.hit = (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(PrimitiveArrayMapper<>), new[] {elementType}, constructorParams);
            return true;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PrimitiveArrayMapper<T> : CollectionMapper <T[], T>
    {
        public override string DataTypeName() { return "array"; }
        
        public PrimitiveArrayMapper(StoreConfig config, Type type) :
            base(config, type, typeof(T), 1, typeof(string), null) {
        }

        public override void Write(JsonWriter writer, T[] slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            T[] array = slot;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < array.Length; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                var elemVar = array[n];

                if (elementType.isNullable && EqualityComparer<T>.Default.Equals(elemVar, default))
                    WriteUtils.AppendNull(writer);
                else
                    elementType.Write(writer, elemVar);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }
        

        public override T[] Read(JsonReader reader, T[] slot, out bool success) {
            if (!ArrayUtils.StartArray(reader, this, out success))
                return default;
            
            T[] array = slot;

            int len = array?.Length ?? 0;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        T elemVar = default;
                        elemVar = elementType.Read(reader, elemVar, out success);
                        if (!success)
                            return default;
                        if (index >= len)
                            array = CopyArray(array, len = ReadUtils.Inc(len));
                        array[index++] = elemVar;
                        break;
                    case JsonEvent.ValueNull:
                        if (!ArrayUtils.IsNullable(reader, this, elementType, out success))
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
                        return ReadUtils.ErrorMsg<T[]>(reader, "unexpected state: ", ev, out success);
                }
            }
        }
    }
}
