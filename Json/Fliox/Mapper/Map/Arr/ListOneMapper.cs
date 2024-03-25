// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Arr
{
    internal sealed class ListOneMatcher : ITypeMatcher {
        public static readonly ListOneMatcher Instance = new ListOneMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // don't handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof( ListOne<>) );
            if (args == null)
                return null;
            Type elementType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (constructor == null)
                constructor = ReflectUtils.GetDefaultConstructor( typeof(ListOne<>).MakeGenericType(elementType) );
             
            object[] constructorParams = {config, type, elementType, constructor};
            // new ListOneMapper<ListOne<TElm>,TElm>  (config, type, elementType, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(ListOneMapper<>), new[] {elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }        
    }
    
    internal sealed class ListOneMapper<TElm> : CollectionMapper<ListOne<TElm>, TElm>
    {
        public override string  DataTypeName()                  => $"ListOne<{typeof(TElm).Name}>";
        public override bool    IsNull(ref ListOne<TElm> value) => false;
        public override int     Count(object array)             => ((ListOne<TElm>) array).Count;
        //
        public ListOneMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor, true, false)
        { }
        
        public override DiffType Diff(Differ differ, ListOne<TElm> left, ListOne<TElm> right) {
            if (left.Count != right.Count)
                return differ.AddNotEqualObject(left, right);
            
            differ.PushParent(left, right);
            for (int n = 0; n < left.Count; n++) {
                TElm leftItem  = left [n];
                TElm rightItem = right[n];
                if (differ.DiffElement(elementType, n, leftItem, rightItem) == DiffType.Equal)
                    continue;
                if (differ.DiffElements)
                    continue;
                return differ.PopParentNotEqual();
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            var list = (ListOne<TElm>)obj;
            int index = patcher.GetElementIndex(list.Count);
            var element = list[index];
            var action = patcher.DescendElement(elementType, element, out TElm value);
            if (action == NodeAction.Assign) {
                list[index] = value;
            }
        }
        
        public override void Write(ref Writer writer, ListOne<TElm> slot) {
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
        

        public override ListOne<TElm> Read(ref Reader reader, ListOne<TElm> slot, out bool success) {
            if (!reader.StartArray(this, out success))
                return default;
            
            var list = slot;
            int startLen = 0;
            if (list == null || reader.readerPool != null) {
                list = (ListOne<TElm>) CreateInstance(reader.readerPool);
                list.Clear();
            } else {
                startLen = list.Count;
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
                        reader.ErrorMsg<ListOne<TElm>>("unexpected state: ", ev, out success);
                        return default;
                }
            }
        }
        
        public override void Copy(ListOne<TElm> src, ref ListOne<TElm> dst) {
            int dstCount = 0;
            if (dst == null) {
                dst = new ListOne<TElm>(src.Count);
            } else {
                dstCount = dst.Count;
            }
            int dstIndex = 0;
            foreach (var srcElement in src.GetReadOnlySpan()) {
                if (dstIndex < dstCount) {
                    var dstElement = dst[dstIndex];
                    elementType.Copy(srcElement, ref dstElement);
                    dst[dstIndex++] = dstElement;
                } else {
                    TElm dstElement = default;
                    elementType.Copy(srcElement, ref dstElement);
                    dst.Add(dstElement);   
                }
            }
            // list.RemoveRange(index, startLen - index);
            for (int n = dstCount - 1; n >= dstIndex; n--) {
                dst.RemoveAt(n); // todo check O(n)
            }
        }
    }
}