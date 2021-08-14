// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal
{
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
        
        private void SetEntityId (object entity, string id) {
            if (field != null) {
                field.SetValue(entity, id);
                return;
            }
            var typedEntity = (T)entity; // unbox.any - entity is a always reference type -> no unboxing
            propertySet(typedEntity, id);
        }
        
        internal string GetEntityId (object entity) {
            if (field != null) {
                return (string)field.GetValue(entity);
            }
            var typedEntity = (T)entity; // unbox.any - entity is a always reference type -> no unboxing 
            return propertyGet(typedEntity);
        }
    } 
}