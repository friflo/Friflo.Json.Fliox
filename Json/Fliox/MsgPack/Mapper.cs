// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

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
        private         byte[]  data        = new byte[4];
        private         bool    writeNil    = true;
        
        [ThreadStatic]
        private static  byte[]   _dataTls;
        
        public ReadOnlySpan<byte> Write<T>(T value) {
            var writer = new MsgWriter(data, writeNil);
            MsgPackMapper<T>.Instance.write(ref value, ref writer);
            data = writer.target;
            return writer.Data;
        }
        
        public static ReadOnlySpan<byte> Serialize<T>(T value, bool writeNil = true) {
            _dataTls  ??= new byte[4];
            var writer  = new MsgWriter(_dataTls, writeNil);
            MsgPackMapper<T>.Instance.write(ref value, ref writer);
            _dataTls    = writer.target;
            return writer.Data;
        }
        
        // --- Deserialize()
        public static T Deserialize<T>(ReadOnlySpan<byte> data, out string error) {
            var reader  = new MsgReader(data);
            T result = default;
            MsgPackMapper<T>.Instance.read(ref result, ref reader);
            error = reader.Error;
            return result;
        }
        
        public static T Deserialize<T>(ReadOnlySpan<byte> data) {
            var reader  = new MsgReader(data);
            T result = default;
            MsgPackMapper<T>.Instance.read(ref result, ref reader);
            if (reader.Error != null) {
                throw new ReadException(reader.Error);
            }
            return result;
        }
        
        // --- DeserializeTo()
        public static void DeserializeTo<T>(ReadOnlySpan<byte> data, ref T result, out string error) {
            var reader = new MsgReader(data);
            MsgPackMapper<T>.Instance.read(ref result, ref reader);
            error = reader.Error;
        }
        
        public static void DeserializeTo<T>(ReadOnlySpan<byte> data, ref T result) {
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

    internal class MsgPackMapper<T> : MsgPackMapper
    {
        internal static readonly MsgPackMapper<T> Instance = CreateMapper<T>();
        
        internal readonly MsgWrite<T>    write;
        internal readonly MsgRead<T>     read;
        
        internal MsgPackMapper(MsgWrite<T>  write, MsgRead<T> read) {
            this.write = write;
            this.read = read;
        }
    }
}