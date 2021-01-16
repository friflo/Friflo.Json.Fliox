// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Types
{

    public abstract class PropField : IDisposable
    {
        internal readonly   String          name;
        public   readonly   SlotType        slotType;
        public              StubType        FieldType { get; internal set; }    // never null 
        internal readonly   Type            fieldTypeNative;                    // never null 
        private  readonly   ClassType       declType;
        internal            Bytes           nameBytes;
        internal            ConstructorInfo collectionConstructor;

        internal PropField (ClassType declType, String name, SlotType slotType, Type fieldType)
        {
            this.declType               = declType;
            this.name                   = name;
            this.nameBytes              = new Bytes(name);
            this.fieldTypeNative        = fieldType;
            this.slotType               = slotType;
            if (fieldType == null)
                throw new InvalidOperationException("Expect fieldType non null");
        }

        public void Dispose() {
            nameBytes.Dispose();
        }

        public void AppendName(ref Bytes bb)
        {
            bb.AppendBytes(ref nameBytes);
        }
        
        public object GetObject (Object prop)
        {
            if (slotType == SlotType.Object)
                return InternalGetObject    (prop) ;
            throw new InvalidComObjectException("Expect method is only called for fields with type object. field: " + name);
        }

        public void SetObject (object prop, Object val)
        {
            InternalSetObject(prop, val);
        }
        
        public void SetField (object prop, ref Slot val)
        {
            switch (val.Cat) {
                case SlotType.Object:   InternalSetObject   (prop, val.Obj);    return;
                //
                case SlotType.Double:   InternalSetDouble   (prop, val.Dbl);    return;
                case SlotType.Float:    InternalSetFloat    (prop, val.Flt);    return;
                //
                case SlotType.Long:     InternalSetLong     (prop, val.Lng);    return;
                case SlotType.Int:      InternalSetInt      (prop, val.Int);    return;
                case SlotType.Short:    InternalSetShort    (prop, val.Short);  return;
                case SlotType.Byte:     InternalSetByte     (prop, val.Byte);   return;
                //
                case SlotType.Bool:     InternalSetBool     (prop, val.Bool);   return;
            }
        }
        
        public void GetField (object prop, ref Slot val)
        {
            switch (slotType) {
                case SlotType.Object:   val.Obj     = InternalGetObject   (prop);    return;
                //
                case SlotType.Double:   val.Dbl     = InternalGetDouble   (prop);    return;
                case SlotType.Float:    val.Flt     = InternalGetFloat    (prop);    return;
                //
                case SlotType.Long:     val.Lng     = InternalGetLong     (prop);    return;
                case SlotType.Int:      val.Int     = InternalGetInt      (prop);    return;
                case SlotType.Short:    val.Short   = InternalGetShort    (prop);    return;
                case SlotType.Byte:     val.Byte    = InternalGetByte     (prop);    return;
                //
                case SlotType.Bool:     val.Bool    = InternalGetBool     (prop);    return;
            }
        }

        internal    abstract Object InternalGetObject   (Object obj);
        internal    abstract long    InternalGetLong     (Object obj);
        internal    abstract int     InternalGetInt      (Object obj);
        internal    abstract short   InternalGetShort    (Object obj);
        internal    abstract byte    InternalGetByte     (Object obj);
        internal    abstract bool    InternalGetBool     (Object obj);
        internal    abstract double  InternalGetDouble   (Object obj);
        internal    abstract float   InternalGetFloat    (Object obj);
        
        internal    abstract void    InternalSetObject   (Object obj, Object val);
        internal    abstract void    InternalSetLong     (Object obj, long val);
        internal    abstract void    InternalSetInt      (Object obj, int val);
        internal    abstract void    InternalSetShort    (Object obj, short val);
        internal    abstract void    InternalSetByte     (Object obj, byte val);
        internal    abstract void    InternalSetBool     (Object obj, bool val);
        internal    abstract void    InternalSetDouble   (Object obj, double val);
        internal    abstract void    InternalSetFloat    (Object obj, float val);

        //
    }
}
