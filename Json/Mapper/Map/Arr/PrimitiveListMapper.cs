// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
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
        
        public ITypeMapper CreateStubType(Type type) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
            if (args != null) {
                Type elementType = args[0];
                return Find(type, elementType);
            }
            return null;
        }
        
         class Query {
            public  ITypeMapper hit;
        }

         ITypeMapper Find(Type type, Type elementType) {
            Query query = new Query();
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
            
            ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
            if (constructor == null)
                constructor = Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
            //  new PrimitiveListMapper<T> (type, constructor);
            object[] constructorParams = { type, constructor };
            query.hit = (ITypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(PrimitiveListMapper<>), new[] {elementType}, constructorParams);
            return true;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class PrimitiveListMapper<T> : CollectionMapper<List<T>, T>
    {
        public override string DataTypeName() { return "List"; }
        
        public PrimitiveListMapper(Type type, ConstructorInfo constructor) :
            base(type, typeof(T), 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, List<T> slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            var list = slot;
            writer.bytes.AppendChar('[');
            T elemVar;
            for (int n = 0; n < list.Count; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                elemVar = list[n];
                // if (elemVar.IsNull)
                if (elementType.isNullable && EqualityComparer<T>.Default.Equals(elemVar, default))
                    WriteUtils.AppendNull(writer);
                else
                    elementType.Write(writer, elemVar);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }
        

        public override List<T> Read(JsonReader reader, List<T> slot, out bool success) {
            if (!ArrayUtils.StartArray(reader, this, out success))
                return default;
            
            ref var parser = ref reader.parser;
            var list = slot;
            if (list == null)
                list = new List<T>(ReadUtils.minLen);

            int startLen = list.Count;
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
                        PrimitiveList.AddListItem(list, elemVar, index++, startLen);
                        break;
                    case JsonEvent.ValueNull:
                        if (!elementType.isNullable) {
                            ReadUtils.ErrorIncompatible<List<T>>(reader, "List element", elementType, ref parser, out success);
                            return default;
                        }
                        PrimitiveList.AddListItemNull(list, index++, startLen);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        elemVar = default;
                        elemVar = elementType.Read(reader, elemVar, out success);
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
                        return ReadUtils.ErrorMsg<List<T>>(reader, "unexpected state: ", ev, out success);
                }
            }
        }
    }
}