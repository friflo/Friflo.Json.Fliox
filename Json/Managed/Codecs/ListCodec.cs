// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public class ListCodec : IJsonCodec
    {
        public static readonly ListCodec Resolver = new ListCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
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

        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            IList list = (IList) obj;
            CollectionType collectionType = (CollectionType) nativeType;
            writer.bytes.AppendChar('[');
            NativeType elementType = collectionType.GetElementType(writer.typeCache);
            for (int n = 0; n < list.Count; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                Object item = list[n];
                if (item != null) {
                    switch (collectionType.id) {
                        case SimpleType.Id.Object:
                            writer.WriteJson(item, elementType);
                            break;
                        case SimpleType.Id.String:
                            writer.WriteString((String) item);
                            break;
                        default:
                            throw new FrifloException("List element type not supported: " +
                                                      collectionType.elementType.Name);
                    }
                }
                else
                    writer.bytes.AppendBytes(ref writer.@null);
            }

            writer.bytes.AppendChar(']');
        }



        public Object Read(JsonReader reader, object col, NativeType nativeType) {
            CollectionType collectionType = (CollectionType) nativeType;
            IList list = (IList) col;
            if (list == null)
                list = (IList) collectionType.CreateInstance();
            NativeType elementType = collectionType.GetElementType(reader.typeCache);
            if (collectionType.id != SimpleType.Id.Object)
                list.Clear();
            int startLen = list.Count;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        list.Add(reader.ReadJson(null, elementType, 0));
                        break;
                    case JsonEvent.ValueNumber:
                        list.Add(reader.ReadJson(null, elementType, 0));
                        break;
                    case JsonEvent.ValueBool:
                        list.Add(reader.ReadJson(null, elementType, 0));
                        break;
                    case JsonEvent.ValueNull:
                        if (index < startLen)
                            list[index] = null;
                        else
                            list.Add(null);
                        index++;
                        break;
                    case JsonEvent.ArrayStart:
                        NativeType subElementType = collectionType.GetElementType(reader.typeCache);
                        if (index < startLen) {
                            Object oldElement = list[index];
                            Object element = reader.ReadJson(oldElement, subElementType, 0);
                            if (element == null)
                                return null;
                            list[index] = element;
                        }
                        else {
                            Object element = reader.ReadJson(null, subElementType, 0);
                            if (element == null)
                                return null;
                            list.Add(element);
                        }

                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            Object oldElement = list[index];
                            Object element = reader.ReadJson(oldElement, elementType, 0);
                            if (element == null)
                                return null;
                            list[index] = element;
                        }
                        else {
                            Object element = reader.ReadJson(null, elementType, 0);
                            if (element == null)
                                return null;
                            list.Add(element);
                        }
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
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