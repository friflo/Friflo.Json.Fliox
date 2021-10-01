// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Arr
{
    internal sealed class GenericIEnumerableMatcher : ITypeMatcher {
        public static readonly GenericIEnumerableMatcher Instance = new GenericIEnumerableMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // don't handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(IEnumerable<>) );
            if (args == null)
                return null;
            if (ReflectUtils.IsIDictionary(type))
                return null;
            Type elementType = args[0];
            ConstructorInfo constructor = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            object[] constructorParams = {config, type, elementType, constructor};
            // new GenericIEnumerableMapper<IEnumerable<TElm>,TElm>  (config, type, elementType, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(GenericIEnumerableMapper<,>), new[] {type, elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }        
    }
    
    internal sealed class GenericIEnumerableMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : IEnumerable<TElm>
    {
        public override string DataTypeName() { return "IEnumerable"; }
        
        public GenericIEnumerableMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }
        
        public override DiffNode Diff(Differ differ, TCol left, TCol right) {
            if (left.Count() != right.Count())
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
            throw new NotSupportedException($"Cant patch IEnumerable<>. Type: {type}");
        }

        public override void Write(ref Writer writer, TCol slot) {
            int startLevel = writer.IncLevel();
            var enumerable = slot;
            writer.WriteArrayBegin();

            int n = 0;
            foreach (var currentItem in enumerable) {
                var item = currentItem; // capture to use by ref
                writer.WriteDelimiter( n++);
                
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
            throw new InvalidOperationException("IEnumerable<> cannot be used for Read(). type: " + type);
        }
    }
}