// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Types
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public enum VarType {
        Double,
        Float,
        Long,
        Int,
        Short,
        Byte,
        Bool,
        Object,
    }

#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public ref struct Var {
        private VarType     type;
        private object      obj;
        private long        lng;
        private double      dbl;
        private bool        isNull;

        public  VarType     VarType => type;
        public  bool        IsNull => isNull;

        public object Obj {
            get => obj;
            set { obj = value;         type = VarType.Object; isNull = value == null; }
        }
        
        // --------------- non nullable primitives
        public double Dbl {        
            get => (type == VarType.Double ?                dbl : (double)obj);
            set { dbl = value;         type = VarType.Double; isNull = false; }
        }
        public float Flt {         
            get =>  type == VarType.Float ?         (float) dbl : (float) obj;
            set { dbl = value;         type = VarType.Float; isNull = false; }
        }          
        public long Lng {          
            get =>  (type == VarType.Long ?                 lng : (long)obj);
            set { lng = value;         type = VarType.Long;  isNull = false; }
        }          
        public int Int {           
            get =>  (type == VarType.Int ?           (int)  lng : (int)obj);
            set { lng = value;         type = VarType.Int;   isNull = false; }
        }          
        public short Short {           
            get =>  (type == VarType.Short ?         (short)lng : (short)obj);
            set { lng = value;         type = VarType.Short; isNull = false; }
        }          
        public byte Byte {         
            get => (type == VarType.Byte ?           (byte) lng :  (byte)obj);
            set { lng = value;         type = VarType.Byte;  isNull = false; }
        }
        public bool Bool {
            get =>  (type == VarType.Bool ?            (lng != 0): (bool) obj);
            set { lng = value ? 1 : 0; type = VarType.Bool;  isNull = false; }
        }
        
        // --------------- nullable primitives
        
        public double? NulDbl {
            get => isNull ? null : type == VarType.Double ?         dbl : (double?)obj;
            set { type = VarType.Double; if (value != null) { dbl = (double)value; isNull = false; } else  isNull = true; }
        }
        public float? NulFlt {         
            get => isNull ? null : type == VarType.Float ? (float?) dbl : (float?)obj;
            set { type = VarType.Float;  if (value != null) { dbl = (float)value;  isNull = false; } else  isNull = true; }
        }          
        public long? NulLng {          
            get => isNull ? null : type == VarType.Long ?           lng : (long?)obj;
            set { type = VarType.Long;   if (value != null) { lng = (long)value;   isNull = false; } else  isNull = true; }
        }          
        public int? NulInt {           
            get => isNull ? null : type == VarType.Int ? (int?)     lng : (int?)obj;
            set { type = VarType.Int;    if (value != null) { lng = (int)value;    isNull = false; } else  isNull = true; }
        }          
        public short? NulShort {           
            get => isNull ? null : type == VarType.Short ? (short?) lng : (short?)obj;
            set { type = VarType.Short;  if (value != null) { lng = (short)value;  isNull = false; } else  isNull = true; }
        }          
        public byte? NulByte {         
            get => isNull ? null : type == VarType.Byte ? (byte?)   lng : (byte?)obj;
            set { type = VarType.Byte;   if (value != null) { lng = (byte)value;   isNull = false; } else  isNull = true; }
        }
        public bool? NulBool {
            get => isNull ? null : type == VarType.Bool ?          (dbl != 0) : (bool?)obj;
            set { type = VarType.Bool;   if (value != null) { lng = (bool)value ? 1 : 0; isNull = false; } else  isNull = true; }
        }
        
     
        
        public void SetNull(VarType varType) {
            isNull = true;
            type = varType;
        }
        
        public object Get () {
            if (!isNull) {
                switch (type) {
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
            }
            return null;
        }
        
        public void Set(object value, VarType varType, bool isNullable) {

            switch (varType) {
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

        /// <summary>
        /// Json.Burst support reusing existing object instances when calling <see cref="TypeMapper.Read"/> via its
        /// <see cref="Var"/> parameter. In case no object is available for reusing it need to be set to null before
        /// calling <see cref="TypeMapper.Read"/>
        /// </summary>
        public void  SetObjNull() {
            obj = null;
            isNull = true;
        }

        public override string ToString() {
            string val = null;
            switch (type) {
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
            return $"{val} ({type})";
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