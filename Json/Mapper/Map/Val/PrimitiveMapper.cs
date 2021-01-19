// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Val
{
    public class StringMapper : IJsonMapper
    {
        public static readonly StringMapper Interface = new StringMapper();
        
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(string))
                return null;
            return new StringType(type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            writer.WriteString((string) slot.Obj);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event == JsonEvent.ValueString) {
                slot.Obj = reader.parser.value.ToString();
                return true;
            }
            return false;
        }
    }
    
    public class DoubleMapper : IJsonMapper
    {
        public static readonly DoubleMapper Interface = new DoubleMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(double) && type != typeof(double?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            writer.format.AppendDbl(ref writer.bytes, slot.Dbl);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Dbl = reader.parser.ValueAsDoubleStd(out bool success);
            return success;
        }
    }
    
    public class FloatMapper : IJsonMapper
    {
        public static readonly FloatMapper Interface = new FloatMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(float) && type != typeof(float?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            writer.format.AppendFlt(ref writer.bytes, slot.Flt);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Flt = reader.parser.ValueAsFloatStd(out bool success);
            return success;
        }
    }
    
    public class LongMapper : IJsonMapper
    {
        public static readonly LongMapper Interface = new LongMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(long) && type != typeof(long?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            writer.format.AppendLong(ref writer.bytes, slot.Lng);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Lng = reader.parser.ValueAsLong(out bool success);
            return success;
        }
    }
    
    public class IntMapper : IJsonMapper
    {
        public static readonly IntMapper Interface = new IntMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(int) && type != typeof(int?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            writer.format.AppendInt(ref writer.bytes, slot.Int);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Int = reader.parser.ValueAsInt(out bool success);
            return success;
        }
    }
    
    public class ShortMapper : IJsonMapper
    {
        public static readonly ShortMapper Interface = new ShortMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(short) && type != typeof(short?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            writer.format.AppendInt(ref writer.bytes, slot.Short);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Short = reader.parser.ValueAsShort(out bool success);
            return success;
        }
    }
    
    public class ByteMapper : IJsonMapper
    {
        public static readonly ByteMapper Interface = new ByteMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(byte) && type != typeof(byte?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            writer.format.AppendInt(ref writer.bytes, slot.Byte);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Byte = reader.parser.ValueAsByte(out bool success);
            return success;
        }
    }
    
    public class BoolMapper : IJsonMapper
    {
        public static readonly BoolMapper Interface = new BoolMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(bool) && type != typeof(bool?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            writer.format.AppendBool(ref writer.bytes, slot.Bool);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Bool = reader.parser.ValueAsBool(out bool success);
            return success;
        }
    }
}