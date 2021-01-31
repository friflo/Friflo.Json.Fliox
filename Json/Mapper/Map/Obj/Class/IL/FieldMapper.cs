// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Map.Val;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    static class NullableMapper {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool WriteNull<T>(JsonWriter writer, T? value) where T : struct {
            if (value.HasValue)
                return false;
            WriteUtils.AppendNull(writer);
            return true;
        }
    }

    // All field mapper classes are shared via multiple JsonReader / JsonWriter instances which run in various threads.
    // So they must not contain any mutable state.
    
    // ---------------------------------------------------------------------------- double
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
    class NullableDoubleFieldMapper : NullableDoubleMapper {
        public NullableDoubleFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadDblNulL(primPos);
            if (!NullableMapper.WriteNull(writer, value))
                Write(writer, value);
        }
        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreDblNulL(primPos, value);
            return success;
        }
    }
    
    // ---------------------------------------------------------------------------- float
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
    class NullableFloatFieldMapper : NullableFloatMapper {
        public NullableFloatFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadFltNulL(primPos);
            if (!NullableMapper.WriteNull(writer, value))
                Write(writer, value);
        }
        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreFltNulL(primPos, value);
            return success;
        }
    }

    // ---------------------------------------------------------------------------- long
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
    class NullableLongFieldMapper : NullableLongMapper {
        public NullableLongFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadLongNulL(primPos);
            if (!NullableMapper.WriteNull(writer, value))
                Write(writer, value);
        }
        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreLongNulL(primPos, value);
            return success;
        }
    }

    // ---------------------------------------------------------------------------- int
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
            var value = mirror.LoadIntNulL(primPos);
            if (!NullableMapper.WriteNull(writer, value))
                Write(writer, value);
        }
        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreIntNulL(primPos, value);
            return success;
        }
    }
    
    // ---------------------------------------------------------------------------- short
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
    class NullableShortFieldMapper : NullableShortMapper {
        public NullableShortFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadShortNulL(primPos);
            if (!NullableMapper.WriteNull(writer, value))
                Write(writer, value);
        }
        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreShortNulL(primPos, value);
            return success;
        }
    }

    
    // ---------------------------------------------------------------------------- byte
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
    class NullableByteFieldMapper : NullableByteMapper {
        public NullableByteFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadByteNulL(primPos);
            if (!NullableMapper.WriteNull(writer, value))
                Write(writer, value);
        }
        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader, 0, out bool success);
            mirror.StoreByteNulL(primPos, value);
            return success;
        }
    }
    
    // ---------------------------------------------------------------------------- bool
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
    class NullableBoolFieldMapper : NullableBoolMapper {
        public NullableBoolFieldMapper(Type type) : base(type) { }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadBoolNulL(primPos);
            if (!NullableMapper.WriteNull(writer, value))
                Write(writer, value);
        }
        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(reader,false, out bool success);
            mirror.StoreBoolNulL(primPos, value);
            return success;
        }
    }
}

#endif

