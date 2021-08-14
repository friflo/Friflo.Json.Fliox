// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal struct SetIntern<T> where T : Entity
    {
        internal readonly   TypeMapper<T>           typeMapper;
        internal readonly   ObjectMapper            jsonMapper;
        internal readonly   ObjectPatcher           objectPatcher;
        internal readonly   Tracer                  tracer;
        internal readonly   EntityStore             store;
        internal readonly   FieldInfo               idField;
        internal readonly   Func  <T,      string>  idPropertyGet;
        internal readonly   Action<T,      string>  idPropertySet;

        
        // --- non readonly
        internal            SubscribeChanges    subscription;

        internal SetIntern(EntityStore store) {
            jsonMapper      = store._intern.jsonMapper;
            typeMapper      = (TypeMapper<T>)store._intern.typeStore.GetTypeMapper(typeof(T));
            objectPatcher   = store._intern.objectPatcher;
            tracer          = new Tracer(store._intern.typeCache, store);
            this.store      = store;
            subscription    = null;
            var id          = EntityId.GetEntityId<T>();
            idField         = id.field;
            idPropertyGet   = id.propertyGet;
            idPropertySet   = id.propertySet;
        }
    }
    
    internal class EntityId {
        private static readonly   Dictionary<Type, EntityId> Ids = new Dictionary<Type, EntityId>();

        internal static EntityId<T> GetEntityId<T> () {
            var type = typeof(T);
            if (Ids.TryGetValue(type, out var id)) {
                return (EntityId<T>)id;
            }
            var result = new EntityId<T>(); 
            Ids[type] = result;
            return result;
        }
    }
    
    internal class EntityId<T> : EntityId {
        internal readonly   FieldInfo               field;
        internal readonly   Func  <T,      string>  propertyGet;
        internal readonly   Action<T,      string>  propertySet;

        internal EntityId() {
            var type = typeof(T);
            field         = type.GetField("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) {
                propertySet = null;
                propertyGet = null;
            } else {
                var property = type.GetProperty("id");
                if (property == null)
                    throw new InvalidOperationException($"id not found. type: {type}");
                var idGetMethod = property.GetGetMethod(true);    
                var idSetMethod = property.GetSetMethod(true);
                propertyGet = (Func  <T, string>) Delegate.CreateDelegate (typeof(Func<T, string>),   idGetMethod);
                propertySet = (Action<T, string>) Delegate.CreateDelegate (typeof(Action<T, string>), idSetMethod);
            } 
        }
    } 
}
