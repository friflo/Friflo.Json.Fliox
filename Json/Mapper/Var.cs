// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper
{

    public enum VarType {
        None,
        Double,
        Float,
        Long,
        Int,
        Short,
        Byte,
        Bool,
        Object,
    }

    public ref struct Var {
        private object      obj;
        private long        lng;
        private double      dbl;
        private bool        isNull;
        
        
        public VarType     Cat { get;  private set; }

        
        public bool IsNull => isNull;

        public object Obj {
            get => obj;
            set { obj = value;         Cat = VarType.Object; isNull = value == null; }
        }
        
        // --------------- non nullable primitives
        public double Dbl {        
            get => (double)(Cat == VarType.Double ?        dbl : obj);
            set { dbl = value;         Cat = VarType.Double; isNull = false; }
        }
        public float Flt {         
            get =>  Cat == VarType.Float ?         (float) dbl : (float) obj;
            set { dbl = value;         Cat = VarType.Float; isNull = false; }
        }          
        public long Lng {          
            get =>  (Cat == VarType.Long ?                 lng : (long)obj);
            set { lng = value;         Cat = VarType.Long;  isNull = false; }
        }          
        public int Int {           
            get =>  (Cat == VarType.Int ?           (int)  lng : (int)obj);
            set { lng = value;         Cat = VarType.Int;   isNull = false; }
        }          
        public short Short {           
            get =>  (Cat == VarType.Short ?         (short)lng : (short)obj);
            set { lng = value;         Cat = VarType.Short; isNull = false; }
        }          
        public byte Byte {         
            get => (Cat == VarType.Byte ?           (byte) lng :  (byte)obj);
            set { lng = value;         Cat = VarType.Byte;  isNull = false; }
        }
        public bool Bool {
            get =>  (Cat == VarType.Bool ?            (lng != 0): (bool) obj);
            set { lng = value ? 1 : 0; Cat = VarType.Bool;  isNull = false; }
        }
        
        // --------------- nullable primitives
        
        public double? NulDbl {
            get => isNull ? null : Cat == VarType.Double ?         dbl : (double?)obj;
            set { Cat = VarType.Double; if (value != null) { dbl = (double)value; isNull = false; } else  isNull = true; }
        }
        public float? NulFlt {         
            get => isNull ? null : Cat == VarType.Float ? (float?) dbl : (float?)obj;
            set { Cat = VarType.Float;  if (value != null) { dbl = (float)value;  isNull = false; } else  isNull = true; }
        }          
        public long? NulLng {          
            get => isNull ? null : Cat == VarType.Long ?           lng : (long?)obj;
            set { Cat = VarType.Long;   if (value != null) { lng = (long)value;   isNull = false; } else  isNull = true; }
        }          
        public int? NulInt {           
            get => isNull ? null : Cat == VarType.Int ? (int?)     lng : (int?)obj;
            set { Cat = VarType.Int;    if (value != null) { lng = (int)value;    isNull = false; } else  isNull = true; }
        }          
        public short? NulShort {           
            get => isNull ? null : Cat == VarType.Short ? (short?) lng : (short?)obj;
            set { Cat = VarType.Short;  if (value != null) { lng = (short)value;  isNull = false; } else  isNull = true; }
        }          
        public byte? NulByte {         
            get => isNull ? null : Cat == VarType.Byte ? (byte?)   lng : (byte?)obj;
            set { Cat = VarType.Byte;   if (value != null) { lng = (byte)value;   isNull = false; } else  isNull = true; }
        }
        public bool? NulBool {
            get => isNull ? null : Cat == VarType.Bool ?          (dbl != 0) : (bool?)obj;
            set { Cat = VarType.Bool;   if (value != null) { lng = (bool)value ? 1 : 0; isNull = false; } else  isNull = true; }
        }
        
     
        
        public void SetNull(VarType varType) {
            isNull = true;
            Cat = varType;
        }
        
        public object Get () {
            if (isNull)
                return null;

            switch (Cat) {
                case VarType.None:      return null;
                case VarType.Object:    return obj;
                //
                case VarType.Double:    return          dbl;
                case VarType.Float:     return (float)  dbl;
                //
                case VarType.Long:      return          lng;
                case VarType.Int:       return (int)    lng;
                case VarType.Short:     return (short)  lng;
                case VarType.Byte:      return (byte)   lng;
                //
                case VarType.Bool:      return lng != 0;
            }
            return null; // unreachable
        }
        
        public void Set(object value, VarType varType, bool isNullable) {

            switch (varType) {
                case VarType.None:                      Cat = VarType.None; return; // throw new InvalidOperationException("Must not set Var to None);
                case VarType.Object:                    Obj = value;        return;
                //
                case VarType.Double: if (!isNullable) { Dbl =   (double)value; } else { if (value == null) SetNull(VarType.Double); else Dbl   = (double) value; } return;
                case VarType.Float:  if (!isNullable) { Flt =   (float) value; } else { if (value == null) SetNull(VarType.Float);  else Flt   = (float)  value; } return;
                //
                case VarType.Long:   if (!isNullable) { Lng =   (long)  value; } else { if (value == null) SetNull(VarType.Long);   else Lng   = (long)   value; } return;
                case VarType.Int:    if (!isNullable) { Int =   (int)   value; } else { if (value == null) SetNull(VarType.Int);    else Int   = (int)    value; } return;
                case VarType.Short:  if (!isNullable) { Short = (short) value; } else { if (value == null) SetNull(VarType.Short);  else Short = (short)  value; } return;
                case VarType.Byte:   if (!isNullable) { Byte =  (byte)  value; } else { if (value == null) SetNull(VarType.Byte);   else Byte  = (byte)   value; } return;
                //
                case VarType.Bool:   if (!isNullable) { Bool =  (bool)  value; } else { if (value == null) SetNull(VarType.Bool);   else Bool  = (bool)   value; } return;
            }
        }

        public void  Clear() {
            Cat = VarType.None;
            obj = null;
            lng = 0;
            dbl = 0;
        }

        public override string ToString() {
            string val = null;
            switch (Cat) {
                case VarType.None:     return "None";
                case VarType.Object:   return isNull ? "null" : $"{obj} ({obj.GetType().Name})";
                //
                case VarType.Double:   val = isNull ? "null" : dbl.ToString(CultureInfo.InvariantCulture); break;
                case VarType.Float:    val = isNull ? "null" : dbl.ToString(CultureInfo.InvariantCulture); break;
                //
                case VarType.Long:     val = isNull ? "null" : lng.ToString(CultureInfo.InvariantCulture); break;
                case VarType.Int:      val = isNull ? "null" : lng.ToString(CultureInfo.InvariantCulture); break;
                case VarType.Short:    val = isNull ? "null" : lng.ToString(CultureInfo.InvariantCulture); break;
                case VarType.Byte:     val = isNull ? "null" : lng.ToString(CultureInfo.InvariantCulture); break;
                //
                case VarType.Bool:     val = isNull ? "null" : lng != 0 ? "true" : "false";              break;
            }
            return $"{val} ({Cat})";
        }
        
        public static VarType GetVarType (Type type)
        {
            if (type == typeof( double     ) || type == typeof( double?     ))  return VarType.Double;
            if (type == typeof( float      ) || type == typeof( float?      ))  return VarType.Float;
            //
            if (type == typeof( long       ) || type == typeof( long?       ))  return VarType.Long;
            if (type == typeof( int        ) || type == typeof( int?        ))  return VarType.Int;
            if (type == typeof( short      ) || type == typeof( short?      ))  return VarType.Short;
            if (type == typeof( byte       ) || type == typeof( byte?       ))  return VarType.Byte;
            //
            if (type == typeof( bool       ) || type == typeof( bool?       ))  return VarType.Bool;
            //
            if (Reflect.IsAssignableFrom(typeof(Object), type))                 return VarType.Object;

            throw new InvalidOperationException("Type not supported. Type: " + type);
        }


    }
}