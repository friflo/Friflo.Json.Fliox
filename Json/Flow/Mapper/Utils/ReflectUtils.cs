// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Flow.Mapper.Utils
{
    public static class ReflectUtils
    {
        private static readonly object[] DefConstructorArgs = new object[0];

        public static object CreateInstance (Type type)
        {
            return CreateInstance (type, DefConstructorArgs );
        }
        
        public static object CreateInstance (Type type, object[] args)
        {
            try
            {
                return Activator.CreateInstance (type, args );
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException ?? e;
            }
        }

        public static T NewInstance <T> (Type type) {

            // return (T) Activator.CreateInstance( type ); // can call only public constructor
            ConstructorInfo ci = GetDefaultConstructor (type);
            if (ci == null)
                throw new InvalidOperationException("No default constructor accessible. type: " + type);
            return (T)ci.Invoke (DefConstructorArgs);
        }

        public static object CreateInstance(ConstructorInfo constructor) {
            return constructor. Invoke (DefConstructorArgs);
        }
        
        public static object CreateInstanceCopy<T>(ConstructorInfo constructor, IEnumerable<T> src) {
            object[] args = new object[1];
            args[0] = src;
            return constructor. Invoke (args);
        }

        public static ConstructorInfo GetDefaultConstructor (Type type) {
            // return type.GetConstructor( new Type[0] ); only .net 3.5
#if NETFX_CORE
            foreach (ConstructorInfo ci in type.GetTypeInfo ().DeclaredConstructors)
#else
            foreach (ConstructorInfo ci in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
#endif
                if (ci.GetParameters().Length == 0)
                    return ci;
            return null;
        }
        
        public static ConstructorInfo GetCopyConstructor (Type type) {
            foreach (ConstructorInfo ci in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) {
                var param = ci.GetParameters();
                if (param.Length == 1) {
                    var paramType = param[0].ParameterType;  
                    if (paramType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        return ci;
                }
            }
            return null;
        }

        // IsAssignableFrom
        public static bool IsAssignableFrom (Type type, Type from)
        {
#if NETFX_CORE
            return type.GetTypeInfo().IsAssignableFrom(from.GetTypeInfo());
#else
            return type. IsAssignableFrom (from);
#endif
        }
        
        public static bool IsIDictionary(Type type) {
            Type[] dictArgs = GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
            return dictArgs != null;
        }

        public static Type[] GetGenericInterfaceArgs (Type type, Type interfaceType)
        {
#if NETFX_CORE
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == interfaceType)
                return type.GenericTypeArguments;
            IEnumerable<Type> interfaces = type.GetTypeInfo().ImplementedInterfaces;
            foreach (Type inter in interfaces)
            {               
                if (inter.GetTypeInfo().IsGenericType && inter.GetGenericTypeDefinition() == interfaceType)
                    return inter.GenericTypeArguments;
            }
#else
            if (type.IsGenericType && type.GetGenericTypeDefinition() == interfaceType)
                return type.GetGenericArguments();
            Type[] interfaces = type.GetInterfaces();
            foreach (Type inter in interfaces)
            {
                if (inter.IsGenericType && inter.GetGenericTypeDefinition() == interfaceType)
                    return inter.GetGenericArguments();
            }
#endif
            return null;
        }
    }
}
