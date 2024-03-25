// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
            if (underlyingEnumType == typeof(long))     return new EnumConvertLong  <T>();
            if (underlyingEnumType == typeof(int))      return new EnumConvertInt   <T>();
            if (underlyingEnumType == typeof(short))    return new EnumConvertShort <T>();
            if (underlyingEnumType == typeof(byte))     return new EnumConvertByte  <T>();
            
        //  if (underlyingEnumType == typeof(ulong))    return new EnumConvertUlong <T>();
            if (underlyingEnumType == typeof(uint))     return new EnumConvertUInt  <T>();
            if (underlyingEnumType == typeof(ushort))   return new EnumConvertUShort<T>();
            if (underlyingEnumType == typeof(sbyte))    return new EnumConvertSByte  <T>();

            throw new InvalidOperationException($"enum not supported: {typeof(T)}");
        }
    }

    public abstract class EnumConvert<T> {
        public  abstract  T     LongToEnum(long value);
    }
    
    // ----------------------------------- implementations ----------------------------------- 
    // --- CLS compliant
    internal sealed class EnumConvertLong<T> : EnumConvert<T> {
        public  override  T LongToEnum(long value)    { return EnumConvert.UnsafeAs<long, T>(value); }
    }
    
    internal sealed class EnumConvertInt<T> : EnumConvert<T> {
        public  override  T LongToEnum(long value)    { return EnumConvert.UnsafeAs<int, T>((int)value); }
    }
    
    internal sealed class EnumConvertShort<T> : EnumConvert<T> {
        public  override  T LongToEnum(long value)    { return EnumConvert.UnsafeAs<short, T>((short)value); }
    }

    internal sealed class EnumConvertByte<T> : EnumConvert<T> {
        public  override  T LongToEnum(long value)    { return EnumConvert.UnsafeAs<byte, T>((byte)value); }
    }
    
    // --- non CLS compliant
    /* internal sealed class EnumConvertUlong<T> : EnumConvert<T> {
        public  override  T LongToEnum(long value)    { return EnumConvert.UnsafeAs<ulong, T>((ulong)value); }
    } */
    
    internal sealed class EnumConvertUInt<T> : EnumConvert<T> {
        public  override  T LongToEnum(long value)    { return EnumConvert.UnsafeAs<uint, T>((uint)value); }
    }
    
    internal sealed class EnumConvertUShort<T> : EnumConvert<T> {
        public  override  T LongToEnum(long value)    { return EnumConvert.UnsafeAs<ushort, T>((ushort)value); }
    }

    internal sealed class EnumConvertSByte<T> : EnumConvert<T> {
        public  override  T LongToEnum(long value)    { return EnumConvert.UnsafeAs<sbyte, T>((sbyte)value); }
    }
    
}