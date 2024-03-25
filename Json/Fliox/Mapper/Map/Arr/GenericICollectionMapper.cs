// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Arr
{
    internal sealed class GenericICollectionMatcher : ITypeMatcher {
        public static readonly GenericICollectionMatcher Instance = new GenericICollectionMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // don't handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(ICollection<>) );
            if (args == null)
                return null;
            if (ReflectUtils.IsIDictionary(type))
                return null;
            Type elementType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (constructor == null) {
                if (type.GetGenericTypeDefinition() == typeof(ICollection<>))
                    constructor = ReflectUtils.GetDefaultConstructor(typeof(List<>).MakeGenericType(elementType));
                else
                    throw new NotSupportedException("not default constructor for type: " + type);
            }
            object[] constructorParams = {config, type, elementType, constructor};
            // new GenericICollectionMapper<ICollection<TElm>,TElm>  (config, type, elementType, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(GenericICollectionMapper<,>), new[] {type, elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }        
    }
    
    internal sealed class GenericICollectionMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : ICollection<TElm>
    {
        private readonly    bool    diffElements;
        
        public override     string  DataTypeName()          => $"ICollection<{typeof(TElm).Name}>";
        public override     bool    IsNull(ref TCol value)  => value == null;
        public override     int     Count(object array)     => ((TCol) array).Count;

        public GenericICollectionMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor)
        {
            // don't create element Diff's if ICollection<> implements ISet<>
            // e.g. HashSet<> and SortedSet<> implements ISet<>
            bool isSet = type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>));
            diffElements = !isSet;
        }
        
        public override DiffType Diff(Differ differ, TCol left, TCol right) {
            if (left.Count != right.Count)
                return differ.AddNotEqualObject(left, right);
            
            differ.PushParent(left, right);
            int n = 0;
            using (var rightIter = right.GetEnumerator()) {
                foreach (var leftItem in left) {
                    rightIter.MoveNext();
                    var rightItem = rightIter.Current;
                    if (differ.DiffElement(elementType, n++, leftItem, rightItem) == DiffType.Equal)
                        continue;
                    if (diffElements && differ.DiffElements)
                        continue;
                    return differ.PopParentNotEqual();
                }
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            var list = (TCol)obj;
            var copy = list.ToArray();
            list.Clear();
            int index = patcher.GetElementIndex(copy.Length);
            var element = copy[index];
            var action = patcher.DescendElement(elementType, element, out TElm value);
            if (action == NodeAction.Assign) {
                copy[index] = value;
                for (int n = 0; n < copy.Length; n++)
                    list.Add(copy[n]);
            }
        }

        public override void Write(ref Writer writer, TCol slot) {
            int startLevel = writer.IncLevel();
            var list = slot;
            writer.WriteArrayBegin();
            
            int n = 0;
            foreach (var currentItem in list) {
                var item = currentItem; // capture to use by ref
                writer.WriteDelimiter(n++);
                
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
            if (list == null || reader.readerPool != null) {
                list = (TCol) CreateInstance(reader.readerPool);
                list.Clear();
            } else {
                list.Clear();
            }
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
                        elemVar = default;
                        elemVar = reader.ReadElement(elementType, ref elemVar, out success);
                        if (!success)
                            return default;
                        list.Add(elemVar);
                        break;
                    case JsonEvent.ArrayEnd:
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
            TElm[]  dstBuffer;

            if (dst == null) {
                dst = (TCol)(object)new List<TElm>(src.Count);
            }
            int dstCount    = dst.Count;
            if (dstCount > 0) {
                dstBuffer = dst.ToArray();
                dst.Clear();
            } else {
                dstBuffer = Array.Empty<TElm>();
            }
            int n = 0;
            foreach (var srcElement in src) {
                TElm dstElement = n < dstCount  ? dstBuffer[n++] : default;
                elementType.Copy(srcElement, ref dstElement);
                dst.Add(dstElement);
            }
        }
    }
}
