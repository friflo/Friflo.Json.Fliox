using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.MapIL.Obj;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public partial struct Reader : IErrorHandler, IDisposable {
        public              JsonParser          parser;
        public              Bytes               strBuf;
        public              Bytes32             searchKey;
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              char[]              charBuf;
        /// <summary>Can be used for custom mappers to lookup for a "string" in a Dictionary
        /// without creating a string on the heap.</summary>
        public readonly     BytesString         keyRef;
        public readonly     TypeCache           typeCache;
        private readonly    IErrorHandler       errorHandler;
#if !UNITY_5_3_OR_NEWER
        private             int                 classLevel;
        private  readonly   List<ClassMirror>   mirrorStack;
#endif

        public Reader(TypeStore typeStore, IErrorHandler errorHandler) {
            parser = new JsonParser();
            this.errorHandler = errorHandler;

            typeCache       = new TypeCache(typeStore);
            strBuf          = new Bytes(0);
            searchKey       = new Bytes32();
            charBuf         = new char[128];
            keyRef          = new BytesString();
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
            strBuf      .Dispose();
            typeCache   .Dispose();
            parser      .Dispose();
        }
        
        public TVal HandleEvent<TVal>(TypeMapper<TVal> mapper, out bool success) {
            switch (parser.Event) {
                case JsonEvent.ValueNull:
                    const string msg = "requirement: null value must be handled by owner. Add missing JsonEvent.ValueNull case to its Mapper";
                    throw new InvalidOperationException(msg);
                /*
                if (!stubType.isNullable)
                    return JsonReader.ErrorIncompatible(reader, "primitive", stubType, ref parser);
                value.SetNull(stubType.varType); // not necessary. null value us handled by owner.
                return true;
                */
                case JsonEvent.Error:
                    const string msg2 = "requirement: error must be handled by owner. Add missing JsonEvent.Error case to its Mapper";
                    throw new InvalidOperationException(msg2);
                // return null;
                default:
                    return ErrorIncompatible<TVal>(mapper.DataTypeName(), mapper, out success);
            }
        }
    }
}
