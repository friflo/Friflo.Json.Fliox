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
    internal class ListMatcher : ITypeMatcher {
        public static readonly ListMatcher Instance = new ListMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // don't handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof( List<>) );
            if (args == null)
                return null;
            Type elementType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (constructor == null)
                constructor = ReflectUtils.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
             
            object[] constructorParams = {config, type, elementType, constructor};
            // new ListMapper<List<TElm>,TElm>  (config, type, elementType, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(ListMapper<>), new[] {elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }        
    }
    
    internal class ListMapper<TElm> : CollectionMapper<List<TElm>, TElm>
    {
        public override string  DataTypeName() { return "List"; }
        public override int     Count(object array) => ((List<TElm>) array).Count;
        
        public ListMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }
        
        public override DiffNode Diff(Differ differ, List<TElm> left, List<TElm> right) {
            if (left.Count != right.Count)
                return differ.AddNotEqual(left, right);
            
            differ.PushParent(left, right);
            for (int n = 0; n < left.Count; n++) {
                TElm leftItem  = left [n];
                TElm rightItem = right[n];
                differ.CompareElement(elementType, n, leftItem, rightItem);
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            var list = (List<TElm>)obj;
            int index = patcher.GetElementIndex(list.Count);
            var element = list[index];
            var action = patcher.DescendElement(elementType, element, out object value);
            if (action == NodeAction.Assign) {
                list[index] = (TElm) value;
            }
        }
        
        public override void Trace(Tracer tracer, List<TElm> slot) {
            var list = slot;
            for (int n = 0; n < list.Count; n++) {
                TElm item = list[n];
                if (!elementType.IsNull(ref item)) {
                    elementType.Trace(tracer, item);
                }
            }
        }

        public override void Write(ref Writer writer, List<TElm> slot) {
            int startLevel = writer.IncLevel();
            var list = slot;
            writer.WriteArrayBegin();

            for (int n = 0; n < list.Count; n++) {
                writer.WriteDelimiter(n);
                TElm item = list[n];
                
                if (!elementType.IsNull(ref item)) {
                    writer.WriteElement(elementType, ref item);
                    writer.FlushFilledBuffer();
                } else
                    writer.AppendNull();
            }
            writer.WriteArrayEnd();
            writer.DecLevel(startLevel);
        }
        

        public override List<TElm> Read(ref Reader reader, List<TElm> slot, out bool success) {
            if (!reader.StartArray(this, out success))
                return default;
            
            var list = slot;
            int startLen = 0;
            if (list == null)
                list = (List<TElm>) CreateInstance();
            else
                startLen = list.Count;
            
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
                        TElm elemVar;
                        if (index < startLen) {
                            elemVar = list[index];
                            elemVar = reader.ReadElement(elementType, ref elemVar, out success);
                            if (!success)
                                return default;
                            list[index] = elemVar;
                        } else {
                            elemVar = default;
                            elemVar = reader.ReadElement(elementType, ref elemVar, out success);
                            if (!success)
                                return default;
                            list.Add(elemVar);
                        }
                        index++;
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
                        reader.ErrorMsg<List<TElm>>("unexpected state: ", ev, out success);
                        return default;
                }
            }
        }
    }
}