// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity
{
    // --------------------------------------------- EntityKey ---------------------------------------------
    internal abstract class EntityKey {
        private static readonly   Dictionary<Type, EntityKey> Map = new Dictionary<Type, EntityKey>();

        internal static EntityKeyT<TKey, T> GetEntityKeyT<TKey, T> () where T : class {
            return (EntityKeyT<TKey, T>)GetEntityKey<T>();
        }
        
        internal static EntityKey<T> GetEntityKey<T> () where T : class {
            var type = typeof(T);
            if (Map.TryGetValue(type, out EntityKey id)) {
                return (EntityKey<T>)id;
            }
            var member = FindKeyMember (type);
            var property = member as PropertyInfo;
            if (property != null) {
                var result  = CreateEntityKeyProperty<T>(property);
                Map[type]   = result;
                return result;
            }
            var field = member as FieldInfo;
            if (field != null) {
                var result  = CreateEntityKeyField<T>(field);
                Map[type]   = result;
                return result;
            }
            throw new InvalidTypeException($"Missing primary [Key] field/property in entity type: {type.Name}"); //
        }
        
        private static MemberInfo FindKeyMember (Type type) {
            var properties = type.GetProperties(Flags);
            foreach (var p in properties) {
                var customAttributes = p.CustomAttributes;
                if (AttributeUtils.IsKey(customAttributes))
                    return p;
            }
            var fields = type.GetFields(Flags);
            foreach (var f in fields) {
                var customAttributes = f.CustomAttributes;
                if (AttributeUtils.IsKey(customAttributes))
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
            /* foreach (var member in members) {
                if (member.Name == "Id")
                    return member;
            } */
            return null;
        }

        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        private static EntityKey<T> CreateEntityKeyProperty<T> (PropertyInfo property)  where T : class {
            var type        = typeof (T);
            var propType    = property.PropertyType;
            var idGetMethod = property.GetGetMethod(true);    
            var idSetMethod = property.GetSetMethod(true);
            if (idGetMethod == null || idSetMethod == null) {
                var msg2 = $"entity [Key] property {type.Name}.{property.Name} requires {{ get; set; }}";
                throw new InvalidTypeException(msg2);
            }
            bool auto = AttributeUtils.IsAutoIncrement(property.CustomAttributes);
            if (propType == typeof(string))     return new EntityKeyStringProperty<T>       (property);
            if (propType == typeof(ShortString))return new EntityKeyShortStringProperty<T>  (property);
            if (propType == typeof(Guid))       return new EntityKeyGuidProperty<T>         (property);
            if (propType == typeof(int))        return new EntityKeyIntProperty<T>          (property);
            if (propType == typeof(long))       return new EntityKeyLongProperty<T>         (property);
            if (propType == typeof(short))      return new EntityKeyShortProperty<T>        (property);
            if (propType == typeof(byte))       return new EntityKeyByteProperty<T>         (property);
            if (propType == typeof(JsonKey))    return new EntityKeyJsonKeyProperty<T>      (property);
            var msg = UnsupportedTypeMessage(type, property, propType);
            throw new InvalidTypeException(msg);
        }
            
        private static EntityKey<T> CreateEntityKeyField<T> (FieldInfo field)  where T : class {
            var type        = typeof (T);
            var fieldType   = field.FieldType;
            bool auto = AttributeUtils.IsAutoIncrement(fieldType.CustomAttributes);
            if (fieldType == typeof(string))        return new EntityKeyStringField<T>      (field);
            if (fieldType == typeof(ShortString))   return new EntityKeyShortStringField<T> (field);
            if (fieldType == typeof(Guid))          return new EntityKeyGuidField<T>        (field);
            if (fieldType == typeof(int))           return new EntityKeyIntField<T>         (field);
            if (fieldType == typeof(long))          return new EntityKeyLongField<T>        (field);
            if (fieldType == typeof(short))         return new EntityKeyShortField<T>       (field);
            if (fieldType == typeof(byte))          return new EntityKeyByteField<T>        (field);
            if (fieldType == typeof(JsonKey))       return new EntityKeyJsonKeyField<T>     (field);
            var msg = UnsupportedTypeMessage(type, field, fieldType);
            throw new InvalidTypeException(msg);
        }
        
        private static string UnsupportedTypeMessage(Type type, MemberInfo member, Type memberType) {
            return $"unsupported TKey Type: EntitySet<{memberType.Name},{type.Name}> {member.Name}";
        }
        
        // --- field getter / setter
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
        
        // --- property getter / setter
        internal static Func<TEntity,TField> GetPropertyGet<TEntity, TField>(PropertyInfo property) {
            var instanceType    = property.DeclaringType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var fieldExp        = Expression.Property(instExp, property);
            return                Expression.Lambda<Func<TEntity, TField>>(fieldExp, instExp).Compile();
        }
        
        internal static Action<TEntity,TField> GetPropertySet<TEntity, TField>(PropertyInfo property) {
            var instanceType    = property.DeclaringType;
            var fieldType       = property.PropertyType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var valueExp        = Expression.Parameter(fieldType,       "value");
            var fieldExp        = Expression.Property(instExp, property);
            var assignExpr      = Expression.Assign (fieldExp, valueExp);
            return                Expression.Lambda<Action<TEntity, TField>>(assignExpr, instExp, valueExp).Compile();
        }
    }

    // -------------------------------------------- EntityKey<T> --------------------------------------------
    internal abstract class EntityKey<T> : EntityKey where T : class {
        internal abstract   Type    GetKeyType();
        internal abstract   string  GetKeyName();
        internal virtual    bool    IsIntKey()                 => true;
        internal virtual    bool    IsEntityKeyNull (T entity) => false;
        internal virtual    bool    IsDefaultKey    (T entity) => false;
    }
    
    // ----------------------------------------- EntityKeyT<TKey, T> -----------------------------------------
    internal abstract class EntityKeyT<TKey, T> : EntityKey<T> where T : class {
        internal readonly       bool                autoIncrement;
        
        private static readonly KeyConverter<TKey>  KeyConvert = KeyConverter.GetConverter<TKey>();
            
        internal abstract   TKey        GetKey      (T entity);
        internal abstract   void        SetKey      (T entity, TKey id);
        
        internal EntityKeyT (MemberInfo member) {
            autoIncrement   = AttributeUtils.IsAutoIncrement(member.CustomAttributes);
        }

        /// <summary> prefer using <see cref="GetKey"/>. Use only if <typeparamref name="TKey"/> is not utilized </summary>
        internal JsonKey GetId   (T entity) {
            TKey key = GetKey(entity);
            return KeyConvert.KeyToId(key);
        }
        
        /// <summary> prefer using <see cref="SetKey"/>. Use only if <typeparamref name="TKey"/> is not utilized </summary>
        internal void    SetId   (T entity, in JsonKey id) {
            TKey key = KeyConvert.IdToKey(id);
            SetKey(entity, key);
        }
    }
}