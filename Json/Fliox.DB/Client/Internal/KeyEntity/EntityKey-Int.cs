// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.DB.Client.Internal.KeyEntity
{
    internal class EntityKeyIntField<T> : EntityKeyT<int, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, int>      fieldGet;
        private  readonly   Action<T, int>      fieldSet;
        
        internal override   Type                GetKeyType()            => typeof(int);
        internal override   string              GetKeyName()            => field.Name;
        internal override   bool                IsDefaultKey(T entity)  => GetKey(entity) == default;

        internal EntityKeyIntField(FieldInfo field) : base (field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, int>(field);
            fieldSet    = GetFieldSet<T, int>(field);
        }

        internal override   int  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, int id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityKeyIntProperty<T> : EntityKeyT<int, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, int>      propertyGet;
        private  readonly   Action<T, int>      propertySet;

        internal override   Type                GetKeyType() => typeof(int);
        internal override   string              GetKeyName() => property.Name;

        internal EntityKeyIntProperty(PropertyInfo property, MethodInfo idGetMethod, MethodInfo idSetMethod) : base (property) {
            this.property = property;
            propertyGet = (Func  <T, int>) Delegate.CreateDelegate (typeof(Func  <T, int>), idGetMethod);
            propertySet = (Action<T, int>) Delegate.CreateDelegate (typeof(Action<T, int>), idSetMethod);
        }

        internal override   int  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, int id) {
            propertySet(entity, id);
        }
    }
}