// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Flow.Mapper.MapIL.Obj;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Mapper.Map
{
    public class JsonSerializerStub : IDisposable
    {
        public JsonSerializer jsonSerializer;
        
        public void Dispose() {
            jsonSerializer.Dispose();
        }
    }

#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial struct Reader : IErrorHandler, IDisposable {
        public              JsonParser          parser;
        public              Bytes               strBuf;
        public              Bytes32             searchKey;
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              char[]              charBuf;
        public              object[]            setMethodParams;
        /// <summary>Can be used for custom mappers to lookup for a "string" in a Dictionary
        /// without creating a string on the heap.</summary>
        public readonly     BytesString         keyRef;
        public readonly     TypeCache           typeCache;
        private readonly    IErrorHandler       errorHandler;
        public              ITracerContext      tracerContext;
        public              JsonSerializerStub  jsonSerializerStub;
#if !UNITY_5_3_OR_NEWER
        private             int                 classLevel;
        private  readonly   List<ClassMirror>   mirrorStack;
#endif

        public Reader(TypeStore typeStore, IErrorHandler errorHandler) {
            parser = new JsonParser();
            this.errorHandler = errorHandler;
            tracerContext   = null;

            typeCache       = new TypeCache(typeStore);
            strBuf          = new Bytes(0);
            searchKey       = new Bytes32();
            charBuf         = new char[128];
            setMethodParams = new object[1];
            keyRef          = new BytesString();
            jsonSerializerStub = null;
#if !UNITY_5_3_OR_NEWER
            mirrorStack     = new List<ClassMirror>(16);
            classLevel      = 0;
#endif
#if !JSON_BURST
            parser.error.errorHandler = this;
#endif
        }
        
        public void HandleError(int pos, ref Bytes message) {
            if (errorHandler != null)
                errorHandler.HandleError(pos, ref message);
            else
                throw new JsonReaderException(message.ToString(), pos);
        }

        public void Dispose() {
            jsonSerializerStub?.Dispose();
            strBuf      .Dispose();
            typeCache   .Dispose();
            parser      .Dispose();
        }
        
        public TVal HandleEvent<TVal>(TypeMapper<TVal> mapper, out bool success) {
            switch (parser.Event) {
                case JsonEvent.ValueNull:
                    if (!mapper.isNullable)
                        return ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out success);
                    success = true;
                    return default;
                
                case JsonEvent.Error:
                    const string msg2 = "requirement: error must be handled by owner. Add missing JsonEvent.Error case to its Mapper";
                    throw new InvalidOperationException(msg2);
                // return null;
                default:
                    return ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out success);
            }
        }
        
        public static bool TryParseGuidBytes(ref Bytes bytes, char[] charBuf, out Guid guid) {
#if UNITY_5_3_OR_NEWER
            guid = new Guid(bytes.ToString());
            return true;
#else
            var array   = bytes.buffer.array;
            int len     = bytes.end - bytes.start;
            int offset  = bytes.start;
            for (int n = 0; n < len; n++) {
                charBuf[n] = (char)array[offset + n];
            }
            var span = new Span<char>(charBuf, 0, len);
            if (Guid.TryParse(span, out guid)) {
                return true;
            }
            return false;
#endif
        }
    }
}
