// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.MsgPack.Map;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public partial class MsgPackMapper
    {
        private const string        WriteMsg    = "WriteMsg";
        private const string        ReadMsg     = "ReadMsg";
        private const BindingFlags  Flags       = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;

        
        internal static MsgPackMapper<T> CreateMapper<T>() {
            var type        = typeof(T);
            if (type.IsClass) {
                if (type.IsArray) {
                    return CreateArrayMapper<T>();
                }
                if (type.IsGenericType) {
                    if (type.GetGenericTypeDefinition() == typeof(List<>)) {
                        return CreateListMapper<T>();
                    }
                    throw new NotSupportedException($"MessagePack. type: {type}");
                }
                return CreateClassMapper<T>();
            }
            if (type == typeof(int)) {
                return (MsgPackMapper<T>)CreatePrimitiveMapper<T>(type);
            }
            throw new NotSupportedException($"MessagePack. type: {type}");
        }
        
        // --- primitive types
        private static object CreatePrimitiveMapper<T>(Type type) {
            if (type == typeof(int)) {
                return new MsgPackMapper<int>(WriteMsg_Int32, ReadMsg_Int32);
            }
            return default;
        }
        
        private static void ReadMsg_Int32 (ref MsgReader reader, ref int value) { value = reader.ReadInt32(); }
        private static void WriteMsg_Int32(ref MsgWriter writer, ref int value) { writer.WriteInt32(value);   }
        
        
        // --- List<T>
        private static MsgPackMapper<T> CreateArrayMapper<T>() {
            var type        = typeof(T);
            var elementType = type.GetElementType();
            if (elementType == typeof(int)) {
                return (MsgPackMapper<T>)(object) new MsgPackMapper<int[]>(MsgPackArray.WriteMsg, MsgPackArray.ReadMsg);
            }
            return CreateGenericMapper<T>(GenericListWrite, GenericListRead, elementType);
        }
        
        private static readonly MethodInfo GenericListWrite = GetGenericMethod(typeof(MsgPackArray), WriteMsg, typeof(MsgWriter).MakeByRefType());
        private static readonly MethodInfo GenericListRead  = GetGenericMethod(typeof(MsgPackArray), ReadMsg,  typeof(MsgReader).MakeByRefType());

        
        // --- List<T>
        private static MsgPackMapper<T> CreateListMapper<T>() {
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (typeof(T), typeof(List<>) );
            if (args == null) {
                return default;
            }
            var elementType = args[0];
            if (elementType == typeof(int)) {
                return (MsgPackMapper<T>)(object) new MsgPackMapper<List<int>>(MsgPackList.WriteMsg, MsgPackList.ReadMsg);
            }
            return CreateGenericMapper<T>(GenericArrayWrite, GenericArrayRead, elementType);
        }
        
        private static readonly MethodInfo GenericArrayWrite = GetGenericMethod(typeof(MsgPackList), WriteMsg, typeof(MsgWriter).MakeByRefType());
        private static readonly MethodInfo GenericArrayRead  = GetGenericMethod(typeof(MsgPackList), ReadMsg,  typeof(MsgReader).MakeByRefType());
        
        private static MsgPackMapper<T> CreateGenericMapper<T>(MethodInfo genWrite, MethodInfo genRead, Type elementType) {
            var type        = typeof(T);
            
            var writeGen    = genWrite.MakeGenericMethod(elementType);
            var writeDel    = Delegate.CreateDelegate(typeof(MsgWrite<>).MakeGenericType(type), writeGen);
            var write       = (MsgWrite<T>)writeDel;
            
            var readGen     = genRead.MakeGenericMethod(elementType);
            var readDel     = Delegate.CreateDelegate(typeof(MsgRead<>).MakeGenericType(type), readGen);
            var read        = (MsgRead<T>)readDel;
            
            return new MsgPackMapper<T>(write, read);
        }
        
        private static MethodInfo GetGenericMethod(Type mapperType, string name, Type param0) {
            var methods     = mapperType.GetMethods(Flags);
            foreach (var method in methods) {
                if (method.Name == name && method.IsGenericMethod) {
                    var parameters = method.GetParameters();
                    if (parameters[0].ParameterType == param0) {
                        return method;
                    }
                }
            }
            return null;
        }
        
        // --- class
        private static MsgPackMapper<T> CreateClassMapper<T>() {
            var type        = typeof(T);
            var assembly    = type.Assembly;
            var genClassName= $"Gen.{type.Namespace}.Gen_{type.Name}";
            var genClass    = assembly.GetType(genClassName);
            if (genClass == null)       throw new InvalidOperationException($"type not found: {genClassName}");
            
            var writeMethod = genClass.GetMethod(WriteMsg);
            if (writeMethod == null)    throw new InvalidOperationException($"method not found: {genClassName}.{WriteMsg}");
            var write       = (MsgWrite<T>)Delegate.CreateDelegate(typeof(MsgWrite<>).MakeGenericType(typeof(T)), writeMethod);
            
            var readMethod  = genClass.GetMethod(ReadMsg);
            if (readMethod == null)     throw new InvalidOperationException($"method not found: {genClassName}.{ReadMsg}");
            var read        = (MsgRead<T>)Delegate.CreateDelegate(typeof(MsgRead<>).MakeGenericType(typeof(T)), readMethod);
            
            return new MsgPackMapper<T>(write, read);
        }
    }
}