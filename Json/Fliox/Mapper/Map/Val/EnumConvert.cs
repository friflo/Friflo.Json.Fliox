// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    public static class  EnumConvert
    {
        internal static TTo UnsafeAs<TFrom, TTo> (TFrom from) {
#if UNITY_5_3_OR_NEWER
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<TFrom, TTo>(ref from);
#else
            return System.Runtime.CompilerServices.Unsafe.As<TFrom, TTo>(ref from);
#endif
        }
        
        public static EnumConvert<T> GetEnumConvert<T>() {
            var underlyingType = Nullable.GetUnderlyingType(typeof(T));
            if (underlyingType != null) {
                throw new InvalidOperationException($"Expect non nullable enum. was {typeof(T)}");
            }
            var underlyingEnumType  = Enum.GetUnderlyingType(typeof(T));
            // if (underlyingEnumType == typeof(long))  return 
            if (underlyingEnumType == typeof(int))      return new EnumConvertInt<T>();
            // if (underlyingEnumType == typeof(short)) return 
            if (underlyingEnumType == typeof(byte))     return new EnumConvertByte<T>();
            // if (underlyingEnumType == typeof(uint))  return 
            // if (underlyingEnumType == typeof(ushort))return 
            // if (underlyingEnumType == typeof(sbyte)) return 

            throw new InvalidOperationException($"enum not supported: {typeof(T)}");
        }
    }

    public abstract class EnumConvert<T> {
        public  abstract  T     IntToEnum(int value);
    }
    
    internal sealed class EnumConvertInt<T> : EnumConvert<T> {
        public  override  T     IntToEnum(int value)    { return EnumConvert.UnsafeAs<int, T>(value); }
    }

    internal sealed class EnumConvertByte<T> : EnumConvert<T> {
        public  override  T     IntToEnum(int value)    { return EnumConvert.UnsafeAs<byte, T>((byte)value); }
    }
    
}