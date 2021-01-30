// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

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

        public static bool IsNull<T>(T value) {
            Type type = typeof(T);
            if (type.IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                return false;
            return EqualityComparer<T>.Default.Equals(value, default);
        }
    }
}