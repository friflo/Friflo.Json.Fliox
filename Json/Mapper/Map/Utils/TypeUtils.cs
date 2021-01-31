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
    }
}