// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    internal class EntityIdShortField<T> : EntityId<T> where T : class {
        private readonly   FieldInfo           field;
        private readonly   Func  <T, short>    fieldGet;
        private readonly   Action<T, short>    fieldSet;
        
        internal EntityIdShortField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, short>(field);
            fieldSet    = GetFieldSet<T, short>(field);
        }
        
        internal override   string  GetEntityId (T entity) {
            // var value = (short)field.GetValue(entity);
            var value = fieldGet(entity);
            return value.ToString();
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            var value = short.Parse(id);
            fieldSet(entity, value);
            // field.SetValue(entity, value);
        }
    }
    
    
    internal class EntityIdShortProperty<T> : EntityId<T> where T : class {
        private  readonly   Func  <T, short>    propertyGet;
        private  readonly   Action<T, short>    propertySet;
        
        internal EntityIdShortProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, short>) Delegate.CreateDelegate (typeof(Func  <T, short>), idGetMethod);
            propertySet = (Action<T, short>) Delegate.CreateDelegate (typeof(Action<T, short>), idSetMethod);
        }
        
        internal override   string  GetEntityId (T entity){
            var value = propertyGet(entity);
            return value.ToString();
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            var value = short.Parse(id);
            propertySet(entity, value);
        }
    }
}