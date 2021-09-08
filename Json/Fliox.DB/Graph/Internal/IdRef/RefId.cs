// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Fliox.DB.Graph.Internal.IdRef
{
    // -------------------------------------------- EntityId -----------------------------------------------
    internal abstract class RefId {
        private static readonly   Dictionary<Type, RefId> Ids = new Dictionary<Type, RefId>();

        internal static RefKey<TKey, T> GetRefKey<TKey, T> () where T : class {
            return (RefKey<TKey, T>)GetRefId<T>();
        }
        
        internal static RefId<T> GetRefId<T> () where T : class {
            var type = typeof(T);
            if (Ids.TryGetValue(type, out RefId id)) {
                return (RefId<T>)id;
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
        
        private static RefId<T> CreateEntityIdProperty<T> (PropertyInfo property)  where T : class {
            var type        = typeof (T);
            var propType    = property.PropertyType;
            var idGetMethod = property.GetGetMethod(true);    
            var idSetMethod = property.GetSetMethod(true);
            
            if (idGetMethod == null || idSetMethod == null) {
                var msg2 = $"entity id property must have get & set: {property.Name}, type: {propType.Name}, entity: {type.Name}";
                throw new InvalidOperationException(msg2);
            }
            if (propType == typeof(string)) {
                return new RefKeyStringProperty<T>   (property, idGetMethod, idSetMethod);
            }
            if (propType == typeof(Guid)) {
                return new RefKeyGuidProperty<T>     (property, idGetMethod, idSetMethod);
            }
            if (propType == typeof(Guid?)) {
                return new RefKeyGuidNullProperty<T> (property, idGetMethod, idSetMethod);
            }
            if (propType == typeof(int)) {
                return new RefKeyIntProperty<T>      (property, idGetMethod, idSetMethod);
            }
            if (propType == typeof(long)) {
                return new RefKeyLongProperty<T>     (property, idGetMethod, idSetMethod);
            }
            if (propType == typeof(short)) {
                return new RefKeyShortProperty<T>    (property, idGetMethod, idSetMethod);
            }
            if (propType == typeof(byte)) {
                return new RefKeyByteProperty<T>    (property, idGetMethod, idSetMethod);
            }
            // add additional types here
            var msg = UnsupportedTypeMessage(type, property, propType);
            throw new InvalidOperationException(msg);
        }
            
        private static RefId<T> CreateEntityIdField<T> (FieldInfo field)  where T : class {
            var type        = typeof (T);
            var fieldType   = field.FieldType;
            
            if (fieldType == typeof(string)) {
                return new RefKeyStringField<T>(field);
            }
            if (fieldType == typeof(Guid)) {
                return new RefKeyGuidField<T>(field);
            }
            if (fieldType == typeof(Guid?)) {
                return new RefKeyGuidNullField<T>(field);
            }
            if (fieldType == typeof(int)) {
                return new RefKeyIntField<T>(field);
            }
            if (fieldType == typeof(long)) {
                return new RefKeyLongField<T>(field);
            }
            if (fieldType == typeof(short)) {
                return new RefKeyShortField<T>(field);
            }
            if (fieldType == typeof(byte)) {
                return new RefKeyByteField<T>(field);
            }
            // add additional types here
            var msg = UnsupportedTypeMessage(type, field, fieldType);
            throw new InvalidOperationException(msg);
        }
        
        private static string UnsupportedTypeMessage(Type type, MemberInfo member, Type memberType) {
            return $"unsupported Type for entity key: {type.Name}.{member.Name}, Type: {memberType.Name}";
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
    internal abstract class RefId<T> : RefId where T : class {
        internal abstract   Type    GetKeyType();
        internal abstract   string  GetKeyName();
    }
    
    internal abstract class RefKey<TKey, T> : RefId<T> where T : class {
        internal abstract   JsonKey KeyToId (in TKey key);
        internal abstract   TKey    IdToKey (in JsonKey key);
        
        internal abstract   TKey    GetKey  (T entity);             // todo remove deprecated method
        internal abstract   void    SetKey  (T entity, TKey id);    // todo remove deprecated method
        
        internal virtual    bool    IsKeyNull (TKey key) => false;
    }
}