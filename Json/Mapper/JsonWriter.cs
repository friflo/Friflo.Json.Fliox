// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public interface IJsonWriter
    {
        // --- Bytes
        void    Write<T>    (T      value, ref Bytes bytes);
        void    WriteObject (object value, ref Bytes bytes);
        
        // --- Stream
        void    Write<T>    (T      value, Stream stream);
        void    WriteObject (object value, Stream stream);
        
        // --- string
        string  Write<T>    (T      value);
        string  WriteObject (object value);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonWriter : IJsonWriter, IDisposable
    {
        private     Writer      intern;

        public      int         Level => intern.level;
        public      int         MaxDepth {
            get => intern.maxDepth;
            set => intern.maxDepth = value;
        }

        public JsonWriter(TypeStore typeStore) {
            intern = new Writer(typeStore);
        }
        
        public void Dispose() {
            intern.Dispose();
        }

        private void InitJsonWriterBytes() {
            intern.outputType  = OutputType.ByteList;
            //
            intern.bytes.Clear();
            intern.level = 0;
            intern.InitMirrorStack();
        }

        private void InitJsonWriterStream(Stream stream) {
            intern.outputType = OutputType.ByteWriter;
            IBytesWriter bytesWriter = new StreamBytesWriter(stream);
#if JSON_BURST
            intern.writerHandle = NonBurstWriter.AddWriter(bytesWriter);      
#else
            intern.bytesWriter = bytesWriter;
#endif
            //
            intern.bytes.Clear();
            intern.level = 0;
            intern.InitMirrorStack();
        }
        
        private void InitJsonWriterString() {
            intern.outputType = OutputType.ByteList;
            //
            intern.bytes.Clear();
            intern.level = 0;
            intern.InitMirrorStack();
        }
        
        // --------------- Bytes ---------------
        public void Write<T>(T value, ref Bytes bytes) {
            InitJsonWriterBytes();
            WriteStart(value);
            bytes.Clear();
            bytes.AppendBytes(ref intern.bytes);
        }

        public void WriteObject(object value, ref Bytes bytes) {
            InitJsonWriterBytes();
            WriteStart(value);
            bytes.Clear();
            bytes.AppendBytes(ref intern.bytes);
        }
        
        // --------------- Stream ---------------

        public void Write<T>(T value, Stream stream) {
            InitJsonWriterStream(stream);
            WriteStart(value);
            WriteUtils.Flush(ref intern);
        }

        public void WriteObject(object value, Stream stream) {
            InitJsonWriterStream(stream);
            WriteStart(value);
            WriteUtils.Flush(ref intern);
        }
        
        // --------------- string ---------------
        public string Write<T>(T value) {
            InitJsonWriterString();
            WriteStart(value);
            return intern.bytes.ToString();
        }

        public string WriteObject(object value) {
            InitJsonWriterString();
            WriteStart(value);
            return intern.bytes.ToString();
        }

        // --------------------------------------- private --------------------------------------- 
        private void WriteStart(object value) {
            if (value == null) {
                WriteUtils.AppendNull(ref intern);
                return;
            }
            TypeMapper mapper = intern.typeCache.GetTypeMapper(value.GetType());
            try {
                mapper.WriteObject(ref intern, value);
            }
            finally { intern.ClearMirrorStack(); }

            if (intern.level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {intern.level}");
        }
        
        private void WriteStart<T>(T value) {
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            try {
                if (mapper.IsNull(ref value))
                    WriteUtils.AppendNull(ref intern);
                else
                    mapper.Write(ref intern, value);
            }
            finally { intern.ClearMirrorStack(); }
            

            if (intern.level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {intern.level}");
        }
    }
}
