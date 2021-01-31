// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect; // only used by CLR
using Friflo.Json.Mapper.Map.Val;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{

    // All field mapper classes are shared via multiple JsonReader / JsonWriter instances which run in various threads.
    // So they must not contain any mutable state.
    
    class DoubleFieldMapper : DoubleMapper {
        public DoubleFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            Write(writer, mirror.LoadDbl(primPos));
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreDbl(primPos, value);
            return success;
        }
    }
    
    class FloatFieldMapper : FloatMapper {
        public FloatFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            Write(writer, mirror.LoadFlt(primPos));
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreFlt(primPos, value);
            return success;
        }
    }

    class LongFieldMapper : LongMapper {
        public LongFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            Write(writer, mirror.LoadLong(primPos));
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreLong(primPos, value);
            return success;
        }
    }

    
    class IntFieldMapper : IntMapper {
        public IntFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            Write(writer, mirror.LoadInt(primPos));
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreInt(primPos, value);
            return success;
        }
    }
    
    class NullableIntFieldMapper : NullableIntMapper {
        public NullableIntFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            Write(writer, mirror.LoadIntNulL(primPos));
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreIntNulL(primPos, value);
            return success;
        }
    }
    
    class ShortFieldMapper : ShortMapper {
        public ShortFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            Write(writer, mirror.LoadShort(primPos));
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreShort(primPos, value);
            return success;
        }
    }
    
    class ByteFieldMapper : ByteMapper {
        public ByteFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            Write(writer, mirror.LoadByte(primPos));
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreByte(primPos, value);
            return success;
        }
    }
    
    class BoolFieldMapper : BoolMapper {
        public BoolFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            Write(writer, mirror.LoadBool(primPos));
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, false, out bool success);
            mirror.StoreBool(primPos, value);
            return success;
        }
    }
}

#endif

