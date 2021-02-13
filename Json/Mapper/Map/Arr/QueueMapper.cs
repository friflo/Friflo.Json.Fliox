// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
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
        public override string DataTypeName() { return "Queue"; }
        
        public QueueMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(ref Writer writer, TCol slot) {
            int startLevel = WriteUtils.IncLevel(ref writer);
            var queue = slot;
            writer.bytes.AppendChar('[');
            
            int n = 0;
            foreach (var currentItem in queue) {
                var item = currentItem; // capture to use by ref
                WriteUtils.WriteDelimiter(ref writer, n++);
                
                if (!elementType.IsNull(ref item)) {
                    ObjectUtils.Write(ref writer, elementType, ref item);
                    WriteUtils.FlushFilledBuffer(ref writer);
                } else
                    WriteUtils.AppendNull(ref writer);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(ref writer, startLevel);
        }
        

        public override TCol Read(ref Reader reader, TCol slot, out bool success) {
            if (!ArrayUtils.StartArray(ref reader, this, out success))
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
                        TElm elemVar;
                        elemVar = default;
                        elemVar = ObjectUtils.Read(ref reader, elementType, ref elemVar, out success);
                        if (!success)
                            return default;
                        queue.Enqueue(elemVar);
                        break;
                    case JsonEvent.ValueNull:
                        if (!ArrayUtils.IsNullable(ref reader, this, elementType, out success))
                            return default;
                        queue.Enqueue(default);
                        break;
                    case JsonEvent.ArrayEnd:
                        success = true;
                        return queue;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        ReadUtils.ErrorMsg<bool>(ref reader, "unexpected state: ", ev, out success);
                        return default;
                }
            }
        }
    }
}
