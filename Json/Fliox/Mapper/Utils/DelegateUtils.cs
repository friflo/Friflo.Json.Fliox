// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

#pragma warning disable 162 // Warning CS0162 : Unreachable code detected

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable AssignNullToNotNullAttribute
namespace Friflo.Json.Fliox.Mapper.Utils
{
    public static class DelegateUtils
    {
#if ENABLE_IL2CPP
        private const bool UseReflection = true;
#else
        private const bool UseReflection = false;
#endif
        
        /// -------------------------------- getter: field / property --------------------------------
        public static Func<T,TMember>    CreateMemberGetter<T, TMember>(MemberInfo mi)
        {
            GetFieldProperty(mi, out var field, out var property, out var memberType);
            if (UseReflection) {
                if (field != null) {
                    return (instance) => (TMember)field.GetValue(instance);
                }
                return (instance) => (TMember)property.GetValue(instance);
            }
            var instanceType    = typeof(T);
            var declaringType   = mi.DeclaringType;
            var instanceParam   = Expression.Parameter(instanceType,    "instance");
            Expression instance = typeof(T) == declaringType ? instanceParam :
                                  Expression.Convert(instanceParam, declaringType);
            var memberExp       = field != null ?
                                  Expression.Field(instance, field) : Expression.Property(instance, property);
            Expression value    = typeof(TMember) == memberType ? memberExp :
                                  Expression.Convert(memberExp, typeof(TMember));  // convert only id necessary
            return                Expression.Lambda<Func<T, TMember>>(value, instanceParam).Compile();
        }
        
        private static Expression<Func<TInstance, TProperty>> OBSOLETE_CreateGetLambda <TInstance, TProperty> (PropertyInfo propInfo) {
            if (UseReflection) {
                return instance => (TProperty)propInfo.GetValue(instance);
            }
            var declaringType   = propInfo.DeclaringType;
            var instanceExp     = Expression.Parameter(typeof(TInstance), "instance");
            var srcInstanceExp  = Expression.Convert(instanceExp, declaringType);   
            var propertyExp     = Expression.Property(srcInstanceExp, propInfo);
            var resultExp       = Expression.Convert(propertyExp, typeof(TProperty));
            return                Expression.Lambda<Func<TInstance, TProperty>>(resultExp, instanceExp);
        }
        
        /// -------------------------------- setter: field / property --------------------------------  
        public static Action<T,TMember>    CreateMemberSetter<T, TMember>(MemberInfo mi)
        {
            GetFieldProperty(mi, out var field, out var property, out var memberType);
            bool readOnlyField = field != null && (field.Attributes & FieldAttributes.InitOnly) != 0;
            if (UseReflection || readOnlyField) {
                if (field != null) {
                    return (instance, value) => field.SetValue(instance, value);
                }
                return (instance, value) => property.SetValue(instance, value);
            }
            var instanceType    = typeof(T);
            var declaringType   = mi.DeclaringType;
            var instanceParam   = Expression.Parameter(instanceType, "instance");
            Expression instance = typeof(T) == declaringType ? instanceParam :
                                  Expression.Convert(instanceParam, declaringType);
            var valueExp        = Expression.Parameter(typeof(TMember), "value");
            Expression value    = typeof(TMember) == memberType ? valueExp :
                                  Expression.Convert(valueExp, memberType); // convert only id necessary
            var memberExp       = field != null ?
                                  Expression.Field(instance, field) : Expression.Property(instance, property);
            var assignExpr      = Expression.Assign (memberExp, value);
            var lambda          = Expression.Lambda<Action<T, TMember>>(assignExpr, instanceParam, valueExp);
            return lambda.Compile();
        }
        
        private static Expression<Action<TInstance, TProperty>> OBSOLETE_CreateSetLambda <TInstance, TProperty> (PropertyInfo propInfo) {
            if (UseReflection) {
                return (instance, value) => propInfo.SetValue(instance, value);
            }
            var declaringType   = propInfo.DeclaringType;
            var propertyType    = propInfo.PropertyType;
            var instanceExp     = Expression.Parameter(typeof(TInstance), "instance");
            var srcInstanceExp  = Expression.Convert(instanceExp, declaringType);
            var valueExp        = Expression.Parameter(typeof(TProperty), "value");
            var convValueExp    = Expression.Convert(valueExp, propertyType);
            var propertyExp     = Expression.Property(srcInstanceExp, propInfo);
            var assignExpr      = Expression.Assign (propertyExp, convValueExp);
            return                Expression.Lambda<Action<TInstance, TProperty>>(assignExpr, instanceExp, valueExp);
        }
        
        // ------------------------------------------- utils -------------------------------------------
        private static void GetFieldProperty(MemberInfo member, out FieldInfo field, out PropertyInfo property, out Type memberType) {
            if (member is FieldInfo fieldInfo) {
                field = fieldInfo;
                property = null;
                memberType = field.FieldType;
                return;
            }
            field = null;
            property = (PropertyInfo)member;
            memberType = property.PropertyType;
        }
        
        /// <summary>
        /// In contrast to expression based delegate IL based delegate is able to change <b>readonly</b> fields 
        /// </summary>
        public static Action<TInstance, TField> CreateFieldSetterIL<TInstance,TField>(FieldInfo field)
        {
#if ENABLE_IL2CPP || NETSTANDARD2_0
            return (instance, value) => field.SetValue(instance, value);
#else
            string methodName = "set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[]{typeof(TInstance),typeof(TField)},true);
            ILGenerator gen = setterMethod.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stfld, field);

            gen.Emit(OpCodes.Ret);
            return (Action<TInstance, TField>)setterMethod.CreateDelegate(typeof(Action<TInstance, TField>));
#endif
        }
    }
} 