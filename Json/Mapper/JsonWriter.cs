// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonWriter : IDisposable
    {
        private     Writer      intern;
        public  ref Bytes       Output => ref intern.bytes;

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

        private void InitJsonWriter() {
            intern.bytes.Clear();
            intern.level = 0;
            intern.InitMirrorStack();
        }
        
        // --------------- Bytes ---------------  todo
        public void Write<T>(T value) {
            WriteStart(value);
        }

        public void WriteObject(object value) {
            WriteStart(value);
        }

        // --------------------------------------- private --------------------------------------- 
        private void WriteStart(object value) {
            if (value == null) {
                WriteUtils.AppendNull(ref intern);
                return;
            }
            TypeMapper mapper = intern.typeCache.GetTypeMapper(value.GetType());
            InitJsonWriter();
            try {
                mapper.WriteObject(ref intern, value);
            }
            finally { intern.ClearMirrorStack(); }

            if (intern.level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {intern.level}");
        }
        
        private void WriteStart<T>(T value) {
            var mapper = (TypeMapper<T>)intern.typeCache.GetTypeMapper(typeof(T));
            InitJsonWriter();
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
