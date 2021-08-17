// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class EntityIdStringField<T> : EntityId<T> where T : class {
        private readonly   FieldInfo           field;
        
        internal EntityIdStringField(FieldInfo field) {
            this.field = field;
        }
        
        internal override   string  GetEntityId (T entity) {
            return (string)field.GetValue(entity);
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            field.SetValue(entity, id);
        }
    }
    

    internal class EntityIdStringProperty<T> : EntityId<T> where T : class {
        private  readonly   Func  <T, string>   propertyGet;
        private  readonly   Action<T, string>   propertySet;
        
        internal EntityIdStringProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, string>) Delegate.CreateDelegate (typeof(Func<T, string>),   idGetMethod);
            propertySet = (Action<T, string>) Delegate.CreateDelegate (typeof(Action<T, string>), idSetMethod);
        }
        
        internal override   string  GetEntityId (T entity){
            return propertyGet(entity);
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            propertySet(entity, id);
        }
    }
}