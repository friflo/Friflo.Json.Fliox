// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class EntityIdIntField<T> : EntityId<T> where T : class {
        private readonly   FieldInfo           field;
        private readonly   Func  <T, int>      fieldGet;
        private readonly   Action<T, int>      fieldSet;
        
        internal EntityIdIntField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, int>(field);
            fieldSet    = GetFieldSet<T, int>(field);
        }
        
        internal override   string  GetEntityId (T entity) {
            // var value = (int)field.GetValue(entity);
            var value = fieldGet(entity);
            return value.ToString();
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            var value = int.Parse(id);
            fieldSet(entity, value);
            // field.SetValue(entity, value);
        }
    }
    
    
    internal class EntityIdIntProperty<T> : EntityId<T> where T : class {
        private  readonly   Func  <T, int>      propertyGet;
        private  readonly   Action<T, int>      propertySet;
        
        internal EntityIdIntProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, int>) Delegate.CreateDelegate (typeof(Func  <T, int>), idGetMethod);
            propertySet = (Action<T, int>) Delegate.CreateDelegate (typeof(Action<T, int>), idSetMethod);
        }
        
        internal override   string  GetEntityId (T entity){
            var value = propertyGet(entity);
            return value.ToString();
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            var value = int.Parse(id);
            propertySet(entity, value);
        }
    }
}