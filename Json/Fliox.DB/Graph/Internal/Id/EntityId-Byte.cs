// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.DB.Graph.Internal.Id
{
    internal class EntityKeyByteField<T> : EntityKey<byte, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, byte>     fieldGet;
        private  readonly   Action<T, byte>     fieldSet;
        
        internal override   Type                GetKeyType() => typeof(byte);
        internal override   string              GetKeyName() => field.Name;

        internal EntityKeyByteField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, byte>(field);
            fieldSet    = GetFieldSet<T, byte>(field);
        }

        internal override   byte  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, byte id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityKeyByteProperty<T> : EntityKey<byte, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, byte>     propertyGet;
        private  readonly   Action<T, byte>     propertySet;

        internal override   Type                GetKeyType() => typeof(byte);
        internal override   string              GetKeyName() => property.Name;

        internal EntityKeyByteProperty(PropertyInfo property, MethodInfo idGetMethod, MethodInfo idSetMethod) {
            this.property = property;
            propertyGet = (Func  <T, byte>) Delegate.CreateDelegate (typeof(Func  <T, byte>), idGetMethod);
            propertySet = (Action<T, byte>) Delegate.CreateDelegate (typeof(Action<T, byte>), idSetMethod);
        }

        internal override   byte  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, byte id) {
            propertySet(entity, id);
        }
    }
}