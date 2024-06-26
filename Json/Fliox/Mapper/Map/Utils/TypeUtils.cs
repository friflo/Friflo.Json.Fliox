﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Fliox.Mapper.Map.Utils
{
    public static class TypeUtils
    {
        public static bool IsStandardType(Type type) {
            return type.IsPrimitive || type == typeof(string) || type.IsArray;
        }
        
        
        public static bool IsGenericType(Type type) {
            return type.IsConstructedGenericType;
        }
        
        public static bool IsNullable(Type type) {
            if (!type.IsValueType)
                return true;
            return GetNullableStruct (type) != null;
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
        
        public static long GetIntegralFromEnumValue<T>(T enumConstant, Type underlyingEnumType) {
            if (underlyingEnumType == typeof(long))    return (long)    (object)enumConstant;
            if (underlyingEnumType == typeof(int))     return (int)     (object)enumConstant;
            if (underlyingEnumType == typeof(short))   return (short)   (object)enumConstant;
            if (underlyingEnumType == typeof(byte))    return (byte)    (object)enumConstant;
            if (underlyingEnumType == typeof(uint))    return (uint)    (object)enumConstant;
            if (underlyingEnumType == typeof(ushort))  return (ushort)  (object)enumConstant;
            if (underlyingEnumType == typeof(sbyte))   return (sbyte)   (object)enumConstant;

            throw new InvalidOperationException("UnderlyingType of Enum not supported. Enum: " + typeof(T));
        }
    }
}