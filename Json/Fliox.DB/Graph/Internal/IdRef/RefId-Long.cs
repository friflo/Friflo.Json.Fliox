// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    internal class RefKeyLongField<T> : RefKey<long, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, long>     fieldGet;
        private  readonly   Action<T, long>     fieldSet;
        
        internal override   Type                GetKeyType() => typeof(long);
        internal override   string              GetKeyName() => field.Name;

        internal RefKeyLongField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, long>(field);
            fieldSet    = GetFieldSet<T, long>(field);
        }

        internal override long IdToKey(in JsonKey id) {
            return id.AsLong();
        }

        internal override JsonKey KeyToId(in long key) {
            return new JsonKey(key);
        }
        
        internal override   long  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, long id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class RefKeyLongProperty<T> : RefKey<long, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, long>     propertyGet;
        private  readonly   Action<T, long>     propertySet;
        
        internal override   Type                GetKeyType() => typeof(long);
        internal override   string              GetKeyName() => property.Name;

        internal RefKeyLongProperty(PropertyInfo property, MethodInfo idGetMethod, MethodInfo idSetMethod) {
            this.property = property;
            propertyGet = (Func  <T, long>) Delegate.CreateDelegate (typeof(Func  <T, long>), idGetMethod);
            propertySet = (Action<T, long>) Delegate.CreateDelegate (typeof(Action<T, long>), idSetMethod);
        }

        internal override long IdToKey(in JsonKey id) {
            return id.AsLong();
        }

        internal override JsonKey KeyToId(in long key) {
            return new JsonKey(key);
        }
        
        internal override   long  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, long id) {
            propertySet(entity, id);
        }
    }
}