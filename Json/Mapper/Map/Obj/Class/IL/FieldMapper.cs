// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
#if UNITY_5_3_OR_NEWER

    // dummy implementations for Unity
    class IntFieldMapper : IntMapper { public IntFieldMapper(Type type) : base(type) { } }
    
#else

    // All field mapper classes are shared via multiple JsonReader / JsonWriter instances which run in various threads.
    // So they must not contain any mutable state.

    class LongFieldMapper : LongMapper {
        public LongFieldMapper(Type type) : base(type) { }
        
        public override void WriteField (JsonWriter writer, ClassPayload payload, PropField field) {
            Write(writer, payload.LoadLong(field.payloadPos));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreLong(field.payloadPos, value);
            return success;
        }
    }

    
    class IntFieldMapper : IntMapper {
        public IntFieldMapper(Type type) : base(type) { }
        
        public override void WriteField (JsonWriter writer, ClassPayload payload, PropField field) {
            Write(writer, payload.LoadInt(field.payloadPos));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreInt(field.payloadPos, value);
            return success;
        }
    }
    
    class ShortFieldMapper : ShortMapper {
        public ShortFieldMapper(Type type) : base(type) { }
        
        public override void WriteField (JsonWriter writer, ClassPayload payload, PropField field) {
            Write(writer, payload.LoadShort(field.payloadPos));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreShort(field.payloadPos, value);
            return success;
        }
    }
    
    class ByteFieldMapper : ByteMapper {
        public ByteFieldMapper(Type type) : base(type) { }
        
        public override void WriteField (JsonWriter writer, ClassPayload payload, PropField field) {
            Write(writer, payload.LoadByte(field.payloadPos));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreByte(field.payloadPos, value);
            return success;
        }
    }
    
    
#endif
    
}
