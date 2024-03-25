// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity
{
    internal sealed class EntityKeyJsonKeyField<T> : EntityKeyT<JsonKey, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, JsonKey>  fieldGet;
        private  readonly   Action<T, JsonKey>  fieldSet;
        
        internal override   Type                GetKeyType()                => typeof(JsonKey);
        internal override   string              GetKeyName()                => field.Name;
        internal override   bool                IsIntKey()                  => false;
        internal override   bool                IsEntityKeyNull (T entity)  => GetKey(entity).IsNull();

        internal EntityKeyJsonKeyField(FieldInfo field) : base (field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, JsonKey>(field);
            fieldSet    = GetFieldSet<T, JsonKey>(field);
        }

        internal override   JsonKey  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, JsonKey id) {
            fieldSet(entity, id);
        }
    }
    

    internal sealed class EntityKeyJsonKeyProperty<T> : EntityKeyT<JsonKey, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, JsonKey>  propertyGet;
        private  readonly   Action<T, JsonKey>  propertySet;
        
        internal override   Type                GetKeyType()                => typeof(JsonKey);
        internal override   string              GetKeyName()                => property.Name;
        internal override   bool                IsIntKey()                  => false;
        internal override   bool                IsEntityKeyNull (T entity)  => GetKey(entity).IsNull();

        internal EntityKeyJsonKeyProperty(PropertyInfo property) : base (property) {
            this.property = property;
            propertyGet = GetPropertyGet<T, JsonKey>(property);
            propertySet = GetPropertySet<T, JsonKey>(property);
        }

        internal override   JsonKey  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, JsonKey id) {
            propertySet(entity, id);
        }
    }
}