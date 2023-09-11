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

    public partial class MsgPackMapper
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