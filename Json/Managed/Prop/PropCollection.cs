// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
	public class PropCollection
	{
		public 	 readonly 	Type			type;
		public 	 readonly	Type	 		typeInterface;
		public 	 readonly	Type			keyType;
		public 	 readonly 	Type			elementType;
		public 				PropType		elementPropType;
		public	 readonly	SimpleType.Id ?	id;
        internal readonly	ConstructorInfo constructor;
	
		internal PropCollection (Type typeInterface, Type type, Type elementType, Type keyType)
		{
			this.type			= type;
			this.typeInterface	= typeInterface;
			this.keyType		= keyType;
			this.elementType	= elementType;
			this.id				= SimpleType.IdFromType(elementType);
            this.constructor    = GetConstructor (type, typeInterface, keyType, elementType);
		}

        public Object CreateInstance ()
        {
            return Reflect.CreateInstance(constructor);
        }

        internal static ConstructorInfo GetConstructor (Type type, Type typeInterface, Type keyType, Type elementType)
        {
    	    ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
    	    if (constructor != null)
    		    return constructor;
			if	(typeInterface == typeof( Array ))
			{
				return null; // For arrays Arrays.CreateInstance(componentType, length) is used
			}
			if  (typeInterface == typeof( IList<> ))
			{
				return Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
			}
			if (typeInterface == typeof( IDictionary<,> ))
			{
				return Reflect.GetDefaultConstructor( typeof(Dictionary<,>).MakeGenericType(keyType, elementType) );
			}
            throw new FrifloException ("interface type not supported");
        }

		public  class Info
    	{
	    	internal PropCollection	collection;
	    	internal PropAccess		access;
	
			static  public Info Create (FieldInfo field)
			{
	    		Info info = new Info();
	    		info.Create (field. FieldType );
	    		return info;
			}
	
			static  public Info Create (PropertyInfo getter)
			{
	    		Info info = new Info();
	    		info.Create (getter. PropertyType);
	    		return info;
	        }

	    	static public PropCollection CreateCollection (Type type)
	    	{
	    		Info info = new Info();
	    		info.Create (type );
	    		return info.collection;
	    	}
	
			private  void Create (Type type )
			{
				// If retrieving element type gets to buggy, change retrieving element type of collections.
				// In this case element type have to be specified via:
				// void	Property.Set(String name, Class<?> entryType)
				if (type. IsArray)
	            {
					collection =	new PropCollection	( typeof( Array ), type, type. GetElementType(), null);
					access = 		new PropAccess		( typeof( Array ), type, type. GetElementType());
	            }
                else
                {
	                Type[] args;
	                args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
	                if (args != null)
	                {
					    collection = 	new PropCollection	( typeof( IList<>), type, args[0], null);
					    access =		new PropAccess		( typeof( IList<>), type, args[0]);
	                }
	                args = Reflect.GetGenericInterfaceArgs (type, typeof( IKeySet <>) );
				    if (args != null)
					    access = 		new PropAccess		( typeof( IKeySet <> ),type, args[0]);
	
	                args = Reflect.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
				    if (args != null)
				    {
					    collection = 	new PropCollection	( typeof( IDictionary<,> ), type, args[1], args[0]);
					    access = 		new PropAccess		( typeof( IDictionary<,> ), type, args[1]);
				    }
                }
	        }
		}
	}
}