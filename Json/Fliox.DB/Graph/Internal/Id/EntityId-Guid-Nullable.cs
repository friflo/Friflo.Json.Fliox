// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Graph.Internal.Id
{
    internal class EntityKeyGuidNullField<T> : EntityKey<Guid?, T> where T : class {
        private  readonly   FieldInfo           field;
        private  readonly   Func  <T, Guid?>    fieldGet;
        private  readonly   Action<T, Guid?>    fieldSet;
        
        internal override   Type                GetKeyType() => typeof(Guid?);
        internal override   string              GetKeyName() => field.Name;
        internal override   bool                IsEntityKeyNull (T entity)  => !GetKey(entity).HasValue;
        internal override   bool                IsKeyNull (Guid? key)       => key == null;

        internal EntityKeyGuidNullField(FieldInfo field) {
            this.field  = field;
            fieldGet    = GetFieldGet<T, Guid?>(field);
            fieldSet    = GetFieldSet<T, Guid?>(field);
        }

        internal override Guid? IdToKey(in JsonKey id) {
            return id.AsGuidNullable();
        }

        internal override JsonKey KeyToId(in Guid? key) {
            return new JsonKey(key);
        }

        internal override   Guid?  GetKey (T entity) {
            return fieldGet(entity);
        }
        
        internal override   void    SetKey (T entity, Guid? id) {
            fieldSet(entity, id);
        }
    }
    
    
    internal class EntityKeyGuidNullProperty<T> : EntityKey<Guid?, T> where T : class {
        private  readonly   PropertyInfo        property;
        private  readonly   Func  <T, Guid?>    propertyGet;
        private  readonly   Action<T, Guid?>    propertySet;
        
        internal override   Type                GetKeyType() => typeof(Guid?);
        internal override   string              GetKeyName() => property.Name;
        internal override   bool                IsEntityKeyNull (T entity)  => !GetKey(entity).HasValue;
        internal override   bool                IsKeyNull (Guid? key)       => key == null;


        internal EntityKeyGuidNullProperty(PropertyInfo property, MethodInfo idGetMethod, MethodInfo idSetMethod) {
            this.property = property;
            propertyGet = (Func  <T, Guid?>) Delegate.CreateDelegate (typeof(Func  <T, Guid?>), idGetMethod);
            propertySet = (Action<T, Guid?>) Delegate.CreateDelegate (typeof(Action<T, Guid?>), idSetMethod);
        }

        internal override Guid? IdToKey(in JsonKey id) {
            return id.AsGuidNullable();
        }

        internal override JsonKey KeyToId(in Guid? key) {
            return new JsonKey(key);
        }
        
        internal override   Guid?  GetKey (T entity) {
            return propertyGet(entity);
        }
        
        internal override   void    SetKey (T entity, Guid? id) {
            propertySet(entity, id);
        }
    }
}