// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Managed.Codecs;

// ReSharper disable PossibleNullReferenceException

namespace Friflo.Json.Managed.Types
{
    // PropFieldAccessor
    internal class PropFieldAccessor : PropField
    {
        private readonly    PropertyInfo    getter;
        private readonly    PropertyInfo    setter;

        //
        internal PropFieldAccessor(ClassType declType, String name, Type type, PropertyInfo getter, PropertyInfo setter)
        :
            base (declType, name, SimpleType.IdFromMethod( getter  ), Slot.GetSlotType(getter.PropertyType), getter. PropertyType) {
            this.getter = getter;
            this.setter = setter;
        }

        public override bool IsAssignable()  { return setter != null; }

        // ---- getter
        internal override Object InternalGetObject (Object obj)
        {
            return getter.GetValue(obj, null);
        }
        internal override String InternalGetString (Object obj)
        {
            return (String) getter.GetValue(obj, null);
        }   
        internal override   long InternalGetLong (Object obj)
        {
            return (long) getter.GetValue(obj, null);
        }   
        internal override   int InternalGetInt (Object obj)
        {
            return (int) getter.GetValue(obj, null);
        }
        internal override   short InternalGetShort (Object obj)
        {
            return (short) getter.GetValue(obj, null);
        }
        internal override   byte InternalGetByte (Object obj)
        {
            return (byte) getter.GetValue(obj, null);
        }   
        internal override   bool InternalGetBool (Object obj)
        {
            return (bool) getter.GetValue(obj, null);
        }
        internal override   double InternalGetDouble (Object obj)
        {
            return (double) getter.GetValue(obj, null);
        }
        internal override   float InternalGetFloat (Object obj)
        {
            return (float) getter.GetValue(obj, null);
        }
        
        // ---- setter
        internal override   void InternalSetObject (Object obj, Object val)
        {
            setter.SetValue(obj, val, null);
        }   
        internal override   void InternalSetString (Object obj, String val)
        {
            setter.SetValue(obj, val, null);
        }
        internal override   void InternalSetLong (Object obj, long val)
        {
            setter.SetValue(obj, val, null);
        }
        internal override   void InternalSetInt (Object obj, int val)
        {
            setter.SetValue(obj, val, null);
        }
        internal override   void InternalSetShort (Object obj, short val)
        {
            setter.SetValue(obj, val, null);
        }
        internal override   void InternalSetByte (Object obj, byte val)
        {
            setter.SetValue(obj, val, null);
        }   
        internal override   void InternalSetBool (Object obj, bool val)
        {
            setter.SetValue(obj, val, null);
        }
        internal override   void InternalSetDouble (Object obj, double val)
        {
            setter.SetValue(obj, val, null);
        }
        internal override   void InternalSetFloat (Object obj, float val)
        {
            setter.SetValue(obj, val, null);
        }
    }
}
