// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Diff;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class GenericIReadOnlyCollectionMatcher : ITypeMatcher {
        public static readonly GenericIReadOnlyCollectionMatcher Instance = new GenericIReadOnlyCollectionMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(IReadOnlyCollection<>) );
            if (args == null)
                return null;
            Type elementType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetCopyConstructor(type);
            if (constructor == null) {
                throw new NotSupportedException("no copy constructor for type: " + type);
            }
            object[] constructorParams = {config, type, elementType, constructor};
            // new GenericIReadOnlyCollectionMapper<IReadOnlyCollection<TElm>,TElm>  (config, type, elementType, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(GenericIReadOnlyCollectionMapper<,>), new[] {type, elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }        
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class GenericIReadOnlyCollectionMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : IReadOnlyCollection<TElm>
    {
        public override string  DataTypeName() { return "IReadOnlyCollection"; }
        public override int     Count(object array) => ((TCol) array).Count;
        
        public GenericIReadOnlyCollectionMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }
        
        public override DiffNode Diff(Differ differ, TCol left, TCol right) {
            if (left.Count != right.Count)
                return differ.AddNotEqual(left, right);
            
            differ.PushParent(left, right);
            int n = 0;
            using (var rightIter = right.GetEnumerator()) {
                foreach (var leftItem in left) {
                    rightIter.MoveNext();
                    var rightItem = rightIter.Current;
                    differ.CompareElement(elementType, n++, leftItem, rightItem);
                }
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            throw new NotSupportedException($"Cant patch IReadOnlyCollection<>. Type: {type}");
        }

        public override void Write(ref Writer writer, TCol slot) {
            int startLevel = writer.IncLevel();
            var col = slot;
            writer.WriteArrayBegin();
            
            int n = 0;
            foreach (var curItem in col) {
                var item = curItem; // capture to use by ref
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
            
            List<TElm> list = new List<TElm>();
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
                        TCol col = (TCol)ReflectUtils.CreateInstanceCopy(constructor, list);
                        return col;
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
