// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Map.Val;
using Friflo.Json.Mapper.MapIL.Obj;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.MapIL.Val
{
    static class NullableMapper {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool WriteNull<T>(ref Writer writer, T? value) where T : struct {
            if (value.HasValue)
                return false;
            WriteUtils.AppendNull(ref writer);
            return true;
        }
    }

    // All field mapper classes are shared via multiple JsonReader / JsonWriter instances which run in various threads.
    // So they must not contain any mutable state.
    
    // ---------------------------------------------------------------------------- double
    class DoubleFieldMapper : DoubleMapper {
        public DoubleFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            Write(ref writer, mirror.LoadDbl(primPos));
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreDbl(primPos, value);
            return success;
        }
    }
    class NullableDoubleFieldMapper : NullableDoubleMapper {
        public NullableDoubleFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadDblNull(primPos);
            if (!NullableMapper.WriteNull(ref writer, value))
                Write(ref writer, value);
        }
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreDblNull(primPos, value);
            return success;
        }
    }
    
    // ---------------------------------------------------------------------------- float
    class FloatFieldMapper : FloatMapper {
        public FloatFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            Write(ref writer, mirror.LoadFlt(primPos));
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreFlt(primPos, value);
            return success;
        }
    }
    class NullableFloatFieldMapper : NullableFloatMapper {
        public NullableFloatFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadFltNull(primPos);
            if (!NullableMapper.WriteNull(ref writer, value))
                Write(ref writer, value);
        }
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreFltNull(primPos, value);
            return success;
        }
    }

    // ---------------------------------------------------------------------------- long
    class LongFieldMapper : LongMapper {
        public LongFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            Write(ref writer, mirror.LoadLong(primPos));
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreLong(primPos, value);
            return success;
        }
    }
    class NullableLongFieldMapper : NullableLongMapper {
        public NullableLongFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadLongNull(primPos);
            if (!NullableMapper.WriteNull(ref writer, value))
                Write(ref writer, value);
        }
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreLongNull(primPos, value);
            return success;
        }
    }

    // ---------------------------------------------------------------------------- int
    class IntFieldMapper : IntMapper {
        public IntFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            Write(ref writer, mirror.LoadInt(primPos));
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreInt(primPos, value);
            return success;
        }
    }
    
    class NullableIntFieldMapper : NullableIntMapper {
        public NullableIntFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadIntNull(primPos);
            if (!NullableMapper.WriteNull(ref writer, value))
                Write(ref writer, value);
        }
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreIntNull(primPos, value);
            return success;
        }
    }
    
    // ---------------------------------------------------------------------------- short
    class ShortFieldMapper : ShortMapper {
        public ShortFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            Write(ref writer, mirror.LoadShort(primPos));
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreShort(primPos, value);
            return success;
        }
    }
    class NullableShortFieldMapper : NullableShortMapper {
        public NullableShortFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadShortNull(primPos);
            if (!NullableMapper.WriteNull(ref writer, value))
                Write(ref writer, value);
        }
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreShortNull(primPos, value);
            return success;
        }
    }

    
    // ---------------------------------------------------------------------------- byte
    class ByteFieldMapper : ByteMapper {
        public ByteFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            Write(ref writer, mirror.LoadByte(primPos));
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreByte(primPos, value);
            return success;
        }
    }
    class NullableByteFieldMapper : NullableByteMapper {
        public NullableByteFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadByteNull(primPos);
            if (!NullableMapper.WriteNull(ref writer, value))
                Write(ref writer, value);
        }
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, 0, out bool success);
            mirror.StoreByteNull(primPos, value);
            return success;
        }
    }
    
    // ---------------------------------------------------------------------------- bool
    class BoolFieldMapper : BoolMapper {
        public BoolFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            Write(ref writer, mirror.LoadBool(primPos));
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader, false, out bool success);
            mirror.StoreBool(primPos, value);
            return success;
        }
    }
    class NullableBoolFieldMapper : NullableBoolMapper {
        public NullableBoolFieldMapper(StoreConfig config, Type type) : base(config, type) { }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            var value = mirror.LoadBoolNull(primPos);
            if (!NullableMapper.WriteNull(ref writer, value))
                Write(ref writer, value);
        }
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            var value = Read(ref reader,false, out bool success);
            mirror.StoreBoolNull(primPos, value);
            return success;
        }
    }
}

#endif

