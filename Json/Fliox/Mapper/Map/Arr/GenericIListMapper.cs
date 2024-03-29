﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
    internal sealed class GenericIListMatcher : ITypeMatcher {
        public static readonly GenericIListMatcher Instance = new GenericIListMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // don't handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(IList<>) );
            if (args == null)
                return null;
            Type elementType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (constructor == null) {
                if (type.GetGenericTypeDefinition() == typeof(IList<>))
                    constructor = ReflectUtils.GetDefaultConstructor(typeof(List<>).MakeGenericType(elementType));
                else
                    throw new NotSupportedException("not default constructor for type: " + type);
            }
            object[] constructorParams = {config, type, elementType, constructor};
            // new GenericIListMapper<IList<TElm>,TElm>  (config, type, elementType, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(GenericIListMapper<,>), new[] {type, elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }        
    }
    
    internal sealed class GenericIListMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : IList<TElm>
    {
        public override string  DataTypeName()          => $"IList<{typeof(TElm).Name}>";
        public override bool    IsNull(ref TCol value)  => value == null;
        public override int     Count(object array)     => ((TCol) array).Count;
        
        public GenericIListMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }
        
        public override DiffType Diff(Differ differ, TCol left, TCol right) {
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
            var list = (TCol)obj;
            int index = patcher.GetElementIndex(list.Count);
            var element = list[index];
            var action = patcher.DescendElement(elementType, element, out TElm value);
            if (action == NodeAction.Assign) {
                list[index] = value;
            }
        }

        public override void Write(ref Writer writer, TCol slot) {
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
        

        public override TCol Read(ref Reader reader, TCol slot, out bool success) {
            if (!reader.StartArray(this, out success))
                return default;
            
            var list = slot;
            int startLen = 0;
            if (list == null || reader.readerPool != null) {
                list = (TCol) CreateInstance(reader.readerPool);
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
                        if (startLen - index > 0) {
                            // list.RemoveRange(index, startLen - index);
                            for (int n = startLen - 1; n >= index; n--)
                                list.RemoveAt(n); // todo check O(n)
                        }
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
        
        public override void Copy(TCol src, ref TCol dst) {
            int dstCount = 0;
            if (dst == null) {
                dst = (TCol)(object)new List<TElm>(src.Count);
            }
            dstCount = dst.Count;
            int dstIndex = 0;
            foreach (var srcElement in src) {
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