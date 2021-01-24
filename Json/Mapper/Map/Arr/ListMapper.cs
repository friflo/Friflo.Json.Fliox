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
    public class ListMatcher : ITypeMatcher {
        public static readonly ListMatcher Instance = new ListMatcher();
        
        public ITypeMapper CreateStubType(Type type) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
            if (args != null) {
                Type elementType = args[0];
                ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
                return new ListMapper<object>  (type, elementType, constructor); // todo replace generic type 
            }
            return null;
        }        
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ListMapper<TElm> : CollectionMapper<List<TElm>, TElm>
    {
        public override string DataTypeName() { return "List"; }
        
        public ListMapper(Type type, Type elementType, ConstructorInfo constructor) :
            base(type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, List<TElm> slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            var list = slot;
            writer.bytes.AppendChar('[');

            for (int n = 0; n < list.Count; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                TElm item = list[n];
                if (item != null) {
                    elementType.Write(writer, item);
                } else
                    WriteUtils.AppendNull(writer);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }
        

        public override List<TElm> Read(JsonReader reader, List<TElm> slot, out bool success) {
            if (!ArrayUtils.StartArray(reader, slot, this, out success))
                return default;
            
            ref var parser = ref reader.parser;
            var list = slot;
            int startLen = 0;
            if (list == null)
                list = new List<TElm>(ReadUtils.minLen);
            else
                startLen = list.Count;
            
            int index = 0;
            TElm elemVar;
            
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
                        if (index < startLen)
                            list[index] = elemVar;
                        else
                            list.Add(elemVar);
                        index++;
                        break;
                    case JsonEvent.ValueNull:
                        if (!elementType.isNullable) {
                            ReadUtils.ErrorIncompatible(reader, "List element", elementType, ref parser, out success);
                            return default;
                        }

                        if (index < startLen)
                            list[index] = default;
                        else
                            list.Add(default);
                        index++;
                        break;
                    case JsonEvent.ArrayStart:
                        if (index < startLen) {
                            elemVar = list[index];
                            elemVar = elementType.Read(reader, elemVar, out success);
                            if (!success)
                                return default;
                            list[index] = elemVar;
                        } else {
                            elemVar = default;
                            elemVar = elementType.Read(reader, elemVar, out success);
                            if (!success)
                                return default;
                            list.Add(elemVar);
                        }
                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            elemVar = list[index];
                            elemVar = elementType.Read(reader, elemVar, out success);
                            if (!success)
                                return default;
                            list[index] = elemVar;
                        } else {
                            elemVar = default;
                            elemVar = elementType.Read(reader, elemVar, out success);
                            if (!success)
                                return default;
                            list.Add(elemVar);
                        }
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        // Remove from tail to head to avoid copying items after remove index
                       for (int n = startLen - 1; n >= index; n--)
                            list.RemoveAt(n);
                        success = true;
                        return list;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<List<TElm>>(reader, "unexpected state: ", ev, out success);
                }
            }
        }
    }
}