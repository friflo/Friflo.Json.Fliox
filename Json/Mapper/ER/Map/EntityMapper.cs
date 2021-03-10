// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.ER.Map
{
    // -------------------------------------------------------------------------------------
    
    public class EntityMatcher : ITypeMatcher {
        public static readonly EntityMatcher Instance = new EntityMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            if (!typeof(Entity).IsAssignableFrom(type))
                return null;
            
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            
            object[] constructorParams = {config, type, constructor};
            // new EntityMapper<T>(config, type, constructor, mapper);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(EntityMapper<>), new[] {type}, constructorParams);
        }
    }


    internal class EntityMapper<T> : TypeMapper<T> where T : Entity
    {
        private readonly TypeMapper<T> mapper;
        
        public override string DataTypeName() { return "Entity"; }

        public EntityMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, true, true)
        {
            mapper = (TypeMapper<T>)ClassMatcher.Instance.MatchTypeMapper(type, config);
        }
        
        public override void Dispose() {
            base.Dispose();
            mapper.Dispose();
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            mapper.InitTypeMapper(typeStore);
        }
        
        public override      object  CreateInstance() {
            return mapper.CreateInstance();
        }
        
        public override void Trace(Tracer tracer, T value) {
            if (value != null) {
                var set = tracer.entityStore.EntitySet<T>();
                set.CreatePeer(value);
            }
        }
        
        public override void Write(ref Writer writer, T value) {
            if (writer.entityStore != null && writer.Level > 0) {
                if (value != null) {
                    writer.WriteString(value.id);
                    var set = writer.entityStore.EntitySet<T>();
                    set.CreatePeer(value);
                } else {
                    writer.AppendNull();
                }
            } else {
                if (value != null) {
                    mapper.WriteObject(ref writer, value);
                    if (writer.entityStore != null) {
                        var set = writer.entityStore.EntitySet<T>();
                        set.CreatePeer(value);
                    }
                } else {
                    writer.AppendNull();
                }
            }
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            var store = reader.entityStore;
            if (store != null && reader.parser.Level > 0) {
                if (reader.parser.Event == JsonEvent.ValueString) {
                    var id = reader.parser.value.ToString();
                    var set = store.EntitySet<T>();
                    success = true;
                    var peer = set.GetPeer(id); 
                    return peer.entity;
                }
                T entity = (T) mapper.ReadObject(ref reader, slot, out success);
                return entity;
            }
            return mapper.Read(ref reader, slot, out success);
        }
    }
    
}