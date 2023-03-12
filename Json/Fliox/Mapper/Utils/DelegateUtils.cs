// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Friflo.Json.Fliox.Mapper.Utils
{
    public static class DelegateUtils
    {
        public static Expression<Func<TInstance, TProperty>> CreateGetLambda<TInstance, TProperty> (PropertyInfo propInfo) {
#if ENABLE_IL2CPP
            return instance => (TProperty)propInfo.GetValue(instance);
#else
            var declaringType   = propInfo.DeclaringType;
            var instanceExp     = Expression.Parameter(typeof(TInstance), "instance");
            var srcInstanceExp  = Expression.Convert(instanceExp, declaringType);
            var propertyExp     = Expression.Property(srcInstanceExp, propInfo);
            var resultExp       = Expression.Convert(propertyExp, typeof(TProperty));
            var lambda          = Expression.Lambda<Func<TInstance, TProperty>>(resultExp, instanceExp);
            return lambda;
#endif
        }
        
        public static Expression<Action<TInstance, TProperty>> CreateSetLambda<TInstance, TProperty> (PropertyInfo propInfo) {
#if ENABLE_IL2CPP
            return (instance, value) => propInfo.SetValue(instance, value);
#else
            var declaringType   = propInfo.DeclaringType;
            var propertyType    = propInfo.PropertyType;
            var instanceExp     = Expression.Parameter(typeof(TInstance), "instance");
            var srcInstanceExp  = Expression.Convert(instanceExp, declaringType);
            var valueExp        = Expression.Parameter(typeof(TProperty), "value");
            var convValueExp    = Expression.Convert(valueExp, propertyType);
            var propertyExp     = Expression.Property(srcInstanceExp, propInfo);
            var assignExpr      = Expression.Assign (propertyExp, convValueExp);
            var lambda          = Expression.Lambda<Action<TInstance, TProperty>>(assignExpr, instanceExp, valueExp);
            return lambda;
#endif
        }
        
        public static Action<TInstance, TField> CreateFieldSetter<TInstance,TField>(FieldInfo field)
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