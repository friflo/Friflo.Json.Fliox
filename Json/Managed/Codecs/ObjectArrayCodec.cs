// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public class ObjectArrayCodec : IJsonCodec
    {
        public static readonly ObjectArrayCodec Interface = new ObjectArrayCodec();
        
        public StubType CreateStubType(Type type) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                if (Reflect.IsAssignableFrom(typeof(Object), elementType)) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    return new CollectionType(type, elementType, this, type.GetArrayRank(), null, constructor);
                }
            }
            return null;
        }
        
        public void Write (JsonWriter writer, object obj, StubType stubType) {
            CollectionType collectionType = (CollectionType) stubType;
            Array arr = (Array) obj;
            writer.bytes.AppendChar('[');
            StubType elementType = collectionType.ElementType;
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                object item = arr.GetValue(n);
                if (item == null)
                    writer.bytes.AppendBytes(ref writer.@null);
                else
                    elementType.codec.Write(writer, item, elementType);
            }
            writer.bytes.AppendChar(']');
        }

        public object Read(JsonReader reader, object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            
            ref var parser = ref reader.parser;
            var collection = (CollectionType) stubType;
            int startLen;
            int len;
            Array array;
            if (col == null) {
                startLen = 0;
                len = JsonReader.minLen;
                array = Arrays.CreateInstance(collection.ElementType.type, len);
            }
            else {
                array = (Array) col;
                startLen = len = array.Length;
            }

            StubType elementType = collection.ElementType;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        // array of string, bool, int, long, float, double, short, byte are handled via primitive array codecs
                        return reader.ErrorNull("expect array item of type: ", collection.ElementType.type.Name);
                    case JsonEvent.ValueNull:
                        if (index >= len)
                            array = Arrays.CopyOfType(collection.ElementType.type, array, len = JsonReader.Inc(len));
                        if (!elementType.isNullable)
                            return reader.ErrorNull("Array element is not nullable. Element Type: ", elementType.type.FullName);
                        array.SetValue(null, index++);
                        break;
                    case JsonEvent.ArrayStart:
                        StubType subElementArray = collection.ElementType;
                        if (index < startLen) {
                            Object oldElement = array.GetValue(index);
                            Object element = subElementArray.codec.Read(reader, oldElement, subElementArray);
                            if (element == null)
                                return null;
                            array.SetValue(element, index);
                        }
                        else {
                            Object element = subElementArray.codec.Read(reader, null, subElementArray);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.ElementType.type, array, len = JsonReader.Inc(len));
                            array.SetValue(element, index);
                        }

                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            Object oldElement = array.GetValue(index);
                            Object element = elementType.codec.Read(reader, oldElement, elementType);
                            if (element == null)
                                return null;
                            array.SetValue(element, index);
                        }
                        else {
                            Object element = elementType.codec.Read(reader, null, elementType);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.ElementType.type, array, len = JsonReader.Inc(len));
                            array.SetValue(element, index);
                        }

                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOfType(collection.ElementType.type, array, index);
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
