// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity
{
    internal sealed class EntityKeyShortStringField<T> : EntityKeyT<ShortString, T> where T : class {
        private  readonly   FieldInfo               field;
        private  readonly   Func  <T, ShortString>  fieldGet;
        private  readonly   Action<T, ShortString>  fieldSet;
        
        internal override   Type                    GetKeyType()                => typeof(ShortString);
        internal override   string                  GetKeyName()                => field.Name;
        internal override   bool                    IsIntKey()                  => false;
        internal override   bool                    IsEntityKeyNull (T entity)  => GetKey(entity).IsNull();

        internal EntityKeyShortStringField(FieldInfo field) : base (field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, ShortString>(field);
            fieldSet    = GetFieldSet<T, ShortString>(field);
        }

        internal override   ShortString GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void        SetKey (T entity, ShortString id) {
            fieldSet(entity, id);
        }
    }
    

    internal sealed class EntityKeyShortStringProperty<T> : EntityKeyT<ShortString, T> where T : class {
        private  readonly   PropertyInfo            property;
        private  readonly   Func  <T, ShortString>  propertyGet;
        private  readonly   Action<T, ShortString>  propertySet;
        
        internal override   Type                    GetKeyType()                => typeof(ShortString);
        internal override   string                  GetKeyName()                => property.Name;
        internal override   bool                    IsIntKey()                  => false;
        internal override   bool                    IsEntityKeyNull (T entity)  => GetKey(entity).IsNull();

        internal EntityKeyShortStringProperty(PropertyInfo property) : base (property) {
            this.property = property;
            propertyGet = GetPropertyGet<T, ShortString>(property);
            propertySet = GetPropertySet<T, ShortString>(property);
        }

        internal override   ShortString GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void        SetKey (T entity, ShortString id) {
            propertySet(entity, id);
        }
    }
}