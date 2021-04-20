// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Diff;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Utils;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.EntityGraph.Map
{
    // -------------------------------------------------------------------------------------
    public class RefMatcher : ITypeMatcher {
        public static readonly RefMatcher Instance = new RefMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(Ref<>) );
            if (args == null)
                return null;
            
            Type refType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            
            object[] constructorParams = {config, type, constructor};
            // new RefMapper<T>(config, type, constructor);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(RefMapper<>), new[] {refType}, constructorParams);
        }
    }

    internal class RefMapper<T> : TypeMapper<Ref<T>> where T : Entity
    {
        private TypeMapper<T> entityMapper;
        
        public override string DataTypeName() { return "Ref<>"; }

        public RefMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, true, true)
        {
        }

        private TypeMapper<T> GetEntityMapper(TypeCache typeCache) {
            if (entityMapper == null)
                entityMapper = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            return entityMapper;
        }
        
        public override DiffNode Diff (Differ differ, Ref<T> left, Ref<T> right) {
            if (left.Id != right.Id)
                return differ.AddNotEqual(left.Id, right.Id);
            return null;
        }
        
        public override void Trace(Tracer tracer, Ref<T> value) {
            string id = value.Id;
            if (id == null)
                return;
            var store = tracer.tracerContext.Store();
            var set = store.EntitySet<T>();
            PeerEntity<T> peer = set.GetPeerByRef(value);
            if (peer.assigned)
                return;
            // Track untracked entity
            set.intern.sync.AddCreate(peer);
            var mapper = GetEntityMapper(tracer.typeCache);
            mapper.Trace(tracer, peer.entity);
        }

        public override void Write(ref Writer writer, Ref<T> value) {
            string id = value.Id;
            if (id != null) {
                writer.WriteString(id);
            } else {
                writer.AppendNull();
            }
        }

        public override Ref<T> Read(ref Reader reader, Ref<T> slot, out bool success) {
            if (reader.parser.Event == JsonEvent.ValueString) {
                success = true;
                string id = reader.parser.value.ToString();
                if (reader.tracerContext != null) {
                    var store = reader.tracerContext.Store();
                    var set = store.EntitySet<T>();
                    var peer = set.GetPeerById(id);
                    slot = new Ref<T> {
                        peer = peer,
                        Id   = id
                    };
                    return slot;
                }
                slot = new Ref<T> { Id = id };
                return slot;
            }
            success = false;
            return null;
        }
    }
}