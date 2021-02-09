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
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
            if (args != null) {
                Type keyType = args[0];
                if (keyType != typeof(string)) // Support only Dictionary with key type: string
                    return TypeNotSupportedMatcher.CreateTypeNotSupported(config, type, "Dictionary only support string as key type");
                Type elementType = args[1];
                ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = ReflectUtils.GetDefaultConstructor( typeof(Dictionary<,>).MakeGenericType(keyType, elementType) );
                object[] constructorParams = {config, type, constructor};
                // return new DictionaryMapper<TElm>  (config, type, constructor);
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
        
        public DictionaryMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, typeof(TElm), 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, Dictionary<string, TElm> map) {
            int startLevel = WriteUtils.IncLevel(writer);

            writer.bytes.AppendChar('{');
            int n = 0;

            foreach (var entry in map) {
                if (n++ > 0)
                    writer.bytes.AppendChar(',');
                WriteUtils.WriteString(writer, entry.Key);
                writer.bytes.AppendChar(':');
                
                var elemVar = entry.Value;
                if (EqualityComparer<TElm>.Default.Equals(elemVar, default))
                    WriteUtils.AppendNull(writer);
                else
                    elementType.Write(writer, elemVar);
            }
            writer.bytes.AppendChar('}');
            WriteUtils.DecLevel(writer, startLevel);
        }
        
        public override Dictionary<string, TElm> Read(JsonReader reader, Dictionary<string, TElm> map, out bool success) {
            if (!ObjectUtils.StartObject(reader, this, out success))
                return default;

            if (EqualityComparer<Dictionary<string, TElm>>.Default.Equals(map, default))
                map = (Dictionary<string, TElm>) CreateInstance();

            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        string key = reader.parser.key.ToString();
                        TElm elemVar = default;
                        elemVar = elementType.Read(reader, elemVar, out success);
                        if (!success)
                            return default;
                        map[key] = elemVar;
                        break;
                    case JsonEvent.ValueNull:
                        if (!elementType.isNullable) {
                            ReadUtils.ErrorIncompatible<Dictionary<string, TElm>>(reader, "Dictionary value", elementType, out success);
                            return default;
                        }
                        key = reader.parser.key.ToString();
                        map[key] = default;
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