// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{

	public abstract class PropField : IDisposable
	{
		internal readonly 	String 			name;
		internal readonly 	MethodInfo		method;
		internal readonly 	SimpleType.Id	type;
		public	 readonly 	Type			fieldType;
		public 	 readonly	PropCollection	collection;
		public 	 readonly	PropAccess		access;
		public	 readonly	PropType		declType;
		internal		 	Bytes			nameBytes;
		private				PropType		fieldPropType;
        private  readonly   ConstructorInfo collectionConstructor;

		public PropType GetFieldPropType(PropType.Cache cache)
		{
			if (fieldPropType != null)
				return fieldPropType;
			return fieldPropType = cache.Get(fieldType);		
		}
	
		internal PropField (PropType declType, String name, SimpleType.Id type, Type fieldType, PropCollection.Info info)
		{
			this.declType				= declType;
			this.name 					= name;
			this.nameBytes				= new Bytes(name);
			this.method					= null;
			this.type					= type;
			this.fieldType				= fieldType;
			this.collection				= info.collection;
			this.access					= info.access;
			this.collectionConstructor 	= collection != null ? collection.constructor : null;
		}

		internal PropField (PropType declType, String name, MethodInfo method)
		{
			this.declType				= declType;
			this.name 					= name;
			this.nameBytes				= new Bytes(name);
			this.method					= method;
			this.type					= SimpleType.Id.Method;
			this.fieldType				= null;
			this.collection				= null;
			this.access					= null;
            this.collectionConstructor	= null;
		}

		public void Dispose() {
			nameBytes.Dispose();
		}

		public void AppendName(ref Bytes bb)
		{
			bb.AppendBytes(ref nameBytes);
		}

	    public Object CreateCollection ()
	    {
	    	if (collectionConstructor != null)
	    		return Reflect.CreateInstance(collectionConstructor);
	    	return null;
	    }

		public String GetString (Object prop)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:		return InternalGetString	(prop);
				case SimpleType.Id. Long:		return InternalGetLong 		(prop) .ToString(NumberFormatInfo.InvariantInfo);
				case SimpleType.Id. Integer:	return InternalGetInt		(prop) .ToString(NumberFormatInfo.InvariantInfo);
				case SimpleType.Id. Short:		return InternalGetShort		(prop) .ToString(NumberFormatInfo.InvariantInfo);
				case SimpleType.Id. Byte:		return InternalGetByte		(prop) .ToString(NumberFormatInfo.InvariantInfo);
				case SimpleType.Id. Bool:		return InternalGetBool		(prop) ? "true"  : "false";
				case SimpleType.Id. Double:		return InternalGetDouble	(prop) .ToString(NumberFormatInfo.InvariantInfo);
				case SimpleType.Id. Float:		return InternalGetFloat		(prop) .ToString(NumberFormatInfo.InvariantInfo);
				default:
					throw new FrifloException("unhandled case for field: " + name);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		public void SetString (Object prop, String val)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:		InternalSetString	(prop, val) ;					break;
				case SimpleType.Id. Long:		InternalSetLong 	(prop, long.	Parse (val,NumberFormatInfo.InvariantInfo) );	break;
				case SimpleType.Id. Integer:	InternalSetInt		(prop, int.		Parse (val, NumberFormatInfo.InvariantInfo) );	break;
				case SimpleType.Id. Short:		InternalSetShort	(prop, short.	Parse (val, NumberFormatInfo.InvariantInfo) );	break;
				case SimpleType.Id. Byte:		InternalSetByte		(prop, byte.	Parse (val, NumberFormatInfo.InvariantInfo) );	break;
				case SimpleType.Id. Bool:		InternalSetBool		(prop, bool.	Parse (val) );	                                break;
				case SimpleType.Id. Double:		InternalSetDouble	(prop, double.	Parse (val, NumberFormatInfo.InvariantInfo) );	break;
				case SimpleType.Id. Float:		InternalSetFloat	(prop, float.	Parse (val, NumberFormatInfo.InvariantInfo) );	break;
				default:
					throw new FrifloException("unhandled case for field: " + name);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}
		
		public Object GetObject (Object prop)
		{
			try
			{
				if (type == SimpleType. Id.Object)
					return InternalGetObject	(prop) ;
				throw new FrifloException("unhandled case for field: " + name + " type: " + type);
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		public void SetObject (Object prop, Object val)
		{
			if (type == SimpleType.Id.Object)
			{
				try
				{
					InternalSetObject(prop, val);
					return;
				}
				catch (Exception e)
				{
					throw new FrifloException("Set field failed. field: " + name, e);
				}
			}
			throw new FrifloException("unhandled case for field: " + name);
		}

		public long GetLong (Object prop)
		{
			if (type == SimpleType.Id.Long)
			{
				try
				{
					return InternalGetLong (prop);
				}
				catch (Exception e)
				{
					throw new FrifloException("Set field failed. field: " + name, e);
				}
			}
			else
				return GetInt (prop);		
		}
		
		public void SetLong (Object prop, long val)
		{
			if (type == SimpleType.Id.Long)
			{
				try
				{
					InternalSetLong (prop, val);
				}
				catch (Exception e)
				{
					throw new FrifloException("Set field failed. field: " + name, e);
				}
			}
			else
				SetInt (prop, (int)val);	
		}

		public int GetInt (Object prop)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:
					String str = 								InternalGetString	(prop) ;
					throw new FrifloException("No conversion to bool. field: " + name + " val: " + str);
				case SimpleType.Id. Long:		return (int)	InternalGetLong		(prop) ;
				case SimpleType.Id. Integer:	return			InternalGetInt		(prop) ;
				case SimpleType.Id. Short:		return 			InternalGetShort	(prop) ;
				case SimpleType.Id. Byte:		return 			InternalGetByte		(prop) ;
				case SimpleType.Id. Bool:		return 			InternalGetBool		(prop) ? 1 : 0;
				case SimpleType.Id. Double:		return (int)	InternalGetDouble	(prop) ;
				case SimpleType.Id. Float:		return (int)	InternalGetFloat	(prop) ;
				default:
					throw new FrifloException("unhandled case for field: " + name);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		public void SetInt (Object prop, int val)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:		InternalSetString	(prop, val .ToString(NumberFormatInfo.InvariantInfo));		break;
				case SimpleType.Id. Long:		InternalSetLong		(prop, 			val);			break;
				case SimpleType.Id. Integer:	InternalSetInt		(prop,			val);			break;
				case SimpleType.Id. Short:		InternalSetShort	(prop, (short)	val);			break;
				case SimpleType.Id. Byte:		InternalSetByte		(prop, (byte)	val);			break;
				case SimpleType.Id. Bool:		InternalSetBool		(prop, 			val != 0 );		break; 
				case SimpleType.Id. Double:		InternalSetDouble	(prop, 			val);			break;
				case SimpleType.Id. Float:		InternalSetFloat	(prop, 			val);			break;
				default:						throw new FrifloException ("no conversion to int. type: " + type);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		public double GetDouble (Object prop)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:		return Double.Parse ( InternalGetString(prop) ,  NumberFormatInfo.InvariantInfo);
				case SimpleType.Id. Long:		return InternalGetLong		(prop) ;
				case SimpleType.Id. Integer:	return InternalGetInt		(prop) ;
				case SimpleType.Id. Short:		return InternalGetShort		(prop) ;
				case SimpleType.Id. Byte:		return InternalGetByte		(prop) ;
				case SimpleType.Id. Bool:		return InternalGetBool		(prop) ? 1 : 0;
				case SimpleType.Id. Double:		return InternalGetDouble	(prop) ;
				case SimpleType.Id. Float:		return InternalGetFloat		(prop) ;
				default:
					throw new FrifloException("unhandled case for field: " + name);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		public void SetDouble (Object prop, double val)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:		InternalSetString	(prop, val .ToString (NumberFormatInfo.InvariantInfo));		break;
				case SimpleType.Id. Long:		InternalSetLong		(prop, (long)	val);			break;
				case SimpleType.Id. Integer:	InternalSetInt		(prop, (int)	val);			break;
				case SimpleType.Id. Short:		InternalSetShort	(prop, (short)	val);			break;
				case SimpleType.Id. Byte:		InternalSetByte		(prop, (byte)	val);			break;
				case SimpleType.Id. Bool:		InternalSetBool		(prop, 			val != 0 );		break; 
				case SimpleType.Id. Double:		InternalSetDouble	(prop, 			val);			break;
				case SimpleType.Id. Float:		InternalSetFloat	(prop, (float)	val);			break;
				default:						throw new FrifloException ("no conversion to double. type: " + type);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		public float GetFloat (Object prop)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:		return Single.Parse ( InternalGetString(prop) ,  NumberFormatInfo.InvariantInfo);
				case SimpleType.Id. Long:		return InternalGetLong				(prop) ;
				case SimpleType.Id. Integer:	return InternalGetInt				(prop) ;
				case SimpleType.Id. Short:		return InternalGetShort				(prop) ;
				case SimpleType.Id. Byte:		return InternalGetByte				(prop) ;
				case SimpleType.Id. Bool:		return InternalGetBool				(prop) ? 1 : 0;
				case SimpleType.Id. Double:		return (float) InternalGetDouble	(prop) ;
				case SimpleType.Id. Float:		return InternalGetFloat				(prop) ;
				default:
					throw new FrifloException("unhandled case for field: " + name);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}
	
		public void SetFloat (Object prop, float val)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:	    InternalSetString	(prop, val .ToString(NumberFormatInfo.InvariantInfo));	break;
				case SimpleType.Id. Long:		InternalSetLong		(prop, (long)	val);			break;
				case SimpleType.Id. Integer:	InternalSetInt		(prop, (int)	val);			break;
				case SimpleType.Id. Short:		InternalSetShort	(prop, (short)	val);			break;
				case SimpleType.Id. Byte:		InternalSetByte		(prop, (byte)	val);			break;
				case SimpleType.Id. Bool:		InternalSetBool		(prop, 			val != 0 );		break;
				case SimpleType.Id. Double:	    InternalSetDouble	(prop, 			val);			break;
				case SimpleType.Id. Float:		InternalSetFloat	(prop, 			val);			break;
				default:		throw new FrifloException ("no conversion to double. type: " + type);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		public bool GetBool (Object prop)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:
					String str =					   InternalGetString	(prop) ;
					if (str. Equals ("true"))	return true;
					if (str. Equals ("false"))	return false;
					throw new FrifloException("No conversion to bool. field: " + name + " val: " + str);
				case SimpleType.Id. Long:		return InternalGetLong		(prop) != 0;
				case SimpleType.Id. Integer:	return InternalGetInt		(prop) != 0;
				case SimpleType.Id. Short:		return InternalGetShort		(prop) != 0;
				case SimpleType.Id. Byte:		return InternalGetByte		(prop) != 0;
				case SimpleType.Id. Bool:		return InternalGetBool		(prop);
				case SimpleType.Id. Double:		return InternalGetDouble	(prop) != 0;
				case SimpleType.Id. Float:		return InternalGetFloat		(prop) != 0;
				default:
					throw new FrifloException("unhandled case for field: " + name);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		public void SetBool (Object prop, bool val)
		{
			try
			{
				switch (type)
				{
				case SimpleType.Id. String:		InternalSetString 	(prop, val ? "true" : "false");		break;
				case SimpleType.Id. Long:		InternalSetLong		(prop,			(val ? 1 : 0));		break;
				case SimpleType.Id. Integer:	InternalSetInt		(prop,			(val ? 1 : 0));		break;
				case SimpleType.Id. Short:		InternalSetShort	(prop, (short)	(val ? 1 : 0));		break;
				case SimpleType.Id. Byte:		InternalSetByte		(prop, (byte)	(val ? 1 : 0));		break;
				case SimpleType.Id. Bool:		InternalSetBool		(prop, 			 val 		 );		break;
				case SimpleType.Id. Double:		InternalSetDouble	(prop,			(val ? 1 : 0));		break;
				case SimpleType.Id. Float:		InternalSetFloat	(prop,			(val ? 1 : 0));		break;
				default:						throw new FrifloException ("no conversion to bool. type: " + type);
				}
			}
			catch (Exception e)
			{
				throw new FrifloException("Set field failed. field: " + name, e);
			}
		}

		private FrifloException Except()
		{
			return new FrifloException("member doesnt support Get/Set");
		}
		

		public abstract bool IsAssignable();
		
		internal	virtual Object	InternalGetObject	(Object obj) 	{ throw Except(); }
		internal	virtual String	InternalGetString	(Object obj) 	{ throw Except(); }
		internal	virtual long	InternalGetLong		(Object obj) 	{ throw Except(); }
		internal	virtual int		InternalGetInt		(Object obj) 	{ throw Except(); }
		internal	virtual short	InternalGetShort	(Object obj) 	{ throw Except(); }
		internal	virtual byte	InternalGetByte		(Object obj) 	{ throw Except(); }
		internal	virtual bool	InternalGetBool		(Object obj) 	{ throw Except(); }
		internal	virtual double	InternalGetDouble	(Object obj) 	{ throw Except(); }
		internal	virtual float	InternalGetFloat	(Object obj) 	{ throw Except(); }
		
		internal	virtual void	InternalSetObject	(Object obj, Object val) 	{ throw Except(); }
		internal	virtual void	InternalSetString	(Object obj, String val)	{ throw Except(); }
		internal	virtual void	InternalSetLong		(Object obj, long val)		{ throw Except(); }
		internal	virtual void	InternalSetInt		(Object obj, int val)		{ throw Except(); }
		internal	virtual void	InternalSetShort	(Object obj, short val)		{ throw Except(); }
		internal	virtual void	InternalSetByte		(Object obj, byte val)		{ throw Except(); }
		internal	virtual void	InternalSetBool		(Object obj, bool val)		{ throw Except(); }
		internal	virtual void	InternalSetDouble	(Object obj, double val)	{ throw Except(); }
		internal	virtual void	InternalSetFloat	(Object obj, float val)		{ throw Except(); }

		//
	}
}
