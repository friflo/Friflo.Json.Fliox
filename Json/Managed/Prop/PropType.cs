// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
    // PropType
    public class PropType : IDisposable
	{
		public	readonly Type						nativeType;
		public	readonly Bytes						typeName;

		private readonly FFMap<String, PropField>	strMap 		= new HashMapOpen<String, PropField>(13);
		private readonly FFMap<Bytes, PropField> 	fieldMap 	= new HashMapOpen<Bytes, PropField>(11);
		public  readonly PropertyFields 			propFields;
		private readonly ConstructorInfo			constructor;
		
		
		public void Dispose() {
			typeName.Dispose();
			propFields.Dispose();
		}

		// PropType
		internal PropType (Type type, String name)
		{
			nativeType = type;
			typeName = new Bytes(name);
			propFields = new  PropertyFields (type, this, true, true);
			for (int n = 0; n < propFields.num; n++)
			{
				PropField 	field = propFields.fields[n];
				strMap.Put(field.name, field);
				fieldMap.Put(field.nameBytes, field);
			}
			constructor = Reflect.GetDefaultConstructor (type);
		}
		
		public Object CreateInstance()
		{
			if (constructor == null)
				throw new FrifloException("No default constructor available for: " + nativeType. Name);
			return Reflect.CreateInstance(constructor);
		}

		public PropField GetField (String name)
		{
			return strMap.Get(name);
		}

		public PropField GetField (Bytes fieldName)
		{
			return fieldMap.Get(fieldName);
		}

		// public static readonly Store store = new Store();
		
		public class Store : IDisposable
		{
			private   readonly 	HashMapLang <Type, PropType>	typeMap= 	new HashMapLang <Type, PropType >();
			internal  readonly 	HashMapLang <Bytes, PropType>	nameMap= 	new HashMapLang <Bytes, PropType >();
			
			public void Dispose() {
				lock (nameMap) {
					foreach (var type in typeMap.Values)
						type.Dispose();
				}
			}
			
			internal PropType GetInternal (Type type, String name)
			{
				lock (typeMap)
				{
					PropType propType = typeMap.Get(type);
					if (propType == null)
					{
						propType = new PropType(type, name);
						typeMap.Put(type, propType);
					}
					return propType;
				}
			}
			
			public void RegisterType (String name, Type type)
			{
				lock (nameMap)
				{
					PropType propType = GetInternal(type, name); 
					if (!propType.typeName.buffer.IsCreated())
						throw new FrifloException("Type already created without registered name");
					if (!propType.typeName.IsEqualString(name))
						throw new FrifloException("Type already registered with different name: " + name);
					nameMap.Put(propType.typeName, propType);
				}
			}

		}
	
		public class Cache
		{
			private readonly	HashMapLang <Type, PropType>	typeMap = new HashMapLang <Type, PropType >();
			private readonly	HashMapLang <Bytes, PropType>	nameMap = new HashMapLang <Bytes, PropType >();
			private readonly	Store 							store;
			
			public Cache (Store store)
			{
				this.store = store;
			}
			
			public PropType Get (Type type)
			{
				PropType propType = typeMap.Get(type);
				if (propType == null)
				{
					propType = store.GetInternal(type, null);
					typeMap.Put(type, propType);
				}
				return propType;
			}

			public PropType GetByName(Bytes name)
			{
				PropType propType = nameMap.Get(name);
				if (propType == null)
				{
					lock (store.nameMap)
					{
						propType = store.nameMap.Get(name);
					}
					nameMap.Put(propType.typeName, propType);
				}
				return propType;
			}

		}
	}	
}
