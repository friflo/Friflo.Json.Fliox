// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    internal class EntityIdIntField<T> : EntityId<T, int> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, int>      fieldGet;
        private  readonly   Action<T, int>      fieldSet;
        
        internal override   Type                GetEntityIdType () => typeof(int);
        
        internal EntityIdIntField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, int>(field);
            fieldSet    = GetFieldSet<T, int>(field);
        }

        internal override int StringToKey(string id) {
            return int.Parse(id);
        }

        internal override string KeyToString(int id) {
            return id.ToString();
        }
        
        internal override   int  GetId (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetId (T entity, int id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityIdIntProperty<T> : EntityId<T, int> where T : class {
        private  readonly   Func  <T, int>      propertyGet;
        private  readonly   Action<T, int>      propertySet;

        internal override   Type                GetEntityIdType () => typeof(int);

        
        internal EntityIdIntProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, int>) Delegate.CreateDelegate (typeof(Func  <T, int>), idGetMethod);
            propertySet = (Action<T, int>) Delegate.CreateDelegate (typeof(Action<T, int>), idSetMethod);
        }

        internal override int StringToKey(string id) {
            return int.Parse(id);
        }

        internal override string KeyToString(int id) {
            return id.ToString();
        }
        
        internal override   int  GetId (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetId (T entity, int id) {
            propertySet(entity, id);
        }
    }
}