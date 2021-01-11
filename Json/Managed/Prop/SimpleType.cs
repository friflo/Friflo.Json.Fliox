// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
    public class SimpleType
    {
        public enum Id
        {
            String,
            Long,   // first number
            Integer,
            Short,
            Byte,
            Bool,
            Double,
            Float,  // last number
            Object,
            Method,
        }

        public static bool IsNumber(Id type)
        {

            return (Id.Long <= type && type <= Id.Float);
        }

        public static Id ? IdFromType (Type type)
        {
            if      (type == typeof( String     ))  return Id.String;
            else if (type == typeof( long       ))  return Id.Long;
            else if (type == typeof( int        ))  return Id.Integer;
            else if (type == typeof( short      ))  return Id.Short;
            else if (type == typeof( byte       ))  return Id.Byte;
            else if (type == typeof( bool       ))  return Id.Bool;
            else if (type == typeof( double     ))  return Id.Double;
            else if (type == typeof( float      ))  return Id.Float;
            else if (Reflect.IsAssignableFrom (typeof(Object), type))   return Id.Object;
            return null;
        }
    
        public static Id IdFromField (FieldInfo field)
        {
            Type type = field. FieldType;
            Id ? id = IdFromType (type);
            if (id == null)
                throw new FrifloException("unsupported simple type: " + type. FullName + " of field " + field. Name);
            return id .Value;
        }

        public static Id IdFromMethod (PropertyInfo method)
        {
            Type type = method. PropertyType;
            Id ? id = IdFromType (type);
            if (id == null)
                throw new FrifloException("unsupported simple type: " + type. FullName + " of method " + method. Name);
            return id .Value;
        }
        
        public static object ObjectFromDouble(Id? id, double value, out bool success) {
            if (id == null) {
                success = false;
                return null;
            }
            switch (id)
            {
                case Id. String:
                    success = false;
                    return null;
                case Id. Long:
                    success = true;
                    return (long) value;
                case Id. Integer:
                    success = true;
                    return (int) value;
                case Id. Short:
                    success = true;
                    return (short) value;
                case Id. Byte:
                    success = true;
                    return (byte) value;
                case Id. Bool:
                    success = false;
                    return null;
                case Id. Double:
                    success = true;
                    return value;
                case Id. Float:
                    success = true;
                    return (float) value;
                case Id. Object:
                    success = false;
                    return null;
                default:
                    success = false;
                    return null;
            }
        }
        /*
        public static object ObjectFromLong(Id? id, long value, out bool success) {
            if (id == null) {
                success = false;
                return null;
            }
            switch (id)
            {
                case Id. String:
                    success = false;
                    return null;
                case Id. Long:
                    success = true;
                    return value;
                case Id. Integer:
                    success = true;
                    return (int) value;
                case Id. Short:
                    success = true;
                    return (short) value;
                case Id. Byte:
                    success = true;
                    return (byte) value;
                case Id. Bool:
                    success = false;
                    return null;
                case Id. Double:
                    success = true;
                    return (double)value;
                case Id. Float:
                    success = true;
                    return (float) value;
                case Id. Object:
                    success = false;
                    return null;
                default:
                    success = false;
                    return null;
            }
        }  */
        
/*      public static bool IsAssignable (SimpleType.ID typeID)
        {
            switch (typeID)
            {
            case SimpleType.ID. String:
            case SimpleType.ID. Long:
            case SimpleType.ID. Integer:
            case SimpleType.ID. Short:
            case SimpleType.ID. Byte:
            case SimpleType.ID. Bool:
            case SimpleType.ID. Double:
            case SimpleType.ID. Float:
            case SimpleType.ID. Object:
                return true;
            default:
                return false;
            }   
        }
        */

    }
}
