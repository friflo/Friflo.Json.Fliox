// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class ListMapper : IJsonMapper
    {
        public static readonly ListMapper Interface = new ListMapper();
        
        public string DataTypeName() { return "List"; }
        
        public StubType CreateStubType(Type type) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
            if (args != null) {
                Type elementType = args[0];
                ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
                return new CollectionType  (type, elementType, this, 1, null, constructor);
            }
            return null;
        }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            IList list = (IList) slot.Obj;
            CollectionType collectionType = (CollectionType) stubType;
            writer.bytes.AppendChar('[');
            StubType elementType = collectionType.ElementType;
            Var elemVar = new Var();
            for (int n = 0; n < list.Count; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                Object item = list[n];
                if (item != null) {
                    elemVar.Obj = item;
                    elementType.map.Write(writer, ref elemVar, elementType);
                } else
                    writer.bytes.AppendBytes(ref writer.@null);
            }
            writer.bytes.AppendChar(']');
        }
        

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            ref var parser = ref reader.parser;
            CollectionType collectionType = (CollectionType) stubType;
            IList list = (IList) slot.Obj;
            int startLen = 0;
            if (list == null)
                list = (IList) collectionType.CreateInstance();
            else
                startLen = list.Count;
            
            StubType elementType = collectionType.ElementType;
            int index = 0;
            Var elemVar = new Var();
            
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        if (elementType.expectedEvent != ev)
                            return JsonReader.ErrorIncompatible(reader, "List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        if (index < startLen)
                            list[index] = elemVar.Get();
                        else
                            list.Add(elemVar.Get());
                        index++;
                        break;
                    case JsonEvent.ValueNull:
                        if (!elementType.isNullable)
                            return JsonReader.ErrorIncompatible(reader, "List element", elementType, ref parser);
                        if (index < startLen)
                            list[index] = null;
                        else
                            list.Add(null);
                        index++;
                        break;
                    case JsonEvent.ArrayStart:
                        StubType subElementType = collectionType.ElementType;
                        if (index < startLen) {
                            elemVar.Obj = list[index];
                            if (!subElementType.map.Read(reader, ref elemVar, subElementType))
                                return false;
                            list[index] = elemVar.Obj;
                        }
                        else {
                            elemVar.Clear();
                            if (!subElementType.map.Read(reader, ref elemVar, subElementType))
                                return false;
                            list.Add(elemVar.Obj);
                        }
                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            elemVar.Obj = list[index];
                            if (!elementType.map.Read(reader, ref elemVar, elementType))
                                return false;
                            list[index] = elemVar.Obj;
                        }
                        else {
                            elemVar.Clear();
                            if (!elementType.map.Read(reader, ref elemVar, elementType))
                                return false;
                            list.Add(elemVar.Obj);
                        }
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        // Remove from tail to head to avoid copying items after remove index
                       for (int n = startLen - 1; n >= index; n--)
                            list.RemoveAt(n);
                        slot.Obj = list;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return JsonReader.ErrorMsg(reader, "unexpected state: ", ev);
                }
            }
        }
    }
}