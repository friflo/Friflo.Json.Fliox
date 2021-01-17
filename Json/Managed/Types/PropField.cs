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
        public   readonly   VarType        slotType;
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
            this.slotType               = Var.GetSlotType(fieldType);
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
            if (slotType == VarType.Object)
                return field.GetValue(obj) ;
            throw new InvalidComObjectException("Expect method is only called for fields with type object. field: " + name);
        }

        public void SetObject (object prop, Object val)
        {
            field.SetValue (prop, val);
        }

        private static readonly bool useDirect = true;
        
        public void SetField (object obj, ref Var val)
        {
            if (field != null) {
                if (useDirect) {
                    switch (val.Cat) {
                        case VarType.Object:   field.SetValue      (obj,            val.Obj);    return;
                        //
                        case VarType.Double:   field.SetValueDirect(__makeref(obj), val.Dbl);    return;
                        case VarType.Float:    field.SetValueDirect(__makeref(obj), val.Flt);    return;
                        //
                        case VarType.Long:     field.SetValueDirect(__makeref(obj), val.Lng);    return;
                        case VarType.Int:      field.SetValueDirect(__makeref(obj), val.Int);    return;
                        case VarType.Short:    field.SetValueDirect(__makeref(obj), val.Short);  return;
                        case VarType.Byte:     field.SetValueDirect(__makeref(obj), val.Byte);   return;
                        //
                        case VarType.Bool:     field.SetValueDirect(__makeref(obj), val.Bool);   return;
                    }
                } else {
                    switch (val.Cat) {
                        case VarType.Object:   field.SetValue (obj, val.Obj);    return;
                        //
                        case VarType.Double:   field.SetValue (obj, val.Dbl);    return;
                        case VarType.Float:    field.SetValue (obj, val.Flt);    return;
                        //
                        case VarType.Long:     field.SetValue (obj, val.Lng);    return;
                        case VarType.Int:      field.SetValue (obj, val.Int);    return;
                        case VarType.Short:    field.SetValue (obj, val.Short);  return;
                        case VarType.Byte:     field.SetValue (obj, val.Byte);   return;
                        //
                        case VarType.Bool:     field.SetValue (obj, val.Bool);   return;
                    }
                }
            } else {
                switch (val.Cat) {
                    case VarType.Object:   getter.SetValue (obj, val.Obj);    return;
                    //
                    case VarType.Double:   getter.SetValue (obj, val.Dbl);    return;
                    case VarType.Float:    getter.SetValue (obj, val.Flt);    return;
                    //
                    case VarType.Long:     getter.SetValue (obj, val.Lng);    return;
                    case VarType.Int:      getter.SetValue (obj, val.Int);    return;
                    case VarType.Short:    getter.SetValue (obj, val.Short);  return;
                    case VarType.Byte:     getter.SetValue (obj, val.Byte);   return;
                    //
                    case VarType.Bool:     getter.SetValue (obj, val.Bool);   return;
                }
            }
        }
        
        // ReSharper disable PossibleNullReferenceException
        public void GetField (object obj, ref Var val)
        {
            if (field != null) {
                if (useDirect) {
                    switch (slotType) {
                        case VarType.Object:   val.Obj     =           field.GetValueDirect (__makeref(obj)); return;
                     
                        case VarType.Double:   val.Dbl     = (double)  field.GetValueDirect (__makeref(obj)); return;
                        case VarType.Float:    val.Flt     = (float)   field.GetValueDirect (__makeref(obj)); return;
                     
                        case VarType.Long:     val.Lng     = (long)    field.GetValueDirect (__makeref(obj)); return;
                        case VarType.Int:      val.Int     = (int)     field.GetValueDirect (__makeref(obj)); return;
                        case VarType.Short:    val.Short   = (short)   field.GetValueDirect (__makeref(obj)); return;
                        case VarType.Byte:     val.Byte    = (byte)    field.GetValueDirect (__makeref(obj)); return;
                     
                        case VarType.Bool:     val.Bool    = (bool)    field.GetValueDirect (__makeref(obj)); return;
                    }
                } else {
                    switch (slotType) {
                        case VarType.Object:   val.Obj     =           field.GetValue   (obj); return;
                       
                        case VarType.Double:   val.Dbl     = (double)  field.GetValue   (obj); return;
                        case VarType.Float:    val.Flt     = (float)   field.GetValue   (obj); return;
                       
                        case VarType.Long:     val.Lng     = (long)    field.GetValue   (obj); return;
                        case VarType.Int:      val.Int     = (int)     field.GetValue   (obj); return;
                        case VarType.Short:    val.Short   = (short)   field.GetValue   (obj); return;
                        case VarType.Byte:     val.Byte    = (byte)    field.GetValue   (obj); return;
                       
                        case VarType.Bool:     val.Bool    = (bool)    field.GetValue   (obj); return;
                    }
                }
            } else {
                switch (slotType) {
                    case VarType.Object:   val.Obj     =           setter.GetValue   (obj);    return;
                    //
                    case VarType.Double:   val.Dbl     = (double)  setter.GetValue   (obj);    return;
                    case VarType.Float:    val.Flt     = (float)   setter.GetValue   (obj);    return;
                    //
                    case VarType.Long:     val.Lng     = (long)    setter.GetValue   (obj);    return;
                    case VarType.Int:      val.Int     = (int)     setter.GetValue   (obj);    return;
                    case VarType.Short:    val.Short   = (short)   setter.GetValue   (obj);    return;
                    case VarType.Byte:     val.Byte    = (byte)    setter.GetValue   (obj);    return;
                    //
                    case VarType.Bool:     val.Bool    = (bool)    setter.GetValue   (obj);    return;
                }
            }
        }
    }
}
