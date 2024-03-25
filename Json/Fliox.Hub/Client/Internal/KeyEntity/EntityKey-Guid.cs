// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity
{
    internal sealed class EntityKeyGuidField<T> : EntityKeyT<Guid, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, Guid>     fieldGet;
        private  readonly   Action<T, Guid>     fieldSet;
        
        internal override   Type                GetKeyType() => typeof(Guid);
        internal override   string              GetKeyName() => field.Name;
        internal override   bool                IsIntKey()   => false;

        internal EntityKeyGuidField(FieldInfo field) : base (field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, Guid>(field);
            fieldSet    = GetFieldSet<T, Guid>(field);
        }

        internal override   Guid  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, Guid id) {
            fieldSet(entity, id);
        }
    }
    
    internal sealed class EntityKeyGuidProperty<T> : EntityKeyT<Guid, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, Guid>     propertyGet;
        private  readonly   Action<T, Guid>     propertySet;
        
        internal override   Type                GetKeyType() => typeof(Guid);
        internal override   string              GetKeyName() => property.Name;
        internal override   bool                IsIntKey()   => false;

        internal EntityKeyGuidProperty(PropertyInfo property) : base (property) {
            this.property = property;
            propertyGet = GetPropertyGet<T, Guid>(property);
            propertySet = GetPropertySet<T, Guid>(property);
        }

        internal override   Guid  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, Guid id) {
            propertySet(entity, id);
        }
    }
}