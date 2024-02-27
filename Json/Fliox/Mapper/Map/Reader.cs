// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Pools;

namespace Friflo.Json.Fliox.Mapper.Map
{
    public sealed class Utf8JsonWriterStub : IDisposable
    {
        public Utf8JsonWriter jsonWriter;
        
        public void Dispose() {
            jsonWriter.Dispose();
        }
    }

    [CLSCompliant(true)]
    public partial struct Reader : IDisposable {
        public              Utf8JsonParser      parser;
        public              Bytes               strBuf;
        public              Bytes32             searchKey;
        internal            bool                setMissingFields;
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              char[]              charBuf;
        public   readonly   TypeCache           typeCache;
        public              Utf8JsonWriterStub  jsonWriterStub;
        public              ReaderPool          readerPool;
        
        public              IErrorHandler       ErrorHandler {
            get => parser.error.errorHandler;
            set => parser.error.errorHandler = value;
        }

        public Reader(TypeStore typeStore) {
            parser          = new Utf8JsonParser();
            typeCache       = new TypeCache(typeStore);
            strBuf          = new Bytes(0);
            searchKey       = new Bytes32();
            charBuf         = new char[128];
            jsonWriterStub  = null;
            setMissingFields= false;
            readerPool      = null;
            parser.error.errorHandler = DefaultErrorHandler;
        }
        
        public void Dispose() {
            jsonWriterStub?.Dispose();
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
        
        public static readonly DefaultErrorHandler DefaultErrorHandler = new DefaultErrorHandler();
    }
    
    public sealed class DefaultErrorHandler : IErrorHandler
    {
        public void HandleError(int pos, in Bytes message) {
            throw new JsonReaderException(message.AsString(), pos);
        }
    }
}
