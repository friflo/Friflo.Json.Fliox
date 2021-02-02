// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Mapper.Map.Utils
{
    public static class TypeUtils
    {
        public static bool IsStandardType(Type type) {
            return type.IsPrimitive || type == typeof(string) || type.IsArray;
        }
        
        public static bool IsGenericType(Type type) {
            while (type != null) {
                if (type.IsConstructedGenericType)
                    return true;
                type = type.BaseType;
            }
            return false;
        }
        
        public static bool IsPrimitiveNullable(Type type) {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static Type GetNullableStruct(Type type) {
            if (!type.IsValueType)
                return null;
            Type ut = Nullable.GetUnderlyingType(type);
            if (ut != null && !ut.IsPrimitive)
                return ut;
            return null;
        }
        
        public static long GetIntegralValue(object enumConstant, Type type) {
            if (enumConstant is long    longVal)    return longVal;
            if (enumConstant is int     intVal)     return intVal;
            if (enumConstant is short   shortVal)   return shortVal;
            if (enumConstant is byte    byteVal)    return byteVal;
            if (enumConstant is uint    uintVal)    return uintVal;
            if (enumConstant is ushort  ushortVal)  return ushortVal;
            if (enumConstant is sbyte   sbyteVal)   return sbyteVal;

            throw new InvalidOperationException("UnderlyingType of Enum not supported. Enum: " + type);
        }
        
        public static T GetEnumValueFromIntegral<T>(long value, Type underlyingEnumType) {
            if (underlyingEnumType == typeof(long))     { return (T) (object) (long)    value; }
            if (underlyingEnumType == typeof(int))      { return (T) (object) (int)     value; }
            if (underlyingEnumType == typeof(short))    { return (T) (object) (short)   value; }
            if (underlyingEnumType == typeof(byte))     { return (T) (object) (byte)    value; }
            if (underlyingEnumType == typeof(uint))     { return (T) (object) (uint)    value; }
            if (underlyingEnumType == typeof(ushort))   { return (T) (object) (ushort)  value; }
            if (underlyingEnumType == typeof(sbyte))    { return (T) (object) (sbyte)   value; }

            throw new InvalidOperationException("UnderlyingType of Enum not supported. Enum: " + underlyingEnumType);
        }
    }
}