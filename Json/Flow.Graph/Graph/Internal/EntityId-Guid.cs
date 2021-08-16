// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class EntityIdGuidField<T> : EntityId<T> where T : class {
        private readonly   FieldInfo           field;
        
        internal EntityIdGuidField(FieldInfo field) {
            this.field = field;
        }
        
        internal override   string  GetEntityId (T entity) {
            var guid = (Guid)field.GetValue(entity);
            return guid.ToString();
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            var guid = new Guid(id);
            field.SetValue(entity, guid);
        }
    }
    
    
    internal class EntityIdGuidProperty<T> : EntityId<T> where T : class {
        private  readonly   Func  <T, Guid>   propertyGet;
        private  readonly   Action<T, Guid>   propertySet;
        
        internal EntityIdGuidProperty(PropertyInfo property) {
            var idGetMethod = property.GetGetMethod(true);    
            var idSetMethod = property.GetSetMethod(true);
            propertyGet = (Func  <T, Guid>) Delegate.CreateDelegate (typeof(Func  <T, Guid>), idGetMethod);
            propertySet = (Action<T, Guid>) Delegate.CreateDelegate (typeof(Action<T, Guid>), idSetMethod);
        }
        
        internal override   string  GetEntityId (T entity){
            var guid = propertyGet(entity);
            return guid.ToString();
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            var guid = new Guid(id);
            propertySet(entity, guid);
        }
    }
}