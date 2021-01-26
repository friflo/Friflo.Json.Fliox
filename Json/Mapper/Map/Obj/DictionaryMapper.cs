// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj
{
    public class DictionaryMatcher : ITypeMatcher {
        public static readonly DictionaryMatcher Instance = new DictionaryMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, ResolverConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
            if (args != null) {
                Type keyType = args[0];
                if (keyType != typeof(string)) // Support only Dictionary with key type: string
                    return TypeNotSupportedMatcher.CreateTypeNotSupported(type, "Dictionary only support string as key type");
                Type elementType = args[1];
                ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = Reflect.GetDefaultConstructor( typeof(Dictionary<,>).MakeGenericType(keyType, elementType) );
                object[] constructorParams = {type, constructor};
                // return new DictionaryMapper<object>  (type, constructor);
                var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(DictionaryMapper<>), new[] {elementType}, constructorParams);
                return (TypeMapper) newInstance;
            }
            return null;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DictionaryMapper<TElm> : CollectionMapper<Dictionary<string, TElm>, TElm>
    {
        public override string DataTypeName() { return "Dictionary"; }
        
        public DictionaryMapper(Type type, ConstructorInfo constructor) :
            base(type, typeof(TElm), 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, Dictionary<string, TElm> slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            
            var map = slot;

            ref var bytes = ref writer.bytes;
            bytes.AppendChar('{');
            int n = 0;

            foreach (var entry in map) {
                if (n++ > 0)
                    bytes.AppendChar(',');
                WriteUtils.WriteString(writer, entry.Key);
                bytes.AppendChar(':');
                // elemVar.Set(entry.Value, elementType.varType, elementType.isNullable);
                var elemVar = entry.Value;
                //if (elemVar.IsNull)
                if (EqualityComparer<TElm>.Default.Equals(elemVar, default))
                    WriteUtils.AppendNull(writer);
                else
                    elementType.Write(writer, elemVar);
            }
            bytes.AppendChar('}');
            WriteUtils.DecLevel(writer, startLevel);
        }
        
        public override Dictionary<string, TElm> Read(JsonReader reader, Dictionary<string, TElm> slot, out bool success) {
            if (!ObjectUtils.StartObject(reader, this, out success))
                return default;

            if (EqualityComparer<Dictionary<string, TElm>>.Default.Equals(slot, default))
                slot = (Dictionary<string, TElm>) CreateInstance();
            var map = slot;
            ref var parser = ref reader.parser;

            while (true) {
                TElm elemVar;
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNull:
                        String key = parser.key.ToString();
                        if (!elementType.isNullable) {
                            ReadUtils.ErrorIncompatible<Dictionary<string, TElm>>(reader, "Dictionary value", elementType, ref parser, out success);
                            return default;
                        }
                        map[key] = default;
                        break;
                    case JsonEvent.ObjectStart:
                        key = parser.key.ToString();
                        elemVar = default;
                        elemVar = elementType.Read(reader, elemVar, out success);
                        if (!success)
                            return default;
                        map[key] = elemVar;
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        key = parser.key.ToString();
                        elemVar = default;
                        elemVar = elementType.Read(reader, elemVar, out success);
                        if (!success)
                            return default;
                        map[key] = elemVar;
                        break;
                    case JsonEvent.ObjectEnd:
                        success = true;
                        return map;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<Dictionary<string, TElm>>(reader, "unexpected state: ", ev, out success);
                }
            }
        }
    }
}