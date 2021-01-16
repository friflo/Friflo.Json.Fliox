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
    public class MapCodec : IJsonCodec
    {
        public static readonly MapCodec Interface = new MapCodec();
        
        public StubType CreateStubType(Type type) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
            if (args != null) {
                Type keyType = args[0];
                if (keyType != typeof(string)) // Support only Dictionary with key type: string
                    return new NotSupportedType(type, "Dictionary only support string as key type");
                Type elementType = args[1];
                ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = Reflect.GetDefaultConstructor( typeof(Dictionary<,>).MakeGenericType(keyType, elementType) );
                return new CollectionType  (type, elementType, this, 1, keyType, constructor);
            }
            return null;
        }

        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            CollectionType collectionType = (CollectionType)stubType;
            IDictionary map = (IDictionary) slot.Obj;

            ref var bytes = ref writer.bytes;
            bytes.AppendChar('{');
            int n = 0;
            if (collectionType.ElementType.type == typeof(String)) {
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
                // Map<String, object>
                StubType elementType = collectionType.ElementType;
                Slot elemSlot = new Slot();
                foreach (DictionaryEntry entry in map) {
                    if (n++ > 0) bytes.AppendChar(',');
                    writer.WriteString((String) entry.Key);
                    bytes.AppendChar(':');
                    object value = entry.Value;
                    if (value != null) {
                        elemSlot.Obj = value;
                        elementType.codec.Write(writer, ref elemSlot, elementType);
                    } else
                        bytes.AppendBytes(ref writer.@null);
                }
            }
            bytes.AppendChar('}');

        }
        
        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            CollectionType collectionType = (CollectionType) stubType;
            if (slot.Obj == null)
                slot.Obj = collectionType.CreateInstance();
            IDictionary map = (IDictionary) slot.Obj;
            ref var parser = ref reader.parser;
            StubType elementType = collectionType.ElementType;
            Slot elemSlot = new Slot(); 
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNull:
                        String key = parser.key.ToString();
                        if (!elementType.isNullable)
                            return reader.ErrorIncompatible("Dictionary value", elementType, ref parser);
                        map[key] = null;
                        break;
                    case JsonEvent.ObjectStart:
                        key = parser.key.ToString();
                        elemSlot.Clear();
                        if (!elementType.codec.Read(reader, ref elemSlot, elementType))
                            return false;
                        map[key] = elemSlot.Get();
                        break;
                    case JsonEvent.ValueString:
                        key = parser.key.ToString();
                        if (elementType.typeCat != TypeCat.String)
                            return reader.ErrorIncompatible("Dictionary value", elementType, ref parser);
                        elemSlot.Clear();
                        if (!elementType.codec.Read(reader, ref elemSlot, elementType))
                            return false;
                        map[key] = elemSlot.Get();
                        break;
                    case JsonEvent.ValueNumber:
                        key = parser.key.ToString();
                        if (elementType.typeCat != TypeCat.Number)
                            return reader.ErrorIncompatible("Dictionary value", elementType, ref parser);
                        elemSlot.Clear();
                        if (!elementType.codec.Read(reader, ref elemSlot, elementType))
                            return false;
                        map[key] = elemSlot.Get();
                        break;
                    case JsonEvent.ValueBool:
                        key = parser.key.ToString();
                        if (elementType.typeCat != TypeCat.Bool)
                            return reader.ErrorIncompatible("Dictionary value", elementType, ref parser);
                        elemSlot.Clear();
                        if (!elementType.codec.Read(reader, ref elemSlot, elementType))
                            return false;
                        map[key] = elemSlot.Get();
                        break;
                    case JsonEvent.ObjectEnd:
                        slot.Obj = map;
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