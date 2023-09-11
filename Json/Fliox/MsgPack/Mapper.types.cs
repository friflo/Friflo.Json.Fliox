// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.MsgPack.Map;

namespace Friflo.Json.Fliox.MsgPack
{
    public partial class MsgPackMapper
    {
        private const string        WriteMsg = "WriteMsg";
        private const string        ReadMsg  = "ReadMsg";
        private const BindingFlags  Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        
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

            var genType = typeof(MsgPackArray<>).MakeGenericType(elementType);
            if (genType == null)        throw new InvalidOperationException($"type not found: {genType}");
            var writeMethod = genType.GetMethod(WriteMsg, Flags);
            if (writeMethod == null)    throw new InvalidOperationException($"method not found: {genType}.{WriteMsg}");
            var write       = (MsgWrite<T>)Delegate.CreateDelegate(typeof(MsgWrite<>).MakeGenericType(type), writeMethod);
            
            var readMethod  = genType.GetMethod(ReadMsg, Flags);
            if (readMethod == null)     throw new InvalidOperationException($"method not found: {genType}.{ReadMsg}");
            var read        = (MsgRead<T>)Delegate.CreateDelegate(typeof(MsgRead<>).MakeGenericType(type), readMethod);
            
            return new MsgPackMapper<T>(write, read);
        }
        
        // --- List<T>
        private static MsgPackMapper<T> CreateListMapper<T>() {
            var type = typeof(T);
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(List<>) );
            if (args == null) {
                return default;
            }
            Type elementType = args[0];
            var genType = typeof(MsgPackList<>).MakeGenericType(elementType);
            if (genType == null)        throw new InvalidOperationException($"type not found: {genType}");
            var writeMethod = genType.GetMethod(WriteMsg, Flags);
            if (writeMethod == null)    throw new InvalidOperationException($"method not found: {genType}.{WriteMsg}");
            var write       = (MsgWrite<T>)Delegate.CreateDelegate(typeof(MsgWrite<>).MakeGenericType(type), writeMethod);
            
            var readMethod  = genType.GetMethod(ReadMsg, Flags);
            if (readMethod == null)     throw new InvalidOperationException($"method not found: {genType}.{ReadMsg}");
            var read        = (MsgRead<T>)Delegate.CreateDelegate(typeof(MsgRead<>).MakeGenericType(type), readMethod);
            
            return new MsgPackMapper<T>(write, read);
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