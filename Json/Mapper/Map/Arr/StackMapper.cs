// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class StackMatcher : ITypeMatcher {
        public static readonly StackMatcher Instance = new StackMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(Stack<>) );
            if (args != null) {
                Type elementType = args[0];
                ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = ReflectUtils.GetDefaultConstructor(typeof(Stack<>).MakeGenericType(elementType));
                
                object[] constructorParams = {config, type, elementType, constructor};
                // new StackMapper<Stack<TElm>,TElm>  (config, type, elementType, constructor);
                var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(StackMapper<,>), new[] {type, elementType}, constructorParams);
                return (TypeMapper) newInstance;
            }
            return null;
        }        
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class StackMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : Stack<TElm>
    {
        public override string DataTypeName() { return "Stack"; }
        
        public StackMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, TCol slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            var stack = slot;
            writer.bytes.AppendChar('[');
            
            int n = 0;
            foreach (var currentItem in stack.Reverse()) {
                var item = currentItem; // capture to use by ref
                if (n++ > 0)
                    writer.bytes.AppendChar(',');
                
                if (!elementType.IsNull(ref item)) {
                    ObjectUtils.Write(writer, elementType, ref item);
                } else
                    WriteUtils.AppendNull(writer);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }
        

        public override TCol Read(JsonReader reader, TCol slot, out bool success) {
            if (!ArrayUtils.StartArray(reader, this, out success))
                return default;
            
            var stack = slot;
            if (stack == null)
                stack = (TCol) CreateInstance();
            else
                stack.Clear();
            List<TElm> list = new List<TElm>();

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
                        elemVar = ObjectUtils.Read(reader, elementType, ref elemVar, out success);
                        if (!success)
                            return default;
                        list.Add(elemVar);
                        break;
                    case JsonEvent.ValueNull:
                        if (!ArrayUtils.IsNullable(reader, this, elementType, out success))
                            return default;
                        list.Add(default);
                        break;
                    case JsonEvent.ArrayEnd:
                        success = true;
                        for (int n = 0; n < list.Count; n++)
                            stack.Push(list[n]);
                        return stack;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        ReadUtils.ErrorMsg<bool>(reader, "unexpected state: ", ev, out success);
                        return default;
                }
            }
        }
    }
}
