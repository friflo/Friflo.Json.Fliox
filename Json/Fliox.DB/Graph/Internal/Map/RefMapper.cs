// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.DB.Graph.Internal.KeyEntity;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.DB.Graph.Internal.Map
{
    // -------------------------------------------------------------------------------------
    internal class RefMatcher : ITypeMatcher {
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // doesnt handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(Ref<,>) );
            if (args == null)
                return null;
            
            Type keyType    = args[0];
            Type entityType = args[1];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            
            object[] constructorParams = {config, type, constructor};
            // new RefMapper<T>(config, type, constructor);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(RefMapper<,>), new[] {keyType, entityType}, constructorParams);
        }
    }

    internal class RefMapper<TKey, T> : TypeMapper<Ref<TKey, T>> where T : class
    {
        private             TypeMapper<TKey>    keyMapper;
        private             TypeMapper<T>       entityMapper;
        
        public  override    string              DataTypeName()          { return "Ref<>"; }
        public  override    TypeMapper          GetUnderlyingMapper()   => keyMapper;
        public  override    TypeSemantic        GetTypeSemantic     ()  => TypeSemantic.Reference;

        // ReSharper disable once UnusedParameter.Local
        public RefMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, false, true)
        { }

        public override void InitTypeMapper(TypeStore typeStore) {
            keyMapper               = (TypeMapper<TKey>)    typeStore.GetTypeMapper(typeof(TKey));
            entityMapper            = (TypeMapper<T>)       typeStore.GetTypeMapper(typeof(T));
            var entityKeyType       = EntitySetBase<T>.EntityKeyMap.GetKeyType();
            var keyType             = typeof(TKey);
            // TAG_NULL_REF
            var underlyingKeyType   = Nullable.GetUnderlyingType(keyType);
            if (underlyingKeyType != null) {
                keyType = underlyingKeyType;
            }
            if (keyType != entityKeyType) {
                var entityName = typeof(T).Name;
                var msg = $"Ref<{typeof(TKey).Name}, {entityName}> != EntitySet<{entityKeyType.Name}, {entityName}>";
                throw new InvalidTypeException(msg);
            }
        }

        /*
        private TypeMapper<T> GetEntityMapper(TypeCache typeCache) {
            if (entityMapper == null)
               entityMapper = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            return entityMapper;
        } */

        public override DiffNode Diff (Differ differ, Ref<TKey, T> left, Ref<TKey, T> right) {
            // if (!left.id.IsEqual(right.id))
            if (!left.IsEqual(right))
                return differ.AddNotEqual(left.key, right.key);
            return null;
        }
        
        public override void Trace(Tracer tracer, Ref<TKey, T> value) {
            if (value.IsKeyNull())
                return;
            var store = tracer.tracerContext.Store();
            var set = store.GetEntitySet<TKey, T>();
            if (!set.GetPeerByRef(value, out Peer<T> peer)) {
                throw new InvalidOperationException($"peer not found: {peer}");     
            }
            if (peer.assigned)
                return;
            // Track untracked entity
            var entity = peer.NullableEntity;
            if (entity == null)
                return;  // todo add test
            var syncSet = set.GetSyncSet();
            if (syncSet.AddCreate(peer))
                store._intern.tracerLogTask.AddCreate(syncSet, peer.id);
            // var mapper = GetEntityMapper(tracer.typeCache);
            entityMapper.Trace(tracer, entity);
        }

        public override void Write(ref Writer writer, Ref<TKey, T> value) {
            if (value.key != null) {
                keyMapper.Write(ref writer, value.key);
            } else {
                writer.AppendNull();
            }
        }

        public override Ref<TKey, T> Read(ref Reader reader, Ref<TKey, T> slot, out bool success) {
            var ev = reader.parser.Event;
            if (ev == JsonEvent.ValueNull) {
                success = true;
                return default;
            }
            TKey key = keyMapper.Read(ref reader, default, out success);
            if (success) {
                if (reader.tracerContext != null) {
                    var store   = reader.tracerContext.Store();
                    var set     = store.GetEntitySetBase<T>();
                    var id      = Ref<TKey, T>.RefKeyMap.KeyToId(key);  // TAG_NULL_REF
                    var peer    = set.GetOrCreatePeerById(id);
                    return new Ref<TKey, T> (peer);
                }
                slot = new Ref<TKey, T> (key);
                return slot;
            }
            return default;
        }
    }
}