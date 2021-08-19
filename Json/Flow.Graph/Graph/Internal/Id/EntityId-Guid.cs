// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    internal class EntityIdGuidField<T> : EntityId<T, Guid> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, Guid>     fieldGet;
        private  readonly   Action<T, Guid>     fieldSet;
        
        internal override   Type                GetEntityIdType () => typeof(Guid);
        
        internal EntityIdGuidField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, Guid>(field);
            fieldSet    = GetFieldSet<T, Guid>(field);
        }

        internal override Guid IdToKey(string id) {
            return new Guid(id);
        }

        internal override string KeyToId(Guid key) {
            return key.ToString();
        }

        internal override   Guid  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, Guid id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityIdGuidProperty<T> : EntityId<T, Guid> where T : class {
        private  readonly   Func  <T, Guid>     propertyGet;
        private  readonly   Action<T, Guid>     propertySet;
        
        internal override   Type                GetEntityIdType () => typeof(Guid);
        
        internal EntityIdGuidProperty(MethodInfo idGetMethod, MethodInfo idSetMethod) {
            propertyGet = (Func  <T, Guid>) Delegate.CreateDelegate (typeof(Func  <T, Guid>), idGetMethod);
            propertySet = (Action<T, Guid>) Delegate.CreateDelegate (typeof(Action<T, Guid>), idSetMethod);
        }

        internal override Guid IdToKey(string id) {
            return new Guid(id);
        }

        internal override string KeyToId(Guid key) {
            return key.ToString();
        }
        
        internal override   Guid  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, Guid id) {
            propertySet(entity, id);
        }
    }
}