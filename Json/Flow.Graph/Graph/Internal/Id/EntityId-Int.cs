// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    internal class EntityKeyIntField<T> : EntityKey<int, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, int>      fieldGet;
        private  readonly   Action<T, int>      fieldSet;
        
        internal override   Type                GetEntityIdType () => typeof(int);
        
        internal EntityKeyIntField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, int>(field);
            fieldSet    = GetFieldSet<T, int>(field);
        }

        internal override int IdToKey(string id) {
            return int.Parse(id);
        }

        internal override string KeyToId(int key) {
            return key.ToString();
        }
        
        internal override   int  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, int id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityKeyIntProperty<T> : EntityKey<int, T> where T : class {
        private  readonly   Func  <T, int>      propertyGet;
        private  readonly   Action<T, int>      propertySet;

        internal override   Type                GetEntityIdType () => typeof(int);

        
        internal EntityKeyIntProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, int>) Delegate.CreateDelegate (typeof(Func  <T, int>), idGetMethod);
            propertySet = (Action<T, int>) Delegate.CreateDelegate (typeof(Action<T, int>), idSetMethod);
        }

        internal override int IdToKey(string id) {
            return int.Parse(id);
        }

        internal override string KeyToId(int key) {
            return key.ToString();
        }
        
        internal override   int  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, int id) {
            propertySet(entity, id);
        }
    }
}