// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity
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

        internal EntityKeyShortProperty(PropertyInfo property) : base (property) {
            this.property = property;
            propertyGet = GetPropertyGet<T, short>(property);
            propertySet = GetPropertySet<T, short>(property);
        }

        internal override   short  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, short id) {
            propertySet(entity, id);
        }
    }
}