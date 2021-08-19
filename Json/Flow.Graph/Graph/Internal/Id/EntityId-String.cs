// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    internal class EntityIdStringField<T> : EntityId<T, string> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, string>   fieldGet;
        private  readonly   Action<T, string>   fieldSet;
        
        internal override   Type                GetEntityIdType () => typeof(string);
        
        internal EntityIdStringField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, string>(field);
            fieldSet    = GetFieldSet<T, string>(field);
        }

        internal override string StringToKey(string id) {
            return id;
        }

        internal override string KeyToString(string id) {
            return id;
        }
        
        internal override   string  GetId (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetId (T entity, string id) {
            fieldSet(entity, id);
        }
    }
    

    internal class EntityIdStringProperty<T> : EntityId<T, string> where T : class {
        private  readonly   Func  <T, string>   propertyGet;
        private  readonly   Action<T, string>   propertySet;
        
        internal override   Type                GetEntityIdType () => typeof(string);
        
        internal EntityIdStringProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, string>) Delegate.CreateDelegate (typeof(Func<T, string>),   idGetMethod);
            propertySet = (Action<T, string>) Delegate.CreateDelegate (typeof(Action<T, string>), idSetMethod);
        }

        internal override string StringToKey(string id) {
            return id;
        }

        internal override string KeyToString(string id) {
            return id;
        }
        
        internal override   string  GetId (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetId (T entity, string id) {
            propertySet(entity, id);
        }
    }
}