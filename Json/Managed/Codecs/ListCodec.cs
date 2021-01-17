// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public class ListCodec : IJsonCodec
    {
        public static readonly ListCodec Interface = new ListCodec();
        
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
                    // todo: implement missing element types
                    switch (collectionType.elementTypeId) {
                        case VarType.Object:
                            elemVar.Obj = item;
                            elementType.codec.Write(writer, ref elemVar, elementType);
                            break;
                        default:
                            throw new FrifloException("List element type not supported: " +
                                                      collectionType.ElementType.type.Name);
                    }
                }
                else
                    writer.bytes.AppendBytes(ref writer.@null);
            }
            writer.bytes.AppendChar(']');
        }
        

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return false;
            ref var parser = ref reader.parser;
            CollectionType collectionType = (CollectionType) stubType;
            IList list = (IList) slot.Obj;
            if (list == null)
                list = (IList) collectionType.CreateInstance();
            StubType elementType = collectionType.ElementType;
            // todo: document why != SlotType.Object
            if (collectionType.elementTypeId != VarType.Object)
                list.Clear();
            int startLen = list.Count;
            int index = 0;
            Var elemVar = new Var();
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (elementType.typeCat != TypeCat.String)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.codec.Read(reader, ref elemVar, elementType))
                            return false;
                        list.Add(elemVar.Get());
                        break;
                    case JsonEvent.ValueNumber:
                        if (elementType.typeCat != TypeCat.Number)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.codec.Read(reader, ref elemVar, elementType))
                            return false;
                        list.Add(elemVar.Get());
                        break;
                    case JsonEvent.ValueBool:
                        if (elementType.typeCat != TypeCat.Bool)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.codec.Read(reader, ref elemVar, elementType))
                            return false;
                        list.Add(elemVar.Get());
                        break;
                    case JsonEvent.ValueNull:
                        if (!elementType.isNullable)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
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
                            if (!subElementType.codec.Read(reader, ref elemVar, subElementType))
                                return false;
                            list[index] = elemVar.Obj;
                        }
                        else {
                            elemVar.Clear();
                            if (!subElementType.codec.Read(reader, ref elemVar, subElementType))
                                return false;
                            list.Add(elemVar.Obj);
                        }

                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            elemVar.Obj = list[index];
                            if (!elementType.codec.Read(reader, ref elemVar, elementType))
                                return false;
                            list[index] = elemVar.Obj;
                        }
                        else {
                            elemVar.Clear();
                            if (!elementType.codec.Read(reader, ref elemVar, elementType))
                                return false;
                            list.Add(elemVar.Obj);
                        }
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        // Remove from tail to head to avoid copying items after remove index
                       for (int n = startLen - 1; n >= index; n--)
                            list.Remove(n);
                        slot.Obj = list;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }
}