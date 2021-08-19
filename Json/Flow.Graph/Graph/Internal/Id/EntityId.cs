// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Flow.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    // -------------------------------------------- EntityId -----------------------------------------------
    internal abstract class EntityId {
        private static readonly   Dictionary<Type, EntityId> Ids = new Dictionary<Type, EntityId>();
        
        internal static EntityId<T, TKey> GetEntityId2<T, TKey> () where T : class {
            return (EntityId<T, TKey>)GetEntityId<T>();
        }
        
        internal static EntityId2<T> GetEntityId<T> () where T : class {
            var type = typeof(T);
            if (Ids.TryGetValue(type, out EntityId id)) {
                return (EntityId2<T>)id;
            }
            var member = FindKeyMember (type);
            var property = member as PropertyInfo;
            if (property != null) {
                var result  = CreateEntityIdProperty<T>(property);
                Ids[type]   = result;
                return result;
            }
            var field = member as FieldInfo;
            if (field != null) {
                var result  = CreateEntityIdField<T>(field);
                Ids[type]   = result;
                return result;
            }
            throw new InvalidOperationException($"missing entity id member. entity: {type.Name}");
        }
        
        private static MemberInfo FindKeyMember (Type type) {
            var properties = type.GetProperties(Flags);
            foreach (var p in properties) {
                var customAttributes = p.CustomAttributes;
                if (FieldQuery.IsKey(customAttributes))
                    return p;
            }
            var fields = type.GetFields(Flags);
            foreach (var f in fields) {
                var customAttributes = f.CustomAttributes;
                if (FieldQuery.IsKey(customAttributes))
                    return f;
            }
            var property = FindMember(properties);
            if (property != null)
                return property;
            
            var field = FindMember(fields);
            if (field != null)
                return field;
            
            return null;
        }
        
        private static T FindMember<T> (T[] members) where T : MemberInfo {
            foreach (var member in members) {
                if (member.Name == "id")
                    return member;
            }
            foreach (var member in members) {
                if (member.Name == "Id")
                    return member;
            }
            return null;
        }

        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        private static EntityId2<T> CreateEntityIdProperty<T> (PropertyInfo property)  where T : class {
            var type        = typeof (T);
            var propType    = property.PropertyType;
            var idGetMethod = property.GetGetMethod(true);    
            var idSetMethod = property.GetSetMethod(true);
            
            if (idGetMethod == null || idSetMethod == null) {
                var msg2 = $"entity id property must have get & set: {property.Name}, type: {propType.Name}, entity: {type.Name}";
                throw new InvalidOperationException(msg2);
            }
            if (propType == typeof(string)) {
                return new EntityIdStringProperty<T>(idGetMethod, idSetMethod);
            }
            if (propType == typeof(Guid)) {
                return new EntityIdGuidProperty<T>  (idGetMethod, idSetMethod);
            }
            if (propType == typeof(int)) {
                return new EntityIdIntProperty<T>  (idGetMethod, idSetMethod);
            }
            if (propType == typeof(long)) {
                return new EntityIdLongProperty<T>  (idGetMethod, idSetMethod);
            }
            if (propType == typeof(short)) {
                return new EntityIdShortProperty<T>  (idGetMethod, idSetMethod);
            }
            // add additional types here
            var msg = $"unsupported type for entity id. property: {property.Name}, type: {propType.Name}, entity: {type.Name}";
            throw new InvalidOperationException(msg);
        }
            
        private static EntityId2<T> CreateEntityIdField<T> (FieldInfo field)  where T : class {
            var type        = typeof (T);
            var fieldType   = field.FieldType;
            
            if (fieldType == typeof(string)) {
                return new EntityIdStringField<T>(field);
            }
            if (fieldType == typeof(Guid)) {
                return new EntityIdGuidField<T>(field);
            }
            if (fieldType == typeof(int)) {
                return new EntityIdIntField<T>(field);
            }
            if (fieldType == typeof(long)) {
                return new EntityIdLongField<T>(field);
            }
            if (fieldType == typeof(short)) {
                return new EntityIdShortField<T>(field);
            }
            // add additional types here
            var msg = $"unsupported type for entity id. field: {field.Name}, type: {fieldType.Name}, entity: {type.Name}";
            throw new InvalidOperationException(msg);
        }
        
        internal static Func<TEntity,TField> GetFieldGet<TEntity, TField>(FieldInfo field) {
            var instanceType    = field.DeclaringType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var fieldExp        = Expression.Field(instExp, field);
            return                Expression.Lambda<Func<TEntity, TField>>(fieldExp, instExp).Compile();
        }
        
        internal static Action<TEntity,TField> GetFieldSet<TEntity, TField>(FieldInfo field) {
            var instanceType    = field.DeclaringType;
            var fieldType       = field.FieldType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var valueExp        = Expression.Parameter(fieldType,       "value");
            var fieldExp        = Expression.Field(instExp, field);
            var assignExpr      = Expression.Assign (fieldExp, valueExp);
            return                Expression.Lambda<Action<TEntity, TField>>(assignExpr, instExp, valueExp).Compile();
        }
    }
    
    
    // -------------------------------------------- EntityId<T> --------------------------------------------
    internal abstract class EntityId2<T> : EntityId where T : class {
        internal abstract   Type    GetEntityIdType (); // todo remove
        internal abstract   string  GetEntityId (T entity);
        internal abstract   void    SetEntityId (T entity, string id);
    }
    
    internal abstract class EntityId<T, TKey> : EntityId2<T> where T : class {
        internal abstract   TKey    StringToKey (string id);
        internal abstract   string  KeyToString (TKey id);
        
        internal abstract   TKey    GetId (T entity);
        internal abstract   void    SetId (T entity, TKey id);

        internal override   string  GetEntityId (T entity) {
            TKey id = GetId(entity);
            return KeyToString(id);
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            TKey key = StringToKey(id);
            SetId(entity, key);
        }
    }
}