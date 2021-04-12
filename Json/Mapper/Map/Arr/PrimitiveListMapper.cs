// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Mapper.Diff;
using Friflo.Json.Flow.Mapper.Map.Utils;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Mapper.Map.Arr
{
    public static class PrimitiveList
    {
        public static void AddListItemNull<TElm> (List<TElm> list, int index, int startLen) {
            if (index < startLen)
                list[index] = default;
            else
                list.Add(default);
        }
        
        public static void AddListItem<TElm> (List<TElm> list, TElm item, int index, int startLen) {
            if (index < startLen)
                list[index] = item;
            else
                list.Add(item);
        }
    }
    
    public class PrimitiveListMatcher : ITypeMatcher {
        public static readonly PrimitiveListMatcher Instance = new PrimitiveListMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof( List<>) );
            if (args == null)
                return null;
            Type elementType = args[0];
            return Find(config, type, elementType);
        }
        
         class Query {
            public TypeMapper   hit;
            public StoreConfig  config;
         }

         TypeMapper Find(StoreConfig config, Type type, Type elementType) {
            Query query = new Query();
            query.config = config;
            if (Match<double>   (type, elementType, query)) return query.hit;
            if (Match<float>    (type, elementType, query)) return query.hit;
            if (Match<long>     (type, elementType, query)) return query.hit;
            if (Match<int>      (type, elementType, query)) return query.hit;
            if (Match<short>    (type, elementType, query)) return query.hit;
            if (Match<byte>     (type, elementType, query)) return query.hit;
            if (Match<bool>     (type, elementType, query)) return query.hit;
            //
            if (Match<double?>  (type, elementType, query)) return query.hit;
            if (Match<float?>   (type, elementType, query)) return query.hit;
            if (Match<long?>    (type, elementType, query)) return query.hit;
            if (Match<int?>     (type, elementType, query)) return query.hit;
            if (Match<short?>   (type, elementType, query)) return query.hit;
            if (Match<byte?>    (type, elementType, query)) return query.hit;
            if (Match<bool?>    (type, elementType, query)) return query.hit;
            return null;
        }

        bool Match<T>(Type type, Type elementType, Query query) {
            if (typeof(T) != elementType)
                return false;
            
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (constructor == null)
                constructor = ReflectUtils.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
            //  new PrimitiveListMapper<T> (config, type, constructor);
            object[] constructorParams = { query.config, type, constructor };
            query.hit = (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(PrimitiveListMapper<>), new[] {elementType}, constructorParams);
            return true;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PrimitiveListMapper<T> : CollectionMapper<List<T>, T>
    {
        public override string  DataTypeName() { return "List"; }
        public override int     Count(object array) => ((List<T>) array).Count;
        
        public PrimitiveListMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, typeof(T), 1, typeof(string), constructor) {
        }
        
        public override DiffNode Diff(Differ differ, List<T> left, List<T> right) {
            if (left.Count != right.Count)
                return differ.AddNotEqual(left, right);
            
            differ.PushParent(left, right);
            for (int n = 0; n < left.Count; n++) {
                T leftItem  = left [n];
                T rightItem = right[n];
                differ.CompareElement(elementType, n, leftItem, rightItem);
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            var list = (List<T>)obj;
            int index = patcher.GetElementIndex(list.Count);
            var element = list[index];
            var action = patcher.DescendElement(elementType, element, out object value);
            if (action == NodeAction.Assign) {
                list[index] = (T) value;
            }
        }

        public override void Write(ref Writer writer, List<T> slot) {
            int startLevel = writer.IncLevel();
            var list = slot;
            writer.WriteArrayBegin();
            for (int n = 0; n < list.Count; n++) {
                writer.WriteDelimiter(n);
                var elemVar = list[n];

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
        

        public override List<T> Read(ref Reader reader, List<T> slot, out bool success) {
            if (!reader.StartArray(this, out success))
                return default;
            
            var list = slot;
            if (list == null)
                list = new List<T>(Reader.minLen);

            int startLen = list.Count;
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
                        PrimitiveList.AddListItem(list, elemVar, index++, startLen);
                        break;
                    case JsonEvent.ArrayEnd:
                        if (startLen - index > 0)
                            list.RemoveRange(index, startLen - index);
                        success = true;
                        return list;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return reader.ErrorMsg<List<T>>("unexpected state: ", ev, out success);
                }
            }
        }
    }
}