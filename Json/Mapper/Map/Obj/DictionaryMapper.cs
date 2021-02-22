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
            if (args == null)
                return null;
            
            Type keyType = args[0];
            if (keyType != typeof(string)) // Support only Dictionary with key type: string
                return TypeNotSupportedMatcher.CreateTypeNotSupported(config, type, "Dictionary only support string as key type");
            Type elementType = args[1];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            if (constructor == null) {
                if (type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    constructor = ReflectUtils.GetDefaultConstructor(typeof(Dictionary<,>).MakeGenericType(keyType, elementType));
                else
                    throw new NotSupportedException("not default constructor for type: " + type);
            }
            object[] constructorParams = {config, type, constructor};
            // return new DictionaryMapper<TElm>  (config, type, constructor);
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(DictionaryMapper<,>), new[] {type, elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DictionaryMapper<TMap, TElm> : CollectionMapper<TMap, TElm> where TMap : IDictionary<string, TElm>
    {
        public override string DataTypeName() { return "Dictionary"; }
        
        public DictionaryMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, typeof(TElm), 1, typeof(string), constructor) {
        }

        public override void Write(ref Writer writer, TMap map) {
            int startLevel = writer.IncLevel();

            writer.bytes.AppendChar('{');
            int n = 0;

            foreach (var entry in map) {
                writer.WriteDelimiter(n++);
                writer.WriteString(entry.Key);
                writer.bytes.AppendChar(':');
                
                var elemVar = entry.Value;
                if (EqualityComparer<TElm>.Default.Equals(elemVar, default)) {
                    writer.AppendNull();
                } else {
                    elementType.Write(ref writer, elemVar);
                    writer.FlushFilledBuffer();
                }
            }
            if (writer.pretty)
                writer.IndentEnd();
            writer.bytes.AppendChar('}');
            writer.DecLevel(startLevel);
        }
        
        public override TMap Read(ref Reader reader, TMap map, out bool success) {
            if (!reader.StartObject(this, out success))
                return default;

            if (EqualityComparer<TMap>.Default.Equals(map, default))
                map = (TMap) CreateInstance(null);

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
                        elemVar = elementType.Read(ref reader, elemVar, out success);
                        if (!success)
                            return default;
                        map[key] = elemVar;
                        break;
                    case JsonEvent.ValueNull:
                        if (!elementType.isNullable) {
                            reader.ErrorIncompatible<Dictionary<string, TElm>>("Dictionary value", elementType, out success);
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
                        return reader.ErrorMsg<TMap>("unexpected state: ", ev, out success);
                }
            }
        }
    }
}