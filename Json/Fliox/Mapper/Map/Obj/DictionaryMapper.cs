// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Obj
{
    internal sealed class DictionaryMatcher : ITypeMatcher {
        public static readonly DictionaryMatcher Instance = new DictionaryMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // don't handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
            if (args == null)
                return null;
            
            Type keyType = args[0];
            if (!config.keyMappers.ContainsKey(keyType))
                return null;
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
            var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(DictionaryMapper<,,>), new[] {type, keyType, elementType}, constructorParams);
            return (TypeMapper) newInstance;
        }
    }
    
    internal sealed class DictionaryMapper<TMap, TKey, TElm> : CollectionMapper<TMap, TElm> where TMap : IDictionary<TKey, TElm>
    {
        private readonly    KeyMapper<TKey> keyMapper;
        
        public  override    string          DataTypeName() { return "Dictionary"; }
        public  override    bool            IsArray         => false;
        public  override    bool            IsDictionary    => true;
        
        public DictionaryMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, typeof(TElm), 1, typeof(string), constructor)
        {
            keyMapper       = (KeyMapper<TKey>)config.keyMappers[typeof(TKey)];
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
                var leftJson = keyMapper.ToJsonKey(leftKey);
                differ.PushKey(elementType, leftJson);
                if (right.TryGetValue(leftKey, out TElm rightValue)) {
                    elementType.DiffObject(differ, leftValue, rightValue);
                } else {
                    differ.AddOnlyLeft(leftValue);
                }
                differ.Pop();
            }
            foreach (var rightPair in right) {
                var rightKey    = rightPair.Key;
                var rightValue  = rightPair.Value;
                var rightJson   = keyMapper.ToJsonKey(rightKey);
                differ.PushKey(elementType, rightJson);
                if (!left.TryGetValue(rightKey, out TElm _)) {
                    differ.AddOnlyRight(rightValue);
                }
                differ.Pop();
            }
            return differ.PopParent();
        }
        
        public override void PatchObject(Patcher patcher, object obj) {
            TMap map = (TMap)obj;
            var jsonKey = patcher.GetMemberKey();
            var key = keyMapper.ToKey(jsonKey); 
            map.TryGetValue(key, out TElm value);
            var action = patcher.DescendMember(elementType, value, out object newValue);
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
                        writer.WriteKey(keyMapper, entry.Key, n++);
                        writer.AppendNull();
                    }
                } else {
                    writer.WriteKey(keyMapper, entry.Key, n++);
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

            if (map == null) {
                map = (TMap) CreateInstance();
            } else {
                map.Clear();
            }
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                    case JsonEvent.ValueNull:
                        TKey key = keyMapper.ReadKey(ref reader, out success);
                        if (!success)
                            return default;
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