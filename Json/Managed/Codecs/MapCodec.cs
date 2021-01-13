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
    public class MapCodec : IJsonCodec
    {
        public static readonly MapCodec Resolver = new MapCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
            if (args != null) {
                Type keyType = args[0];
                Type elementType = args[1];
                ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = Reflect.GetDefaultConstructor( typeof(Dictionary<,>).MakeGenericType(keyType, elementType) );
                return new PropCollection  (type, elementType, this, 1, keyType, constructor);
            }
            return null;
        }

        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            PropCollection collection = (PropCollection)nativeType;
            IDictionary map = (IDictionary) obj;

            ref var bytes = ref writer.bytes;
            bytes.AppendChar('{');
            int n = 0;
            if (collection.elementType == typeof(String)) {
                // Map<String, String>
                IDictionary<String, String> strMap = (IDictionary<String, String>) map;
                foreach (KeyValuePair<String, String> entry in strMap) {
                    if (n++ > 0) bytes.AppendChar(',');
                    writer.WriteString(entry.Key);
                    bytes.AppendChar(':');
                    String value = entry.Value;
                    if (value != null)
                        writer.WriteString(value);
                    else
                        bytes.AppendBytes(ref writer.@null);
                }
            }
            else {
                // Map<String, Object>
                NativeType elementType = collection.GetElementType(writer.typeCache);
                foreach (DictionaryEntry entry in map) {
                    if (n++ > 0) bytes.AppendChar(',');
                    writer.WriteString((String) entry.Key);
                    bytes.AppendChar(':');
                    Object value = entry.Value;
                    if (value != null)
                        writer.WriteJson(value, elementType);
                    else
                        bytes.AppendBytes(ref writer.@null);
                }
            }
            bytes.AppendChar('}');

        }
        
        public Object Read(JsonReader reader, object obj, NativeType nativeType) {
            PropCollection collection = (PropCollection) nativeType;
            if (obj == null)
                obj = collection.CreateInstance();
            IDictionary map = (IDictionary) obj;
            ref var parser = ref reader.parser;
            NativeType elementType = collection.GetElementType(reader.typeCache);
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNull:
                        String key = parser.key.ToString();
                        map[key] = null;
                        break;
                    case JsonEvent.ObjectStart:
                        key = parser.key.ToString();
                        Object value = reader.ReadJson(null, elementType, 0);
                        if (value == null)
                            return null;
                        map[key] = value;
                        break;
                    case JsonEvent.ValueString:
                        key = parser.key.ToString();
                        if (collection.id != SimpleType.Id.String)
                            return reader.ErrorNull("Expect Dictionary value type string. Found: ",
                                collection.elementType.Name);
                        map[key] = parser.value.ToString();
                        break;
                    case JsonEvent.ValueNumber:
                        key = parser.key.ToString();
                        map[key] = reader.NumberFromValue(collection.id, out bool successNum);
                        if (!successNum)
                            return null;
                        break;
                    case JsonEvent.ValueBool:
                        key = parser.key.ToString();
                        map[key] = reader.BoolFromValue(collection.id, out bool successBool);
                        if (!successBool)
                            return null;
                        break;
                    case JsonEvent.ObjectEnd:
                        return map;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }
}