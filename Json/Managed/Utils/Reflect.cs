// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;

namespace Friflo.Json.Managed.Utils
{
	// Reflect
	public class Reflect
	{
		// GetField
		public static FieldInfo GetField (Type type, String name)
		{
#if NETFX_CORE
			TypeInfo ti = type.GetTypeInfo ();
			FieldInfo fi = ti.GetDeclaredField (name);
			if (fi != null)
				return fi;
			type = ti.BaseType;
			if (type != null)
				return GetField (type, name);
			return null;
#else
			return type. GetField (name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
#endif
		}

		// GetFields
		static public FieldInfo[] GetFields (Type type)
		{
#if NETFX_CORE
			List<FieldInfo> list = new List<FieldInfo>();
			Type t = type;
			while (t != null)
			{
				TypeInfo ti = t.GetTypeInfo();
				foreach (FieldInfo item in ti.DeclaredFields)
				{
					if (item.IsPublic && !item.IsStatic)
						list.Add(item);
				}
				t = ti.BaseType;
			}
			return list.ToArray();
#else
			return type.GetFields(BindingFlags.Public | BindingFlags.Instance ); //| BindingFlags.Static);
#endif
		}

		public static PropertyInfo GetPropertyGet (Type type, String name)
		{
#if NETFX_CORE
			TypeInfo ti = type.GetTypeInfo ();
			PropertyInfo pi = ti.GetDeclaredProperty (name);
			if (pi != null && pi.GetMethod != null)
				return pi;
			type = ti.BaseType;
			if (type != null)
				return GetPropertyGet (type, name);
			return null;
#else
			PropertyInfo pi = type.GetProperty (name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
			if (pi == null || pi.GetGetMethod (true) == null)
				return null;
			return pi;
#endif
		}

		public static PropertyInfo GetPropertySet (Type type, String name)
		{
#if NETFX_CORE
			TypeInfo ti = type.GetTypeInfo ();
			PropertyInfo pi = ti.GetDeclaredProperty (name);
			if (pi != null && pi.SetMethod != null)
				return pi;
			type = ti.BaseType;
			if (type != null)
				return GetPropertySet (type, name);
			return null;
#else
			PropertyInfo pi = type.GetProperty (name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
			if (pi == null || pi.GetSetMethod (true) == null)
				return null;
			return pi;
#endif
		}

		private readonly static Object[] DefConstructorArgs = new Object[0];

		public static Object CreateInstance (Type type)
		{
			return CreateInstance (type, DefConstructorArgs );
		}
		
		public static Object CreateInstance (Type type, Object[] args)
		{
			try
			{
				return Activator.CreateInstance (type, args );
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException != null ? e.InnerException : e;
			}
		}

		public static T NewInstance <T> (Type type)
		{
			try
			{
				// return (T) Activator.CreateInstance( type ); // can call only public constructor
				ConstructorInfo ci = GetDefaultConstructor (type);
				if (ci == null)
					throw new FrifloException("No default constructor accessible. type: " + type.FullName);
				return (T)ci.Invoke (DefConstructorArgs);
			}
			catch (Exception e)
			{
				throw new FrifloException("Failed to invoke default constructor of: " + type.FullName, e);
			}
		}

		public static Object CreateInstance(ConstructorInfo constructor)
		{
			try
			{
				return constructor. Invoke (DefConstructorArgs);
			}
			catch (Exception e)
			{
				throw new FrifloException("Failed calling default constructor", e);
			}
		}

		public static ConstructorInfo GetDefaultConstructor (Type type)
		{
			try
			{
				// return type.GetConstructor( new Type[0] ); only .net 3.5
#if NETFX_CORE
				foreach (ConstructorInfo ci in type.GetTypeInfo ().DeclaredConstructors)
#else
				foreach (ConstructorInfo ci in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
#endif
				{
					if (ci.GetParameters().Length == 0)
						return ci;
				}
				return null;
			}
			catch (Exception e)
			{
				throw new FrifloException("Cannot access default constructor", e);
			}
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

		// GetMethods
		public static MethodInfo[] GetMethods (Type type)
		{
#if NETFX_CORE
			List<MethodInfo> list = new List<MethodInfo>();
			Type curType = type;
			while (curType != typeof( Object ))
			{
				TypeInfo ti = curType.GetTypeInfo();
				foreach (MethodInfo method in ti.DeclaredMethods)
					list.Add (method);
				curType = ti.BaseType;
			}
			return list.ToArray();
#else
			return type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
#endif
		}

		// GetMethod
		public static MethodInfo GetMethod (Type type, String method, Type[] types)
		{
#if NETFX_CORE
			Type curType = type;
			while (curType != typeof( Object ))
			{
				TypeInfo ti = curType.GetTypeInfo();
				MethodInfo mi = ti.GetDeclaredMethod(method);
				if (mi != null)
					return mi;
				curType = ti.BaseType;
			}
			return null;
#else
			return type.GetMethod(method, types);
#endif
		}

		// GetMethodEx
		public static MethodInfo GetMethodEx (Type type, String method, Type[] types)
		{
			MethodInfo[] methods = Reflect.GetMethods(type);
			for (int n = 0; n < methods. Length; n++)
			{
				MethodInfo m = methods[n];
				if (m. Name. Equals (method))
				{
					ParameterInfo[] po = m.GetParameters();
					if (po.Length == types.Length)
					{
						int i = 0;
						for (; i < po.Length; i++)
						{
							if (po[i].ParameterType != types[i])
								break;
						}
						if (i == types.Length)
							return m;
					}
				}
			}
			return null;
		}

		// Invoke
		public static Object Invoke (MethodInfo method, Object obj, Object[] args)
		{
			try
			{
				return method. Invoke (obj, args);
			}
			catch (TargetInvocationException e)
			{
				Exception cause = e. InnerException; // Java: getCause() == getTargetException()
		        if (cause == null)
		        {
		            throw new FrifloException("Got InvocationTargetException, with cause = null.", e);
		        }
		        else
		        {
		            throw cause;
		        }
				// Java only




			}
			// illegal access
/*			catch (MethodAccessException e)  missing in WinRT
			{
				throw new FrifloIOException("Invoke() illegal access on method: " + GetMethodName(method), e);
			} */
			// illegal argument
			catch (ArgumentException  e)
			{
				throw new FrifloException("Invoke() illegal argument on method: " + GetMethodName(method), e);
			}
			catch (TargetParameterCountException  e)
			{
				throw new FrifloException("Invoke() illegal argument on method: " + GetMethodName(method), e);
			}
		}
	
		public static String GetMethodName (MethodInfo method)
		{
			return method. DeclaringType.FullName + "." + method. Name;
		}

	}
}
