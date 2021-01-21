// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj
{
    public class DictionaryMapper : IJsonMapper
    {
        public static readonly DictionaryMapper Interface = new DictionaryMapper();
        
        public string DataTypeName() { return "Dictionary"; }
        
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

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            CollectionType collectionType = (CollectionType)stubType;
            IDictionary map = (IDictionary) slot.Obj;

            ref var bytes = ref writer.bytes;
            bytes.AppendChar('{');
            int n = 0;

            StubType elementType = collectionType.ElementType;
            Var elemVar = new Var();
            foreach (DictionaryEntry entry in map) {
                if (n++ > 0)
                    bytes.AppendChar(',');
                WriteUtils.WriteString(writer, (String) entry.Key);
                bytes.AppendChar(':');
                // elemVar.Set(entry.Value, elementType.varType, elementType.isNullable);
                elemVar.Obj = entry.Value;
                if (elemVar.IsNull)
                    writer.bytes.AppendBytes(ref writer.@null);
                else
                    elementType.map.Write(writer, ref elemVar, elementType);
            }
            bytes.AppendChar('}');
        }
        
        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ObjectUtils.StartObject(reader, ref slot, stubType, out bool success))
                return success;
            
            CollectionType collectionType = (CollectionType) stubType;
            if (slot.Obj == null)
                slot.Obj = collectionType.CreateInstance();
            IDictionary map = (IDictionary) slot.Obj;
            ref var parser = ref reader.parser;
            StubType elementType = collectionType.ElementType;
            Var elemVar = new Var(); 
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNull:
                        String key = parser.key.ToString();
                        if (!elementType.isNullable)
                            return ReadUtils.ErrorIncompatible(reader, "Dictionary value", elementType, ref parser);
                        map[key] = null;
                        break;
                    case JsonEvent.ObjectStart:
                        key = parser.key.ToString();
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        map[key] = elemVar.Get();
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        key = parser.key.ToString();
                        if (elementType.expectedEvent != ev)
                            return ReadUtils.ErrorIncompatible(reader, "Dictionary value", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        map[key] = elemVar.Get();
                        break;
                    case JsonEvent.ObjectEnd:
                        slot.Obj = map;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ReadUtils.ErrorMsg(reader, "unexpected state: ", ev);
                }
            }
        }
    }
}