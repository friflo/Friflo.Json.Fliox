// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Val
{
    public class StringMatcher : ITypeMatcher {
        public static readonly StringMatcher Instance = new StringMatcher();

        public StubType CreateStubType(Type type) {
            if (type != typeof(string))
                return null;
            return new StringType(type, StringMapper.Interface);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class StringMapper : ITypeMapper
    {
        public static readonly StringMapper Interface = new StringMapper();
        
        public string DataTypeName() { return "string"; }

        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            WriteUtils.WriteString(writer, (string) slot.Obj);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Obj = reader.parser.value.ToString();
            return true;
        }
    }
    
    
    public class DoubleMatcher : ITypeMatcher {
        public static readonly DoubleMatcher Instance = new DoubleMatcher();

        public StubType CreateStubType(Type type) {
            if (type != typeof(double) && type != typeof(double?))
                return null;
            return new PrimitiveType (type, DoubleMapper.Interface);
        }
    }
    
    public class DoubleMapper : ITypeMapper
    {
        public static readonly DoubleMapper Interface = new DoubleMapper();
        
        public string DataTypeName() { return "double"; }

        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendDbl(ref writer.bytes, slot.Dbl);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Dbl = reader.parser.ValueAsDoubleStd(out bool success);
            return success;
        }
    }
    
    
    public class FloatMatcher : ITypeMatcher {
        public static readonly FloatMatcher Instance = new FloatMatcher();

        public StubType CreateStubType(Type type) {
            if (type != typeof(float) && type != typeof(float?))
                return null;
            return new PrimitiveType (type, FloatMapper.Interface);
        }
    }
    
    public class FloatMapper : ITypeMapper
    {
        public static readonly FloatMapper Interface = new FloatMapper();
        
        public string DataTypeName() { return "float"; }

        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendFlt(ref writer.bytes, slot.Flt);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Flt = reader.parser.ValueAsFloatStd(out bool success);
            return success;
        }
    }
    
    public class LongMatcher : ITypeMatcher {
        public static readonly LongMatcher Instance = new LongMatcher();
                
        public StubType CreateStubType(Type type) {
            if (type != typeof(long) && type != typeof(long?))
                return null;
            return new PrimitiveType (type, LongMapper.Interface);
        }
    }
    
    public class LongMapper : ITypeMapper
    {
        public static readonly LongMapper Interface = new LongMapper();
        
        public string DataTypeName() { return "long"; }

        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendLong(ref writer.bytes, slot.Lng);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Lng = reader.parser.ValueAsLong(out bool success);
            return success;
        }
    }
    
    public class IntMatcher : ITypeMatcher {
        public static readonly IntMatcher Instance = new IntMatcher();

        public StubType CreateStubType(Type type) {
            if (type != typeof(int) && type != typeof(int?))
                return null;
            return new PrimitiveType (type, IntMapper.Interface);
        }
    }
    
    public class IntMapper : ITypeMapper
    {
        public static readonly IntMapper Interface = new IntMapper();
        
        public string DataTypeName() { return "int"; }

        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Int);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Int = reader.parser.ValueAsInt(out bool success);
            return success;
        }
    }
    
    public class ShortMatcher : ITypeMatcher {
        public static readonly ShortMatcher Instance = new ShortMatcher();

        public StubType CreateStubType(Type type) {
            if (type != typeof(short) && type != typeof(short?))
                return null;
            return new PrimitiveType (type, ShortMapper.Interface);
        }
    }
    public class ShortMapper : ITypeMapper
    {
        public static readonly ShortMapper Interface = new ShortMapper();
        
        public string DataTypeName() { return "short"; }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Short);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Short = reader.parser.ValueAsShort(out bool success);
            return success;
        }
    }
    
    
    public class ByteMatcher : ITypeMatcher {
        public static readonly ByteMatcher Instance = new ByteMatcher();

        public StubType CreateStubType(Type type) {
            if (type != typeof(byte) && type != typeof(byte?))
                return null;
            return new PrimitiveType (type, ByteMapper.Interface);
        }
    }
    public class ByteMapper : ITypeMapper
    {
        public static readonly ByteMapper Interface = new ByteMapper();
        
        public string DataTypeName() { return "byte"; }

        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Byte);
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Byte = reader.parser.ValueAsByte(out bool success);
            return success;
        }
    }
    
    public class BoolMatcher : ITypeMatcher {
        public static readonly BoolMatcher Instance = new BoolMatcher();

        public StubType CreateStubType(Type type) {
            if (type != typeof(bool) && type != typeof(bool?))
                return null;
            return new PrimitiveType (type, BoolMapper.Interface);
        }
    }
    
    public class BoolMapper : ITypeMapper
    {
        public static readonly BoolMapper Interface = new BoolMapper();
        
        public string DataTypeName() { return "bool"; }

        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
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