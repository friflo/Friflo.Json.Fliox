// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;

namespace Friflo.Json.Mapper.Map.Val
{
    public class StringMatcher : ITypeMatcher {
        public static readonly StringMatcher Instance = new StringMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(string))
                return null;
            return new StringMapper(type);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class StringMapper : TypeMapper<string>
    {
        public override string DataTypeName() { return "string"; }
        
        public StringMapper(Type type) : base (type, true) { }

        public override void Write(JsonWriter writer, string slot) {
            WriteUtils.WriteString(writer, (slot));
        }

        public override string Read(JsonReader reader, string slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return ValueUtils.CheckElse(reader, this, out success);
            success = true;
            return reader.parser.value.ToString();
        }
    }
    
    
    public class DoubleMatcher : ITypeMatcher {
        public static readonly DoubleMatcher Instance = new DoubleMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(double) && type != typeof(double?))
                return null;
            return new DoubleMapper (type);
        }
    }
    
    public class DoubleMapper : TypeMapper<double>
    {
        public override string DataTypeName() { return "double"; }
        
        public DoubleMapper(Type type) : base (type, true) { }

        public override void Write(JsonWriter writer, double slot) {
            writer.format.AppendDbl(ref writer.bytes, slot);
        }

        public override double Read(JsonReader reader, double slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsDoubleStd(out success);
        }
    }
    
    
    public class FloatMatcher : ITypeMatcher {
        public static readonly FloatMatcher Instance = new FloatMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(float) && type != typeof(float?))
                return null;
            return new FloatMapper (type);
        }
    }
    
    public class FloatMapper : TypeMapper<float>
    {
        public override string DataTypeName() { return "float"; }

        public FloatMapper(Type type) : base (type, true) { }
        
        public override void Write(JsonWriter writer, float slot) {
            writer.format.AppendFlt(ref writer.bytes, slot);
        }

        public override float Read(JsonReader reader, float slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsFloatStd(out success);
        }
    }
    
    public class LongMatcher : ITypeMatcher {
        public static readonly LongMatcher Instance = new LongMatcher();
                
        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(long) && type != typeof(long?))
                return null;
            return new LongMapper (type);
        }
    }
    
    public class LongMapper : TypeMapper<long>
    {
        public override string DataTypeName() { return "long"; }

        public LongMapper(Type type) : base (type, true) { }
        
        public override void Write(JsonWriter writer, long slot) {
            writer.format.AppendLong(ref writer.bytes, slot);
        }

        public override long Read(JsonReader reader, long slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsLong(out success);
        }
    }
    
    public class IntMatcher : ITypeMatcher {
        public static readonly IntMatcher Instance = new IntMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(int) && type != typeof(int?))
                return null;
            return new IntMapper (type);
        }
    }
    
    public class IntMapper : TypeMapper<int>
    {
        public override string DataTypeName() { return "int"; }

        public IntMapper(Type type) : base (type, true) { }
        
        public override void Write(JsonWriter writer, int slot) {
            writer.format.AppendInt(ref writer.bytes, slot);
        }

        public override int Read(JsonReader reader, int slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsInt(out success);
        }
    }
    
    public class ShortMatcher : ITypeMatcher {
        public static readonly ShortMatcher Instance = new ShortMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(short) && type != typeof(short?))
                return null;
            return new ShortMapper (type);
        }
    }
    public class ShortMapper : TypeMapper<short>
    {
        public override string DataTypeName() { return "short"; }
        
        public ShortMapper(Type type) : base (type, true) { }

        public override void Write(JsonWriter writer, short slot) {
            writer.format.AppendInt(ref writer.bytes, slot);
        }

        public override short Read(JsonReader reader, short slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsShort(out success);
        }
    }
    
    
    public class ByteMatcher : ITypeMatcher {
        public static readonly ByteMatcher Instance = new ByteMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(byte) && type != typeof(byte?))
                return null;
            return new ByteMapper (type);
        }
    }
    public class ByteMapper : TypeMapper<byte>
    {
        public override string DataTypeName() { return "byte"; }

        public ByteMapper(Type type) : base (type, true) { }
        
        public override void Write(JsonWriter writer, byte slot) {
            writer.format.AppendInt(ref writer.bytes, slot);
        }

        public override byte Read(JsonReader reader, byte slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsByte(out success);
        }
    }
    
    public class BoolMatcher : ITypeMatcher {
        public static readonly BoolMatcher Instance = new BoolMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(bool) && type != typeof(bool?))
                return null;
            return new BoolMapper (type);
        }
    }
    
    public class BoolMapper : TypeMapper<bool>
    {
        public override string DataTypeName() { return "bool"; }
        
        public BoolMapper(Type type) : base (type, true) { }

        public override void Write(JsonWriter writer, bool slot) {
            writer.format.AppendBool(ref writer.bytes, slot);
        }

        public override bool Read(JsonReader reader, bool slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsBool(out success);
        }
    }
}