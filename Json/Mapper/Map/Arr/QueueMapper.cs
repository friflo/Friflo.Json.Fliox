// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Graph;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class QueueMatcher : ITypeMatcher {
        public static readonly QueueMatcher Instance = new QueueMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(Queue<>) );
            if (args == null)
                return null;
            Type elementType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (constructor == null)
                constructor = ReflectUtils.GetDefaultConstructor(typeof(Queue<>).MakeGenericType(elementType));
            
            object[] constructorParams = {config, type, elementType, constructor};
            // new StackMapper<Stack<TElm>,TElm>  (config, type, elementType, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(QueueMapper<,>), new[] {type, elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }        
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class QueueMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : Queue<TElm>
    {
        public override string  DataTypeName() { return "Queue"; }
        public override int     Count(object array) => ((TCol) array).Count;
        
        public QueueMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
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
            var list = (TCol)obj;
            var copy = list.ToArray();
            list.Clear();
            var count = copy.Length;
            int index = patcher.GetElementIndex(count);
            var element = copy[index];
            var action = patcher.DescendElement(elementType, element, out object value);
            if (action == NodeAction.Assign) {
                copy[index] = (TElm) value;
                for (int n = 0; n < count; n++)
                    list.Enqueue(copy[n]);
            }
        }

        public override void Write(ref Writer writer, TCol slot) {
            int startLevel = writer.IncLevel();
            var queue = slot;
            writer.WriteArrayBegin();
            
            int n = 0;
            foreach (var currentItem in queue) {
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
            
            var queue = slot;
            if (queue == null)
                queue = (TCol) CreateInstance();
            else
                queue.Clear();

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
                        queue.Enqueue(elemVar);
                        break;
                    case JsonEvent.ArrayEnd:
                        success = true;
                        return queue;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        reader.ErrorMsg<bool>("unexpected state: ", ev, out success);
                        return default;
                }
            }
        }
    }
}
