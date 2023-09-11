// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.MsgPack.Map;

namespace Friflo.Json.Fliox.MsgPack
{

    public      delegate void MsgWrite<T>(ref T item, ref MsgWriter writer);    // TODO make internal
    internal    delegate void MsgRead<T> (ref T item, ref MsgReader reader);

    public class ReadException : Exception
    {
        public ReadException(string message) : base(message) { }
    }

    public class MsgPackMapper
    {
        private         byte[]      data        = new byte[4];
        private         bool        writeNil    = true;
        private         MsgWriter   writer;        
        public          string      DataHex     => writer.DataHex;
        
        [ThreadStatic]
        private static  byte[]   _dataTls;
        
        public MsgPackMapper() {
            writer = new MsgWriter(data, writeNil);
        }
        
        public ReadOnlySpan<byte> Write<T>(T value)
        {
            MsgPackMapper<T>.Instance.write(ref value, ref writer);
            data = writer.target;
            return writer.Data;
        }
        
        public static ReadOnlySpan<byte> Serialize<T>(T value, bool writeNil = true)
        {
            _dataTls  ??= new byte[4];
            var writer  = new MsgWriter(_dataTls, writeNil);
            MsgPackMapper<T>.Instance.write(ref value, ref writer);
            _dataTls    = writer.target;
            return writer.Data;
        }
        
        // --- Deserialize()
        public static T Deserialize<T>(ReadOnlySpan<byte> data, out string error)
        {
            var reader  = new MsgReader(data);
            T result = default;
            MsgPackMapper<T>.Instance.read(ref result, ref reader);
            error = reader.Error;
            return result;
        }
        
        public static T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            var reader  = new MsgReader(data);
            T result = default;
            MsgPackMapper<T>.Instance.read(ref result, ref reader);
            if (reader.Error != null) {
                throw new ReadException(reader.Error);
            }
            return result;
        }
        
        // --- DeserializeTo()
        public static void DeserializeTo<T>(ReadOnlySpan<byte> data, ref T result, out string error)
        {
            var reader = new MsgReader(data);
            MsgPackMapper<T>.Instance.read(ref result, ref reader);
            error = reader.Error;
        }
        
        public static void DeserializeTo<T>(ReadOnlySpan<byte> data, ref T result)
        {
            var reader = new MsgReader(data);
            MsgPackMapper<T>.Instance.read(ref result, ref reader);
            if (reader.Error != null) {
                throw new ReadException(reader.Error);
            }
        }
        
        private const string WriteMsg = "WriteMsg";
        private const string ReadMsg  = "ReadMsg";
        
        internal static MsgPackMapper<T> CreateMapper<T>() {
            var type        = typeof(T);
            if (type.IsClass) {
                if (type.IsGenericType) {
                    if (type.GetGenericTypeDefinition() == typeof(List<>)) {
                        return CreateListMapper<T>();
                    }
                    throw new NotSupportedException($"MessagePack. type: {type}");
                }
                return CreateClassMapper<T>();
            }
            if (type == typeof(int)) {
                return (MsgPackMapper<T>)(object)new MsgPackMapper<int>(WriteMsg_Int32, ReadMsg_Int32);
            }
            throw new NotSupportedException($"MessagePack. type: {type}");
        }
        
        private static void ReadMsg_Int32 (ref int value, ref MsgReader reader) {
            value = reader.ReadInt32();
        }
        
        private static void WriteMsg_Int32(ref int value, ref MsgWriter writer) {
            writer.WriteInt32(value);
        }
        
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        
        private static MsgPackMapper<T> CreateListMapper<T>() {
            var listType = typeof(T);
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (listType, typeof(List<>) );
            if (args == null) {
                return default;
            }
            Type elementType = args[0];
            var genType = typeof(MsgPackList<>).MakeGenericType(elementType);
            if (genType == null)        throw new InvalidOperationException($"type not found: {genType}");
            var writeMethod = genType.GetMethod(WriteMsg, Flags);
            if (writeMethod == null)    throw new InvalidOperationException($"method not found: {genType}.{WriteMsg}");
            var write       = (MsgWrite<T>)Delegate.CreateDelegate(typeof(MsgWrite<>).MakeGenericType(listType), writeMethod);
            
            var readMethod  = genType.GetMethod(ReadMsg, Flags);
            if (readMethod == null)     throw new InvalidOperationException($"method not found: {genType}.{ReadMsg}");
            var read        = (MsgRead<T>)Delegate.CreateDelegate(typeof(MsgRead<>).MakeGenericType(listType), readMethod);
            
            return new MsgPackMapper<T>(write, read);
        }
        
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

    internal readonly struct MsgPackMapper<T>
    {
        internal static readonly MsgPackMapper<T> Instance = MsgPackMapper.CreateMapper<T>();
        
        internal readonly MsgWrite<T>    write;
        internal readonly MsgRead<T>     read;
        
        internal MsgPackMapper(MsgWrite<T>  write, MsgRead<T> read) {
            this.write  = write;
            this.read   = read;
        }
    }
}