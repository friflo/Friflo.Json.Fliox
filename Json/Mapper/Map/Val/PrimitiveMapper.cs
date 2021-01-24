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
    public class StringMapper : TypeMapper
    {
        public static readonly StringMapper Interface = new StringMapper();
        
        public override string DataTypeName() { return "string"; }

        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            WriteUtils.WriteString(writer, (string) slot.Obj);
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
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
    
    public class DoubleMapper : TypeMapper
    {
        public static readonly DoubleMapper Interface = new DoubleMapper();
        
        public override string DataTypeName() { return "double"; }

        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendDbl(ref writer.bytes, slot.Dbl);
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
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
    
    public class FloatMapper : TypeMapper
    {
        public static readonly FloatMapper Interface = new FloatMapper();
        
        public override string DataTypeName() { return "float"; }

        
        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendFlt(ref writer.bytes, slot.Flt);
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
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
    
    public class LongMapper : TypeMapper
    {
        public static readonly LongMapper Interface = new LongMapper();
        
        public override string DataTypeName() { return "long"; }

        
        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendLong(ref writer.bytes, slot.Lng);
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
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
    
    public class IntMapper : TypeMapper
    {
        public static readonly IntMapper Interface = new IntMapper();
        
        public override string DataTypeName() { return "int"; }

        
        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Int);
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
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
    public class ShortMapper : TypeMapper
    {
        public static readonly ShortMapper Interface = new ShortMapper();
        
        public override string DataTypeName() { return "short"; }

        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Short);
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
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
    public class ByteMapper : TypeMapper
    {
        public static readonly ByteMapper Interface = new ByteMapper();
        
        public override string DataTypeName() { return "byte"; }

        
        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendInt(ref writer.bytes, slot.Byte);
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
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
    
    public class BoolMapper : TypeMapper
    {
        public static readonly BoolMapper Interface = new BoolMapper();
        
        public override string DataTypeName() { return "bool"; }

        
        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            writer.format.AppendBool(ref writer.bytes, slot.Bool);
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return ValueUtils.CheckElse(reader, ref slot, stubType);
            slot.Bool = reader.parser.ValueAsBool(out bool success);
            return success;
        }
    }
}