// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Access;
using Friflo.Json.Flow.Mapper.Diff;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Obj;
using Friflo.Json.Flow.Mapper.Map.Utils;
using Friflo.Json.Flow.Mapper.Utils;
using Friflo.Json.Flow.Transform.Select;

namespace Friflo.Json.Flow.Graph.Internal.Map
{
    // -------------------------------------------------------------------------------------
    
    public class EntityMatcher : ITypeMatcher {
        public static readonly EntityMatcher Instance = new EntityMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // doesnt handle standard types
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
        
        public override string          DataTypeName()   { return "Entity"; }
        public override TypeMapper      GetUnderlyingMapper()   => mapper;
        public override TypeSemantic    GetTypeSemantic     ()  => TypeSemantic.Entity;

        // ReSharper disable once UnusedParameter.Local
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
        
        public override void PatchObject(Patcher patcher, object obj) {
            mapper.PatchObject(patcher, obj);
        }

        public override void MemberObject(Accessor accessor, object obj, PathNode<MemberValue> node) {
            mapper.MemberObject(accessor, obj, node);
        }

        public override DiffNode Diff (Differ differ, T left, T right) {
            DiffNode diff = mapper.Diff(differ, left, right);
            return diff;
        }
        
        public override void Trace(Tracer tracer, T value) {
            if (value != null) {
                mapper.Trace(tracer, value);
            }
        }
        
        public override void Write(ref Writer writer, T value) {
            if (writer.tracerContext != null && writer.Level > 0) {
                if (value != null) {
                    writer.WriteString(value.id);
                } else {
                    writer.AppendNull();
                }
            } else {
                if (value != null) {
                    mapper.WriteObject(ref writer, value);
                } else {
                    writer.AppendNull();
                }
            }
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            var entityStore = reader.tracerContext;
            if (entityStore != null && reader.parser.Level > 0) {
                if (reader.parser.Event == JsonEvent.ValueString) {
                    var id = reader.parser.value.ToString();
                    var store = reader.tracerContext.Store();
                    var set = store.GetEntitySet<T>();
                    success = true;
                    var peer = set.GetPeerById(id);
                    throw new InvalidOperationException("Currently unused - not sure, if required");
                    // return peer.Entity;
                }
                T entity = mapper.Read(ref reader, slot, out success);                
                return entity;
            }
            return mapper.Read(ref reader, slot, out success);
        }
    }
    
}