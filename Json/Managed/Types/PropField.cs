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

        private FrifloException Except()
        {
            return new FrifloException("member doesnt support Get/Set");
        }
        

        public abstract bool IsAssignable();
        
        internal    virtual Object  InternalGetObject   (Object obj)    { throw Except(); }
        internal    virtual String  InternalGetString   (Object obj)    { throw Except(); }
        internal    virtual long    InternalGetLong     (Object obj)    { throw Except(); }
        internal    virtual int     InternalGetInt      (Object obj)    { throw Except(); }
        internal    virtual short   InternalGetShort    (Object obj)    { throw Except(); }
        internal    virtual byte    InternalGetByte     (Object obj)    { throw Except(); }
        internal    virtual bool    InternalGetBool     (Object obj)    { throw Except(); }
        internal    virtual double  InternalGetDouble   (Object obj)    { throw Except(); }
        internal    virtual float   InternalGetFloat    (Object obj)    { throw Except(); }
        
        internal    virtual void    InternalSetObject   (Object obj, Object val)    { throw Except(); }
        internal    virtual void    InternalSetString   (Object obj, String val)    { throw Except(); }
        internal    virtual void    InternalSetLong     (Object obj, long val)      { throw Except(); }
        internal    virtual void    InternalSetInt      (Object obj, int val)       { throw Except(); }
        internal    virtual void    InternalSetShort    (Object obj, short val)     { throw Except(); }
        internal    virtual void    InternalSetByte     (Object obj, byte val)      { throw Except(); }
        internal    virtual void    InternalSetBool     (Object obj, bool val)      { throw Except(); }
        internal    virtual void    InternalSetDouble   (Object obj, double val)    { throw Except(); }
        internal    virtual void    InternalSetFloat    (Object obj, float val)     { throw Except(); }

        //
    }
}
