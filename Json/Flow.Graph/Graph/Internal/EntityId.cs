// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class EntityId {
        private static readonly   Dictionary<Type, EntityId> Ids = new Dictionary<Type, EntityId>();

        internal static EntityId<T> GetEntityId<T> () where T : class {
            var type = typeof(T);
            if (Ids.TryGetValue(type, out var id)) {
                return (EntityId<T>)id;
            }
            var result = new EntityId<T>(); 
            Ids[type] = result;
            return result;
        }
    }
    
    internal class EntityId<T> : EntityId where T : class {
        internal readonly   FieldInfo           field;
        internal readonly   Func  <T, string>   propertyGet;
        internal readonly   Action<T, string>   propertySet;

        internal EntityId() {
            var propertyName = "id";
            var type = typeof(T);
            field         = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) {
                propertySet = null;
                propertyGet = null;
            } else {
                var property = type.GetProperty(propertyName);
                if (property == null)
                    throw new InvalidOperationException($"id not found. type: {type}");
                var idGetMethod = property.GetGetMethod(true);    
                var idSetMethod = property.GetSetMethod(true);
                propertyGet = (Func  <T, string>) Delegate.CreateDelegate (typeof(Func<T, string>),   idGetMethod);
                propertySet = (Action<T, string>) Delegate.CreateDelegate (typeof(Action<T, string>), idSetMethod);
            } 
        }
        
        private void SetEntityId (T entity, string id) {
            if (field != null) {
                field.SetValue(entity, id);
                return;
            }
            propertySet(entity, id);
        }
        
        internal string GetEntityId (T entity) {
            if (field != null) {
                return (string)field.GetValue(entity);
            }
            return propertyGet(entity);
        }
    } 
}