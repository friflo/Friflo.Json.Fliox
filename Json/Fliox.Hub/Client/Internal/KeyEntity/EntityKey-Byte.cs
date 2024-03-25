// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity
{
    internal sealed class EntityKeyByteField<T> : EntityKeyT<byte, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, byte>     fieldGet;
        private  readonly   Action<T, byte>     fieldSet;
        
        internal override   Type                GetKeyType() => typeof(byte);
        internal override   string              GetKeyName() => field.Name;

        internal EntityKeyByteField(FieldInfo field) : base (field) {
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
    
    
    internal sealed class EntityKeyByteProperty<T> : EntityKeyT<byte, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, byte>     propertyGet;
        private  readonly   Action<T, byte>     propertySet;

        internal override   Type                GetKeyType() => typeof(byte);
        internal override   string              GetKeyName() => property.Name;

        internal EntityKeyByteProperty(PropertyInfo property) : base (property) {
            this.property = property;
            propertyGet = GetPropertyGet<T, byte>(property);
            propertySet = GetPropertySet<T, byte>(property);
        }

        internal override   byte  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, byte id) {
            propertySet(entity, id);
        }
    }
}