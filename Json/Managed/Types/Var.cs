// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Globalization;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Types
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
        
        public VarType     Cat { get;  private set; }

        public object Obj {
            get => obj;
            set { obj = value; Cat = VarType.Object; }
        }
        public double Dbl {
            get => dbl;
            set { dbl = value; Cat = VarType.Double; }
        }
        public float Flt {
            get => (float)dbl;
            set { dbl = value; Cat = VarType.Float; }
        }
        public long Lng {
            get => lng;
            set { lng = value; Cat = VarType.Long; }
        }
        public int Int {
            get => (int)lng;
            set { lng = value; Cat = VarType.Int; }
        }
        public short Short {
            get => (short)lng;
            set { lng = value; Cat = VarType.Short; }
        }
        public byte Byte {
            get => (byte)lng;
            set { lng = value; Cat = VarType.Byte; }
        }
        public bool Bool {
            get => lng != 0;
            set { lng = value ? 1 : 0; Cat = VarType.Bool; }
        }
        
        public object Get () {
            switch (Cat) {
                case VarType.None:     return null;
                case VarType.Object:   return obj;
                //
                case VarType.Double:   return dbl;
                case VarType.Float:    return (float)dbl;
                //
                case VarType.Long:     return lng;
                case VarType.Int:      return (int)lng;
                case VarType.Short:    return (short)lng;
                case VarType.Byte:     return (byte)lng;
                //
                case VarType.Bool:     return lng != 0;
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
        
        public static VarType GetSlotType (Type type)
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