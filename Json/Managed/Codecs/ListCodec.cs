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

        public void Write(JsonWriter writer, object obj, StubType stubType) {
            IList list = (IList) obj;
            CollectionType collectionType = (CollectionType) stubType;
            writer.bytes.AppendChar('[');
            StubType elementType = collectionType.ElementType;
            for (int n = 0; n < list.Count; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                Object item = list[n];
                if (item != null) {
                    switch (collectionType.id) {
                        case SimpleType.Id.Object:
                            elementType.codec.Write(writer, item, elementType);
                            break;
                        case SimpleType.Id.String:
                            writer.WriteString((String) item);
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



        public Object Read(JsonReader reader, object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            ref var parser = ref reader.parser;
            CollectionType collectionType = (CollectionType) stubType;
            IList list = (IList) col;
            if (list == null)
                list = (IList) collectionType.CreateInstance();
            StubType elementType = collectionType.ElementType;
            if (collectionType.id != SimpleType.Id.Object)
                list.Clear();
            int startLen = list.Count;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        list.Add(elementType.codec.Read(reader, null, elementType));
                        break;
                    case JsonEvent.ValueNumber:
                        list.Add(elementType.codec.Read(reader, null, elementType));
                        break;
                    case JsonEvent.ValueBool:
                        list.Add(elementType.codec.Read(reader, null, elementType));
                        break;
                    case JsonEvent.ValueNull:
                        if (index < startLen)
                            list[index] = null;
                        else
                            list.Add(null);
                        index++;
                        break;
                    case JsonEvent.ArrayStart:
                        StubType subElementType = collectionType.ElementType;
                        if (index < startLen) {
                            Object oldElement = list[index];
                            Object element = subElementType.codec.Read(reader, oldElement, subElementType);
                            if (element == null)
                                return null;
                            list[index] = element;
                        }
                        else {
                            Object element = subElementType.codec.Read(reader, null, subElementType);
                            if (element == null)
                                return null;
                            list.Add(element);
                        }

                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            Object oldElement = list[index];
                            Object element = elementType.codec.Read(reader, oldElement, elementType);
                            if (element == null)
                                return null;
                            list[index] = element;
                        }
                        else {
                            Object element = elementType.codec.Read(reader, null, elementType);
                            if (element == null)
                                return null;
                            list.Add(element);
                        }
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        // Remove from tail to head to avoid copying items after remove index
                       for (int n = startLen - 1; n >= index; n--)
                            list.Remove(n);
                        return list;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }
}