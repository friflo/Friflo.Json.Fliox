﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity
{
    internal sealed class EntityKeyStringField<T> : EntityKeyT<string, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, string>   fieldGet;
        private  readonly   Action<T, string>   fieldSet;
        
        internal override   Type                GetKeyType()                => typeof(string);
        internal override   string              GetKeyName()                => field.Name;
        internal override   bool                IsIntKey()                  => false;
        internal override   bool                IsEntityKeyNull (T entity)  => GetKey(entity) == null;

        internal EntityKeyStringField(FieldInfo field) : base (field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, string>(field);
            fieldSet    = GetFieldSet<T, string>(field);
        }

        internal override   string  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, string id) {
            fieldSet(entity, id);
        }
    }
    

    internal sealed class EntityKeyStringProperty<T> : EntityKeyT<string, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, string>   propertyGet;
        private  readonly   Action<T, string>   propertySet;
        
        internal override   Type                GetKeyType()                => typeof(string);
        internal override   string              GetKeyName()                => property.Name;
        internal override   bool                IsIntKey()                  => false;
        internal override   bool                IsEntityKeyNull (T entity)  => GetKey(entity) == null;

        internal EntityKeyStringProperty(PropertyInfo property) : base (property) {
            this.property = property;
            propertyGet = GetPropertyGet<T, string>(property);
            propertySet = GetPropertySet<T, string>(property);
        }

        internal override   string  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, string id) {
            propertySet(entity, id);
        }
    }
}