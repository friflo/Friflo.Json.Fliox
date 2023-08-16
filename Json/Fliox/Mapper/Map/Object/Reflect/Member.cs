// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Friflo.Json.Fliox.Mapper.Utils;


namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    // --- fields
    internal sealed class MemberField : Var.Member {
        private  readonly   VarType     varType;
        private  readonly   FieldInfo   field;
        
        internal MemberField(VarType varType, FieldInfo field) {
            this.varType    = varType;
            this.field      = field;
        }
        
        public    override    Var     GetVar (object obj) {
            // if (useDirect) return field.GetValueDirect(__makeref(obj));
            var value = field.GetValue(obj);
            return varType.FromObject(value);
        }
        
        public      override    void    SetVar (object obj, in Var value) {
            var valueObject = varType.ToObject(value);
            // if (useDirect) { field.SetValueDirect(__makeref(obj), value); return; }
            field.SetValue(obj, valueObject); // todo use Expression - but not for Unity
        }
    }
    
    // --- properties
    internal sealed class MemberProperty : Var.Member {
        private  readonly   VarType                 varType;
        private  readonly   Func<object, object>    getLambda;
        private  readonly   Action<object, object>  setLambda;
        
        internal MemberProperty(VarType varType, PropertyInfo property) {
            this.varType        = varType;
            var getLambdaExp    = DelegateUtils.CreateGetLambda<object,object>(property);
            var setLambdaExp    = DelegateUtils.CreateSetLambda<object,object>(property);
            getLambda           = getLambdaExp.Compile();
            setLambda           = setLambdaExp.Compile();
        }
        
        public    override    Var     GetVar (object obj) {
            var value = getLambda(obj); // return new Var(getMethod.Invoke(obj, null));
            return varType.FromObject(value);
        }
        
        public    override    void    SetVar (object obj, in Var value) {
            var valueObject = varType.ToObject(value);
            setLambda(obj, valueObject);
        }
    }
    
    // ReSharper disable AssignNullToNotNullAttribute
    public static class MemberUtils
    {
        // --- field / property getter
        // private static readonly bool useDirect = false; // Unity: System.NotImplementedException : GetValueDirect
        internal static Func<TEntity,TField>    CreateFieldGetter<TEntity, TField>(FieldInfo field) {
            var instanceType    = field.DeclaringType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var fieldExp        = Expression.Field(instExp, field);
            return                Expression.Lambda<Func<TEntity, TField>>(fieldExp, instExp).Compile();
        }
        
        internal static Func<TEntity,TField>    CreatePropertyGetter<TEntity, TField>(PropertyInfo property) {
            var instanceType    = property.DeclaringType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var fieldExp        = Expression.Property(instExp, property);
            return                Expression.Lambda<Func<TEntity, TField>>(fieldExp, instExp).Compile();
        }
        
        // --- field / property setter
        internal static Func<T, TField>         CreateFieldSetter<T,TField>(FieldInfo fieldInfo) {
            var instanceType    = fieldInfo.DeclaringType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var fieldExp        = Expression.Field(instExp, fieldInfo);
            return                Expression.Lambda<Func<T, TField>>(fieldExp, instExp).Compile();
        }
        
        internal static Func<T, TField>         CreatePropertySetter<T, TField>(PropertyInfo property) {
            var instExp         = Expression.Parameter(typeof(object),    "instance");
            var instanceType    = property.DeclaringType;
            var exp             = Expression.Convert(instExp, instanceType);
            var fieldExp        = Expression.Property(exp, property);
            return                Expression.Lambda<Func<T, TField>>(fieldExp, instExp).Compile();
        }
        
        // --- alternative: field getter using IL
        // ReSharper disable once UnusedMember.Local
        private static Func<T, TField> CreateFieldGetterIL<T,TField>(FieldInfo fieldInfo) {
            var parameterTypes = new[] { typeof(T) };
            var dynMethod = new DynamicMethod($"{typeof(T).FullName}_{fieldInfo.Name}_Get", typeof(TField), parameterTypes, true);
            var il = dynMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldInfo);
            il.Emit(OpCodes.Ret);
            return (Func<T, TField>)dynMethod.CreateDelegate(typeof(Func<T, TField>));
        }
    }
}