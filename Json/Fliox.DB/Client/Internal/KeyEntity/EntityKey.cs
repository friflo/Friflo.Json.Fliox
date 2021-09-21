// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Fliox.DB.Client.Internal.KeyEntity
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
        
        private static EntityKey<T> CreateEntityKeyProperty<T> (PropertyInfo property)  where T : class {
            var type        = typeof (T);
            var propType    = property.PropertyType;
            var idGetMethod = property.GetGetMethod(true);    
            var idSetMethod = property.GetSetMethod(true);
            if (idGetMethod == null || idSetMethod == null) {
                var msg2 = $"entity id property must have get & set: {property.Name}, type: {propType.Name}, entity: {type.Name}";
                throw new InvalidOperationException(msg2);
            }
            bool auto = FieldQuery.IsAutoIncrement(property.CustomAttributes);
            if (propType == typeof(string)) return new EntityKeyStringProperty<T>   (property, idGetMethod, idSetMethod);
            if (propType == typeof(Guid))   return new EntityKeyGuidProperty<T>     (property, idGetMethod, idSetMethod);
            if (propType == typeof(int))    return new EntityKeyIntProperty<T>      (property, idGetMethod, idSetMethod);
            if (propType == typeof(long))   return new EntityKeyLongProperty<T>     (property, idGetMethod, idSetMethod);
            if (propType == typeof(short))  return new EntityKeyShortProperty<T>    (property, idGetMethod, idSetMethod);
            if (propType == typeof(byte))   return new EntityKeyByteProperty<T>     (property, idGetMethod, idSetMethod);
            var msg = UnsupportedTypeMessage(type, property, propType);
            throw new InvalidOperationException(msg);
        }
            
        private static EntityKey<T> CreateEntityKeyField<T> (FieldInfo field)  where T : class {
            var type        = typeof (T);
            var fieldType   = field.FieldType;
            bool auto = FieldQuery.IsAutoIncrement(fieldType.CustomAttributes);
            if (fieldType == typeof(string))    return new EntityKeyStringField<T>  (field);
            if (fieldType == typeof(Guid))      return new EntityKeyGuidField<T>    (field);
            if (fieldType == typeof(int))       return new EntityKeyIntField<T>     (field);
            if (fieldType == typeof(long))      return new EntityKeyLongField<T>    (field);
            if (fieldType == typeof(short))     return new EntityKeyShortField<T>   (field);
            if (fieldType == typeof(byte))      return new EntityKeyByteField<T>    (field);
            var msg = UnsupportedTypeMessage(type, field, fieldType);
            throw new InvalidOperationException(msg);
        }
        
        private static string UnsupportedTypeMessage(Type type, MemberInfo member, Type memberType) {
            return $"unsupported TKey Type: EntitySet<{memberType.Name},{type.Name}> {member.Name}";
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

    // -------------------------------------------- EntityKey<T> --------------------------------------------
    internal abstract class EntityKey<T> : EntityKey where T : class {
        internal abstract   Type    GetKeyType();
        internal abstract   string  GetKeyName();
        internal virtual    bool    IsIntKey()                 => true;
        internal virtual    bool    IsEntityKeyNull (T entity) => false;
        internal virtual    bool    IsDefaultKey    (T entity) => false;

        internal abstract   JsonKey GetId           (T entity);
        internal abstract   void    SetId           (T entity, in JsonKey id);
        
        internal abstract   TAsType GetKeyAsType<TAsType> (T entity);    // TAG_NULL_REF
    }
    
    // ----------------------------------------- EntityKeyT<TKey, T> -----------------------------------------
    internal abstract class EntityKeyT<TKey, T> : EntityKey<T> where T : class {
        internal readonly   bool        autoIncrement;
            
        internal abstract   TKey        GetKey      (T entity);
        internal abstract   void        SetKey      (T entity, TKey id);
        
        internal EntityKeyT (MemberInfo member) {
            autoIncrement   = FieldQuery.IsAutoIncrement(member.CustomAttributes);
        }

        internal override   TAsType GetKeyAsType<TAsType> (T entity) {
            // Will box in DEBUG - not in RELEASE (.NET Core 3.1)
            return (TAsType)(object)GetKey(entity); // TAG_NULL_REF
        }

        internal override   JsonKey GetId   (T entity) {
            TKey key = GetKey(entity);
            return Ref<TKey,T>.RefKeyMap.KeyToId(key);
        }
        
        internal override   void    SetId   (T entity, in JsonKey id) {
            TKey key = Ref<TKey,T>.RefKeyMap.IdToKey(id);
            SetKey(entity, key);
        }
    }
}