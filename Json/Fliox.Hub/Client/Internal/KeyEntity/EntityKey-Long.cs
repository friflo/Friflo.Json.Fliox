// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity
{
    internal sealed class EntityKeyLongField<T> : EntityKeyT<long, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, long>     fieldGet;
        private  readonly   Action<T, long>     fieldSet;
        
        internal override   Type                GetKeyType() => typeof(long);
        internal override   string              GetKeyName() => field.Name;

        internal EntityKeyLongField(FieldInfo field) : base (field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, long>(field);
            fieldSet    = GetFieldSet<T, long>(field);
        }

        internal override   long  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, long id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal sealed class EntityKeyLongProperty<T> : EntityKeyT<long, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, long>     propertyGet;
        private  readonly   Action<T, long>     propertySet;
        
        internal override   Type                GetKeyType() => typeof(long);
        internal override   string              GetKeyName() => property.Name;

        internal EntityKeyLongProperty(PropertyInfo property) : base (property) {
            this.property = property;
            propertyGet = GetPropertyGet<T, long>(property);
            propertySet = GetPropertySet<T, long>(property);
        }

        internal override   long  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, long id) {
            propertySet(entity, id);
        }
    }
}