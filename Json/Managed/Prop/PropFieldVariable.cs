// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
// ReSharper disable PossibleNullReferenceException

namespace Friflo.Json.Managed.Prop
{
	// PropFieldVariable
	internal class PropFieldVariable : PropField
	{
		private readonly	FieldInfo 	field;
		
		internal PropFieldVariable(PropType declType, String name, FieldInfo field)
		:
			base (declType, name, SimpleType.IdFromField( field ), field. FieldType, PropCollection.Info.Create(field)) {
			this.field			= field;
		}
	
		public override bool IsAssignable()  { return true; }
		
		// ---- getter
		internal override Object InternalGetObject (Object obj)
		{
			return field. GetValue (obj);
		}
		internal override String InternalGetString (Object obj)
		{
			return (String) field. GetValue (obj);
		}	
		internal override	long InternalGetLong (Object obj)
		{
			return (long) field. GetValue (obj);
		}	
		internal override	int InternalGetInt (Object obj)
		{
			return (int) field. GetValue (obj);
		}
		internal override	short InternalGetShort (Object obj)
		{
			return (short) field. GetValue (obj);
		}
		internal override	byte InternalGetByte (Object obj)
		{
			return (byte) field. GetValue (obj);
		}	
		internal override	bool InternalGetBool (Object obj)
		{
			return (bool) field. GetValue (obj);
		}
		internal override	double InternalGetDouble (Object obj)
		{
			return (double) field. GetValue (obj);
		}
		internal override	float InternalGetFloat (Object obj)
		{
			return (float) field. GetValue (obj);
		}
		
		// ---- setter
		internal override	void InternalSetObject (Object obj, Object val)
		{
			field. SetValue (obj, val);
		}	
		internal override	void InternalSetString (Object obj, String val)
		{
			field. SetValue (obj, val);
		}
		internal override	void InternalSetLong (Object obj, long val)
		{
			field. SetValue (obj, val);
		}
		internal override	void InternalSetInt (Object obj, int val)
		{
			field. SetValue (obj, val);
		}
		internal override	void InternalSetShort (Object obj, short val)
		{
			field. SetValue (obj, val);
		}
		internal override	void InternalSetByte (Object obj, byte val)
		{
			field. SetValue (obj, val);
		}	
		internal override	void InternalSetBool (Object obj, bool val)
		{
			field. SetValue (obj, val);
		}
		internal override	void InternalSetDouble (Object obj, double val)
		{
			field. SetValue (obj, val);
		}
		internal override	void InternalSetFloat (Object obj, float val)
		{
			field. SetValue (obj, val);
		}
	}
}