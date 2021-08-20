// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    internal class EntityKeyLongField<T> : EntityKey<long, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, long>     fieldGet;
        private  readonly   Action<T, long>     fieldSet;

        internal EntityKeyLongField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, long>(field);
            fieldSet    = GetFieldSet<T, long>(field);
        }

        internal override long IdToKey(string id) {
            return long.Parse(id);
        }

        internal override string KeyToId(long key) {
            return key.ToString();
        }
        
        internal override   long  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, long id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityKeyLongProperty<T> : EntityKey<long, T> where T : class {
        private  readonly   Func  <T, long>     propertyGet;
        private  readonly   Action<T, long>     propertySet;

        internal EntityKeyLongProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, long>) Delegate.CreateDelegate (typeof(Func  <T, long>), idGetMethod);
            propertySet = (Action<T, long>) Delegate.CreateDelegate (typeof(Action<T, long>), idSetMethod);
        }

        internal override long IdToKey(string id) {
            return long.Parse(id);
        }

        internal override string KeyToId(long key) {
            return key.ToString();
        }
        
        internal override   long  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, long id) {
            propertySet(entity, id);
        }
    }
}