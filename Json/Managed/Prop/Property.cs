// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
	public abstract class  Property
	{
		private readonly static 	Type[] Types = new Type [] { typeof( Property ) };

		public abstract	void	Set(String name) ;
		public abstract void	Set(String name, String field) ;

		public abstract void	SetMethod(String name) ;

		public static MethodInfo GetPropertiesDeclaration (Type type)
		{
			return Reflect.GetMethodEx(type, "SetProperties", Types);
		}

		internal void SetProperties (Type type)
		{
			try
			{
				MethodInfo method = GetPropertiesDeclaration(type);
				if (method != null)
				{
					Object[] args = new Object[] { this };
					Reflect.Invoke (method, null, args);
				}
				else
				{
					FieldInfo[] field = Reflect.GetFields(type);
					for (int n = 0; n < field. Length; n++)
						Set(field[n]. Name);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("SetProperties() failed for type: " + type. FullName, e);
			}
		}
	}
}
