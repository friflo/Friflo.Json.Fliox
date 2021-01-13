// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public class ObjectArrayCodec : IJsonCodec
    {
        public static readonly ObjectArrayCodec Resolver = new ObjectArrayCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return new TypeNotSupported(type);
                if (Reflect.IsAssignableFrom(typeof(Object), elementType)) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    return new CollectionType(type, elementType, this, type.GetArrayRank(), null, constructor);
                }
            }
            return null;
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            CollectionType collectionType = (CollectionType) nativeType;
            Array arr = (Array) obj;
            writer.bytes.AppendChar('[');
            NativeType elementType = collectionType.GetElementType(writer.typeCache);
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                object item = arr.GetValue(n);
                if (item == null)
                    writer.bytes.AppendBytes(ref writer.@null);
                else
                    writer.WriteJson(item, elementType);
            }
            writer.bytes.AppendChar(']');
        }

        public object Read(JsonReader reader, object col, NativeType nativeType) {
            var collection = (CollectionType) nativeType;
            int startLen;
            int len;
            Array array;
            if (col == null) {
                startLen = 0;
                len = JsonReader.minLen;
                array = Arrays.CreateInstance(collection.elementType, len);
            }
            else {
                array = (Array) col;
                startLen = len = array.Length;
            }

            NativeType elementType = collection.GetElementType(reader.typeCache);
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        // array of string, bool, int, long, float, double, short, byte are handled in ReadJsonArray()
                        return reader.ErrorNull("expect array item of type: ", collection.elementType.Name);
                    case JsonEvent.ValueNull:
                        if (index >= len)
                            array = Arrays.CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                        array.SetValue(null, index++);
                        break;
                    case JsonEvent.ArrayStart:
                        NativeType subElementArray = collection.GetElementType(reader.typeCache);
                        if (index < startLen) {
                            Object oldElement = array.GetValue(index);
                            Object element = reader.ReadJson(oldElement, subElementArray, 0);
                            if (element == null)
                                return null;
                            array.SetValue(element, index);
                        }
                        else {
                            Object element = reader.ReadJson(null, subElementArray, 0);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                            array.SetValue(element, index);
                        }

                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            Object oldElement = array.GetValue(index);
                            Object element = reader.ReadJson(oldElement, elementType, 0);
                            if (element == null)
                                return null;
                            array.SetValue(element, index);
                        }
                        else {
                            Object element = reader.ReadJson(null, elementType, 0);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                            array.SetValue(element, index);
                        }

                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOfType(collection.elementType, array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }
}
