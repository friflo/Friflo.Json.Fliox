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

        public bool IsNull() {
            return Cat == VarType.Object && obj == null;
        }

        public object Obj {
            get => obj;
            set { obj = value;         Cat = VarType.Object; }
        }
        
        // --------------- non nullable primitives
        public double Dbl {        
            get => dbl;            
            set { dbl = value;         Cat = VarType.Double; isNull = false; }
        }
        public float Flt {         
            get => (float)dbl;         
            set { dbl = value;         Cat = VarType.Float; isNull = false; }
        }          
        public long Lng {          
            get => lng;        
            set { lng = value;         Cat = VarType.Long;  isNull = false; }
        }          
        public int Int {           
            get => (int)lng;           
            set { lng = value;         Cat = VarType.Int;   isNull = false; }
        }          
        public short Short {           
            get => (short)lng;         
            set { lng = value;         Cat = VarType.Short; isNull = false; }
        }          
        public byte Byte {         
            get => (byte)lng;          
            set { lng = value;         Cat = VarType.Byte;  isNull = false; }
        }
        public bool Bool {
            get => lng != 0;
            set { lng = value ? 1 : 0; Cat = VarType.Bool;  isNull = false; }
        }
        
        // --------------- nullable primitives
        public double?  NullableDbl   {  get => isNull ? null : (double?)   dbl; }
        public float?   NullableFlt   {  get => isNull ? null : (float?)    dbl; }
        public long?    NullableLng   {  get => isNull ? null : (long?)     lng; }
        public int?     NullableInt   {  get => isNull ? null : (int?)      lng; }
        public short?   NullableShort {  get => isNull ? null : (short?)    lng; }
        public byte?    NullableByte  {  get => isNull ? null : (byte?)     lng; }
        public bool?    NullableBool  {  get => isNull ? null : (bool?)    (lng != 0); }
        
        
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

        public void  Clear() {
            Cat = VarType.None;
            obj = null;
            lng = 0;
            dbl = 0;
        }

        public override string ToString() {
            string val = null;
            switch (Cat) {
                case VarType.None:     val = "None";       break;
                case VarType.Object:   val = $"\"{obj}\""; break;
                //
                case VarType.Double:   val = dbl.ToString(CultureInfo.InvariantCulture); break;
                case VarType.Float:    val = dbl.ToString(CultureInfo.InvariantCulture); break;
                //
                case VarType.Long:     val = lng.ToString(CultureInfo.InvariantCulture); break;
                case VarType.Int:      val = lng.ToString(CultureInfo.InvariantCulture); break;
                case VarType.Short:    val = lng.ToString(CultureInfo.InvariantCulture); break;
                case VarType.Byte:     val = lng.ToString(CultureInfo.InvariantCulture); break;
                //
                case VarType.Bool:     val = lng != 0 ? "true" : "false";              break;
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