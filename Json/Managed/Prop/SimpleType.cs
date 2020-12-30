using System;
using System.Reflection;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
	public class SimpleType
	{
		public enum ID
		{
			String,
			Long,	// first number
			Integer,
			Short,
			Byte,
			Bool,
			Double,
			Float,	// last number
			Object,
			Method,
		}

		public static bool IsNumber(ID type)
		{

			return (ID.Long <= type && type <= ID.Float);
		}

		public static ID ? IdFromType (Type type)
		{
			if 		(type == typeof( String		)) 	return ID.String;
			else if (type == typeof( long		)) 	return ID.Long;
			else if (type == typeof( int		)) 	return ID.Integer;
			else if (type == typeof( short		)) 	return ID.Short;
			else if (type == typeof( byte		)) 	return ID.Byte;
			else if (type == typeof( bool		)) 	return ID.Bool;
			else if	(type == typeof( double		)) 	return ID.Double;
			else if	(type == typeof( float		)) 	return ID.Float;
			else if	(Reflect.IsAssignableFrom (typeof(Object), type)) 	return ID.Object;
			return null;
		}
	
		public static ID IdFromField (FieldInfo field)
		{
			Type type = field. FieldType;
			ID ? id = IdFromType (type);
			if (id == null)
				throw new FrifloException("unsupported simple type: " + type. FullName + " of field " + field. Name);
			return id .Value;
		}

		public static ID IdFromMethod (PropertyInfo method)
		{
			Type type = method. PropertyType;
			ID ? id = IdFromType (type);
			if (id == null)
				throw new FrifloException("unsupported simple type: " + type. FullName + " of method " + method. Name);
			return id .Value;
		}

/*		public static bool IsAssignable (SimpleType.ID typeID)
		{
			switch (typeID)
			{
			case SimpleType.ID. String:
			case SimpleType.ID. Long:
			case SimpleType.ID. Integer:
			case SimpleType.ID. Short:
			case SimpleType.ID. Byte:
			case SimpleType.ID. Bool:
			case SimpleType.ID. Double:
			case SimpleType.ID. Float:
			case SimpleType.ID. Object:
				return true;
			default:
				return false;
			}	
		}
		*/
	}
}
