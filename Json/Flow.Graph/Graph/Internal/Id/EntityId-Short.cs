// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    internal class EntityIdShortField<T> : EntityId<T, short> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, short>    fieldGet;
        private  readonly   Action<T, short>    fieldSet;
        
        internal override   Type                GetEntityIdType () => typeof(short);
        
        internal EntityIdShortField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, short>(field);
            fieldSet    = GetFieldSet<T, short>(field);
        }

        internal override short StringToKey(string id) {
            return short.Parse(id);
        }

        internal override string KeyToString(short id) {
            return id.ToString();
        }
        
        internal override   short  GetId (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetId (T entity, short id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityIdShortProperty<T> : EntityId<T, short> where T : class {
        private  readonly   Func  <T, short>    propertyGet;
        private  readonly   Action<T, short>    propertySet;
        
        internal override   Type                GetEntityIdType () => typeof(short);
        
        internal EntityIdShortProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, short>) Delegate.CreateDelegate (typeof(Func  <T, short>), idGetMethod);
            propertySet = (Action<T, short>) Delegate.CreateDelegate (typeof(Action<T, short>), idSetMethod);
        }

        internal override short StringToKey(string id) {
            return short.Parse(id);
        }

        internal override string KeyToString(short id) {
            return id.ToString();
        }
        
        internal override   short  GetId (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetId (T entity, short id) {
            propertySet(entity, id);
        }
    }
}