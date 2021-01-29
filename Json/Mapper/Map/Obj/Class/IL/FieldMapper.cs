// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect; // only used by CLR
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
#if UNITY_5_3_OR_NEWER

    // dummy implementations for Unity
    class DoubleFieldMapper : DoubleMapper  { public DoubleFieldMapper  (Type type) : base(type) { } }
    class FloatFieldMapper  : FloatMapper   { public FloatFieldMapper   (Type type) : base(type) { } }
    class LongFieldMapper   : LongMapper    { public LongFieldMapper    (Type type) : base(type) { } }
    class IntFieldMapper    : IntMapper     { public IntFieldMapper     (Type type) : base(type) { } }
    class ShortFieldMapper  : ShortMapper   { public ShortFieldMapper   (Type type) : base(type) { } }
    class ByteFieldMapper   : ByteMapper    { public ByteFieldMapper    (Type type) : base(type) { } }
    class BoolFieldMapper   : BoolMapper    { public BoolFieldMapper    (Type type) : base(type) { } }
    
#else

    // All field mapper classes are shared via multiple JsonReader / JsonWriter instances which run in various threads.
    // So they must not contain any mutable state.
    
    class DoubleFieldMapper : DoubleMapper {
        public DoubleFieldMapper(Type type) : base(type) { }
        
        public override void WriteField(JsonWriter writer, ClassPayload payload, PropField field, int primPos, int objPos) {
            Write(writer, payload.LoadDbl(primPos + field.primIndex));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreDbl(field.primIndex, value);
            return success;
        }
    }
    
    class FloatFieldMapper : FloatMapper {
        public FloatFieldMapper(Type type) : base(type) { }
        
        public override void WriteField(JsonWriter writer, ClassPayload payload, PropField field, int primPos, int objPos) {
            Write(writer, payload.LoadFlt(primPos + field.primIndex));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreFlt(field.primIndex, value);
            return success;
        }
    }

    class LongFieldMapper : LongMapper {
        public LongFieldMapper(Type type) : base(type) { }
        
        public override void WriteField(JsonWriter writer, ClassPayload payload, PropField field, int primPos, int objPos) {
            Write(writer, payload.LoadLong(primPos + field.primIndex));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreLong(field.primIndex, value);
            return success;
        }
    }

    
    class IntFieldMapper : IntMapper {
        public IntFieldMapper(Type type) : base(type) { }
        
        public override void WriteField(JsonWriter writer, ClassPayload payload, PropField field, int primPos, int objPos) {
            Write(writer, payload.LoadInt(primPos + field.primIndex));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreInt(field.primIndex, value);
            return success;
        }
    }
    
    class ShortFieldMapper : ShortMapper {
        public ShortFieldMapper(Type type) : base(type) { }
        
        public override void WriteField(JsonWriter writer, ClassPayload payload, PropField field, int primPos, int objPos) {
            Write(writer, payload.LoadShort(primPos + field.primIndex));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreShort(field.primIndex, value);
            return success;
        }
    }
    
    class ByteFieldMapper : ByteMapper {
        public ByteFieldMapper(Type type) : base(type) { }
        
        public override void WriteField(JsonWriter writer, ClassPayload payload, PropField field, int primPos, int objPos) {
            Write(writer, payload.LoadByte(primPos + field.primIndex));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, 0, out bool success);
            payload.StoreByte(field.primIndex, value);
            return success;
        }
    }
    
    class BoolFieldMapper : BoolMapper {
        public BoolFieldMapper(Type type) : base(type) { }
        
        public override void WriteField(JsonWriter writer, ClassPayload payload, PropField field, int primPos, int objPos) {
            Write(writer, payload.LoadBool(primPos + field.primIndex));
        }

        public override bool ReadField  (JsonReader reader, ClassPayload payload, PropField field) {
            var value = Read(reader, false, out bool success);
            payload.StoreBool(field.primIndex, value);
            return success;
        }
    }
    
    
#endif
    
}
