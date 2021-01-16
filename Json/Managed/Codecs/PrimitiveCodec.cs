// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{

    public static class PrimitiveCodec
    {
        public static bool CheckElse(JsonReader reader, StubType stubType) {
            ref JsonParser parser = ref reader.parser;
            switch (parser.Event) {
                case JsonEvent.ValueNull:
                    if (stubType.isNullable)
                        return false;
                    return reader.ErrorIncompatible("primitive", stubType, ref parser);
                case JsonEvent.Error:
                    return false;
                default:
                    return reader.ErrorIncompatible("primitive", stubType, ref parser);
            }
        }
    }

    public class StringCodec : IJsonCodec
    {
        public static readonly StringCodec Interface = new StringCodec();
        
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(string))
                return null;
            return new StringType(type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString((string) slot.Obj);
            writer.bytes.AppendChar('\"');
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (reader.parser.Event == JsonEvent.ValueString) {
                slot.Obj = reader.parser.value.ToString();
                return true;
            }
            return false;
        }
    }
    
    public class DoubleCodec : IJsonCodec
    {
        public static readonly DoubleCodec Interface = new DoubleCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(double) && type != typeof(double?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            writer.format.AppendDbl(ref writer.bytes, slot.Dbl);
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveCodec.CheckElse(reader, stubType);
            slot.Dbl = reader.parser.ValueAsDoubleStd(out bool success);
            return success;
        }
    }
    
    public class FloatCodec : IJsonCodec
    {
        public static readonly FloatCodec Interface = new FloatCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(float) && type != typeof(float?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            writer.format.AppendFlt(ref writer.bytes, slot.Flt);
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveCodec.CheckElse(reader, stubType);
            slot.Flt = reader.parser.ValueAsFloatStd(out bool success);
            return success;
        }
    }
    
    public class LongCodec : IJsonCodec
    {
        public static readonly LongCodec Interface = new LongCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(long) && type != typeof(long?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            writer.format.AppendLong(ref writer.bytes, slot.Lng);
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveCodec.CheckElse(reader, stubType);
            slot.Lng = reader.parser.ValueAsLong(out bool success);
            return success;
        }
    }
    
    public class IntCodec : IJsonCodec
    {
        public static readonly IntCodec Interface = new IntCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(int) && type != typeof(int?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Int);
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveCodec.CheckElse(reader, stubType);
            slot.Int = reader.parser.ValueAsInt(out bool success);
            return success;
        }
    }
    
    public class ShortCodec : IJsonCodec
    {
        public static readonly ShortCodec Interface = new ShortCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(short) && type != typeof(short?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Short);
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveCodec.CheckElse(reader, stubType);
            slot.Short = reader.parser.ValueAsShort(out bool success);
            return success;
        }
    }
    
    public class ByteCodec : IJsonCodec
    {
        public static readonly ByteCodec Interface = new ByteCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(byte) && type != typeof(byte?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Byte);
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveCodec.CheckElse(reader, stubType);
            slot.Byte = reader.parser.ValueAsByte(out bool success);
            return success;
        }
    }
    
    public class BoolCodec : IJsonCodec
    {
        public static readonly BoolCodec Interface = new BoolCodec();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(bool) && type != typeof(bool?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Slot slot, StubType stubType) {
            writer.format.AppendBool(ref writer.bytes, slot.Bool);
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return PrimitiveCodec.CheckElse(reader, stubType);
            slot.Bool = reader.parser.ValueAsBool(out bool success);
            return success;
        }
    }
}