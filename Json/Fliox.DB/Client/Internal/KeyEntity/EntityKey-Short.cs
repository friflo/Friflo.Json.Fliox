// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.DB.Client.Internal.KeyEntity
{
    internal sealed class EntityKeyShortField<T> : EntityKeyT<short, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, short>    fieldGet;
        private  readonly   Action<T, short>    fieldSet;
        
        internal override   Type                GetKeyType() => typeof(short);
        internal override   string              GetKeyName() => field.Name;

        internal EntityKeyShortField(FieldInfo field) : base (field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, short>(field);
            fieldSet    = GetFieldSet<T, short>(field);
        }

        internal override   short  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, short id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal sealed class EntityKeyShortProperty<T> : EntityKeyT<short, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, short>    propertyGet;
        private  readonly   Action<T, short>    propertySet;
        
        internal override   Type                GetKeyType() => typeof(short);
        internal override   string              GetKeyName() => property.Name;

        internal EntityKeyShortProperty(PropertyInfo property, MethodInfo idGetMethod, MethodInfo idSetMethod) : base (property) {
            this.property = property;
            propertyGet = (Func  <T, short>) Delegate.CreateDelegate (typeof(Func  <T, short>), idGetMethod);
            propertySet = (Action<T, short>) Delegate.CreateDelegate (typeof(Action<T, short>), idSetMethod);
        }

        internal override   short  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, short id) {
            propertySet(entity, id);
        }
    }
}