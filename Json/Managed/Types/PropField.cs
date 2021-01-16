// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Friflo.Json.Burst;

namespace Friflo.Json.Managed.Types
{
    public class PropField : IDisposable
    {
        internal readonly   String          name;
        public   readonly   SlotType        slotType;
        public              StubType        FieldType { get; internal set; }    // never null 
        internal readonly   Type            fieldTypeNative;                    // never null 
        private  readonly   ClassType       declType;
        internal            Bytes           nameBytes;
        internal            ConstructorInfo collectionConstructor;
        //
        private readonly    FieldInfo       field;
        private readonly    PropertyInfo    getter;
        private readonly    PropertyInfo    setter;

        internal PropField (ClassType declType, String name, Type fieldType, FieldInfo field, PropertyInfo getter, PropertyInfo setter)
        {
            this.declType               = declType;
            this.name                   = name;
            this.nameBytes              = new Bytes(name);
            this.fieldTypeNative        = fieldType;
            this.slotType               = Slot.GetSlotType(fieldType);
            //
            this.field  = field;
            this.getter = getter;
            this.setter = setter;
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
        
        public object GetObject (Object obj)
        {
            if (slotType == SlotType.Object)
                return field.GetValue(obj) ;
            throw new InvalidComObjectException("Expect method is only called for fields with type object. field: " + name);
        }

        public void SetObject (object prop, Object val)
        {
            field.SetValue (prop, val);
        }

        private static readonly bool useDirect = true;
        
        public void SetField (object obj, ref Slot val)
        {
            if (field != null) {
                if (useDirect) {
                    switch (val.Cat) {
                        case SlotType.Object:   field.SetValue      (obj,            val.Obj);    return;
                        //
                        case SlotType.Double:   field.SetValueDirect(__makeref(obj), val.Dbl);    return;
                        case SlotType.Float:    field.SetValueDirect(__makeref(obj), val.Flt);    return;
                        //
                        case SlotType.Long:     field.SetValueDirect(__makeref(obj), val.Lng);    return;
                        case SlotType.Int:      field.SetValueDirect(__makeref(obj), val.Int);    return;
                        case SlotType.Short:    field.SetValueDirect(__makeref(obj), val.Short);  return;
                        case SlotType.Byte:     field.SetValueDirect(__makeref(obj), val.Byte);   return;
                        //
                        case SlotType.Bool:     field.SetValueDirect(__makeref(obj), val.Bool);   return;
                    }
                } else {
                    switch (val.Cat) {
                        case SlotType.Object:   field.SetValue (obj, val.Obj);    return;
                        //
                        case SlotType.Double:   field.SetValue (obj, val.Dbl);    return;
                        case SlotType.Float:    field.SetValue (obj, val.Flt);    return;
                        //
                        case SlotType.Long:     field.SetValue (obj, val.Lng);    return;
                        case SlotType.Int:      field.SetValue (obj, val.Int);    return;
                        case SlotType.Short:    field.SetValue (obj, val.Short);  return;
                        case SlotType.Byte:     field.SetValue (obj, val.Byte);   return;
                        //
                        case SlotType.Bool:     field.SetValue (obj, val.Bool);   return;
                    }
                }
            } else {
                switch (val.Cat) {
                    case SlotType.Object:   getter.SetValue (obj, val.Obj);    return;
                    //
                    case SlotType.Double:   getter.SetValue (obj, val.Dbl);    return;
                    case SlotType.Float:    getter.SetValue (obj, val.Flt);    return;
                    //
                    case SlotType.Long:     getter.SetValue (obj, val.Lng);    return;
                    case SlotType.Int:      getter.SetValue (obj, val.Int);    return;
                    case SlotType.Short:    getter.SetValue (obj, val.Short);  return;
                    case SlotType.Byte:     getter.SetValue (obj, val.Byte);   return;
                    //
                    case SlotType.Bool:     getter.SetValue (obj, val.Bool);   return;
                }
            }
        }
        
        // ReSharper disable PossibleNullReferenceException
        public void GetField (object obj, ref Slot val)
        {
            if (field != null) {
                if (useDirect) {
                    switch (slotType) {
                        case SlotType.Object:   val.Obj     =           field.GetValueDirect (__makeref(obj)); return;
                     
                        case SlotType.Double:   val.Dbl     = (double)  field.GetValueDirect (__makeref(obj)); return;
                        case SlotType.Float:    val.Flt     = (float)   field.GetValueDirect (__makeref(obj)); return;
                     
                        case SlotType.Long:     val.Lng     = (long)    field.GetValueDirect (__makeref(obj)); return;
                        case SlotType.Int:      val.Int     = (int)     field.GetValueDirect (__makeref(obj)); return;
                        case SlotType.Short:    val.Short   = (short)   field.GetValueDirect (__makeref(obj)); return;
                        case SlotType.Byte:     val.Byte    = (byte)    field.GetValueDirect (__makeref(obj)); return;
                     
                        case SlotType.Bool:     val.Bool    = (bool)    field.GetValueDirect (__makeref(obj)); return;
                    }
                } else {
                    switch (slotType) {
                        case SlotType.Object:   val.Obj     =           field.GetValue   (obj); return;
                       
                        case SlotType.Double:   val.Dbl     = (double)  field.GetValue   (obj); return;
                        case SlotType.Float:    val.Flt     = (float)   field.GetValue   (obj); return;
                       
                        case SlotType.Long:     val.Lng     = (long)    field.GetValue   (obj); return;
                        case SlotType.Int:      val.Int     = (int)     field.GetValue   (obj); return;
                        case SlotType.Short:    val.Short   = (short)   field.GetValue   (obj); return;
                        case SlotType.Byte:     val.Byte    = (byte)    field.GetValue   (obj); return;
                       
                        case SlotType.Bool:     val.Bool    = (bool)    field.GetValue   (obj); return;
                    }
                }
            } else {
                switch (slotType) {
                    case SlotType.Object:   val.Obj     =           setter.GetValue   (obj);    return;
                    //
                    case SlotType.Double:   val.Dbl     = (double)  setter.GetValue   (obj);    return;
                    case SlotType.Float:    val.Flt     = (float)   setter.GetValue   (obj);    return;
                    //
                    case SlotType.Long:     val.Lng     = (long)    setter.GetValue   (obj);    return;
                    case SlotType.Int:      val.Int     = (int)     setter.GetValue   (obj);    return;
                    case SlotType.Short:    val.Short   = (short)   setter.GetValue   (obj);    return;
                    case SlotType.Byte:     val.Byte    = (byte)    setter.GetValue   (obj);    return;
                    //
                    case SlotType.Bool:     val.Bool    = (bool)    setter.GetValue   (obj);    return;
                }
            }
        }
    }
}
