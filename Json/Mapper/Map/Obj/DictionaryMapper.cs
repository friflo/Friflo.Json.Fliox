// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Diff;
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
        public  override    string      DataTypeName() { return "Dictionary"; }
        public  override    bool        IsArray => false;
        
        public DictionaryMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, typeof(TElm), 1, typeof(string), constructor) {
        }
        
        public override void Trace(Tracer tracer, TMap map) {
            foreach (var entry in map) {
                var elemVar = entry.Value;
                if (!EqualityComparer<TElm>.Default.Equals(elemVar, default)) {
                    elementType.Trace(tracer, elemVar);
                }
            }
        }
        
        public override DiffNode Diff(Differ differ, TMap left, TMap right) {
            differ.PushParent(left, right);
            foreach (var leftPair in left) {
                var leftKey   = leftPair.Key;
                var leftValue = leftPair.Value;
                differ.PushKey(elementType, leftKey);
                if (right.TryGetValue(leftKey, out TElm rightValue)) {
                    elementType.DiffObject(differ, leftValue, rightValue);
                } else {
                    differ.AddOnlyLeft(leftValue);
                }
                differ.Pop();
            }
            foreach (var rightPair in right) {
                var rightKey   = rightPair.Key;
                var rightValue = rightPair.Value;
                differ.PushKey(elementType, rightKey);
                if (!left.TryGetValue(rightKey, out TElm _)) {
                    differ.AddOnlyRight(rightValue);
                }
                differ.Pop();
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            TMap map = (TMap)obj;
            var key = patcher.GetMemberKey();
            map.TryGetValue(key, out TElm value);
            var action = patcher.Member(elementType, value, out object newValue);
            switch (action) {
                case NodeAction.Assign:
                    map[key] = (TElm) newValue;
                    break;
                case NodeAction.Remove:
                    map.Remove(key);
                    break;
                default:
                    throw new InvalidOperationException($"NodeAction not applicable: {action}");
            }
        }

        public override void Write(ref Writer writer, TMap map) {
            int startLevel = writer.IncLevel();

            writer.bytes.AppendChar('{');
            int n = 0;

            foreach (var entry in map) {
                var elemVar = entry.Value;
                if (EqualityComparer<TElm>.Default.Equals(elemVar, default)) {
                    if (writer.writeNullMembers) {
                        writer.WriteKey(entry.Key, n++);
                        writer.AppendNull();
                    }
                } else {
                    writer.WriteKey(entry.Key, n++);
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
                map = (TMap) CreateInstance();

            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        string key = reader.parser.key.ToString();
                        TElm elemVar = default;
                        elemVar = elementType.Read(ref reader, elemVar, out success);
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
                        return reader.ErrorMsg<TMap>("unexpected state: ", ev, out success);
                }
            }
        }
    }
}