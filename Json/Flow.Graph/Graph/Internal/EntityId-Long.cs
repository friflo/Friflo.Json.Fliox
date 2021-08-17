// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class EntityIdLongField<T> : EntityId<T> where T : class {
        private readonly   FieldInfo           field;
        
        internal EntityIdLongField(FieldInfo field) {
            this.field = field;
        }
        
        internal override   string  GetEntityId (T entity) {
            var value = (long)field.GetValue(entity);
            return value.ToString();
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            var value = long.Parse(id);
            field.SetValue(entity, value);
        }
    }
    
    
    internal class EntityIdLongProperty<T> : EntityId<T> where T : class {
        private  readonly   Func  <T, long>      propertyGet;
        private  readonly   Action<T, long>      propertySet;
        
        internal EntityIdLongProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, long>) Delegate.CreateDelegate (typeof(Func  <T, long>), idGetMethod);
            propertySet = (Action<T, long>) Delegate.CreateDelegate (typeof(Action<T, long>), idSetMethod);
        }
        
        internal override   string  GetEntityId (T entity){
            var value = propertyGet(entity);
            return value.ToString();
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            var value = long.Parse(id);
            propertySet(entity, value);
        }
    }
}