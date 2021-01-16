// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Types
{
    public static class SimpleType
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
        /*
        public static bool IsNumber(Id type)
        {

            return (Id.Long <= type && type <= Id.Float);
        } */

        public static Id ? IdFromType (Type type)
        {
            if (type == typeof( String     ))  return Id.String;
            if (type == typeof( long       ))  return Id.Long;
            if (type == typeof( int        ))  return Id.Integer;
            if (type == typeof( short      ))  return Id.Short;
            if (type == typeof( byte       ))  return Id.Byte;
            if (type == typeof( bool       ))  return Id.Bool;
            if (type == typeof( double     ))  return Id.Double;
            if (type == typeof( float      ))  return Id.Float;
            if (Reflect.IsAssignableFrom (typeof(Object), type))   return Id.Object;
            return null;
        }
    
        public static Id IdFromField (FieldInfo field)
        {
            Type type = field. FieldType;
            Id ? id = IdFromType (type);
            if (id == null)
                throw new FrifloException("unsupported simple type: " + type + " of field " + field. Name);
            return id .Value;
        }

        public static Id IdFromMethod (PropertyInfo method)
        {
            Type type = method. PropertyType;
            Id ? id = IdFromType (type);
            if (id == null)
                throw new FrifloException("unsupported simple type: " + type + " of method " + method. Name);
            return id .Value;
        }
    }
}
