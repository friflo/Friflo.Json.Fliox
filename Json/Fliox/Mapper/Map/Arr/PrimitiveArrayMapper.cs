﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;

namespace Friflo.Json.Fliox.Mapper.Map.Arr
{
    internal sealed class PrimitiveArrayMatcher : ITypeMatcher {
        public static readonly PrimitiveArrayMatcher Instance = new PrimitiveArrayMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!type.IsArray)
                return null;
            int rank = type.GetArrayRank();
            if (rank > 1)
                return null; // todo implement multi dimensional array support
            return Find(config, type);
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
    
    internal sealed class PrimitiveArrayMapper<T> : CollectionMapper <T[], T>
    {
        public override string  DataTypeName()          => $"{typeof(T).Name}[]";
        public override bool    IsNull(ref T[] value)   => value == null;
        public override int     Count(object array)     => ((T[]) array).Length;
        
        public PrimitiveArrayMapper(StoreConfig config, Type type) :
            base(config, type, typeof(T), 1, typeof(string), null) {
        }
        
        public override DiffType Diff(Differ differ, T[] left, T[] right) {
            if (left.Length != right.Length)
                return differ.AddNotEqualObject(left, right);
            
            differ.PushParent(left, right);
            for (int n = 0; n < left.Length; n++) {
                T leftItem  = left [n];
                T rightItem = right[n];
                if (differ.DiffElement(elementType, n, leftItem, rightItem) == DiffType.Equal)
                    continue;
                if (differ.DiffElements)
                    continue;
                return differ.PopParentNotEqual();
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            var list = (T[])obj;
            int index = patcher.GetElementIndex(list.Length);
            var element = list[index];
            var action = patcher.DescendElement(elementType, element, out T value);
            if (action == NodeAction.Assign) {
                list[index] = value;
            }
        }

        public override void Write(ref Writer writer, T[] slot) {
            int startLevel = writer.IncLevel();
            T[] array = slot;
            writer.WriteArrayBegin();
            for (int n = 0; n < array.Length; n++) {
                writer.WriteDelimiter(n);
                var elemVar = array[n];

                if (elementType.isNullable && EqualityComparer<T>.Default.Equals(elemVar, default)) {
                    writer.AppendNull();
                } else {
                    elementType.Write(ref writer, elemVar);
                    writer.FlushFilledBuffer();
                }
            }
            writer.WriteArrayEnd();
            writer.DecLevel(startLevel);
        }
        

        public override T[] Read(ref Reader reader, T[] slot, out bool success) {
            if (!reader.StartArray(this, out success))
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
                    case JsonEvent.ValueNull:
                        T elemVar = default;
                        elemVar = elementType.Read(ref reader, elemVar, out success);
                        if (!success)
                            return default;
                        if (index >= len)
                            array = CopyArray(array, len = Reader.Inc(len));
                        array[index++] = elemVar;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = CopyArray(array, index);
                        success = true;
                        return array ?? Array.Empty<T>();
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<T[]>("unexpected state: ", ev, out success);
                }
            }
        }
        
        public override void Copy(T[] src, ref T[] dst) {
            int len = src.Length; 
            if (len != dst.Length) {
                dst = new T[len];
            }
            for (int n = 0; n  < len; n++) {
                dst[n] = src[n];   
            }
        }
    }
}
