// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.MapIL.Val;

// ReSharper disable PossibleInvalidOperationException
namespace Friflo.Json.Mapper.Map.Val
{
    public class StringMatcher : ITypeMatcher {
        public static readonly StringMatcher Instance = new StringMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(string))
                return null;
            return new StringMapper(config, type);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class StringMapper : TypeMapper<string>
    {
        public override string DataTypeName() { return "string"; }
        
        public StringMapper(StoreConfig config, Type type) : base (config, type, true, false) { }

        public override void Write(JsonWriter writer, string slot) {
            WriteUtils.WriteString(writer, (slot));
        }

        public override string Read(JsonReader reader, string slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return ValueUtils.CheckElse(reader, this, out success);
            success = true;
            return reader.parser.value.GetString(ref reader.charBuf);
            // return reader.parser.value.ToString();
            // return null;
        }
    }
    
    // ---------------------------------------------------------------------------- double
    public class DoubleMatcher : ITypeMatcher {
        public static readonly DoubleMatcher Instance = new DoubleMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(double))
                return config.useIL ? new DoubleFieldMapper(config, type) : new DoubleMapper (config, type);
            if (type == typeof(double?))
                return config.useIL ? new NullableDoubleFieldMapper(config, type) : new NullableDoubleMapper (config, type);
            return null;
        }
    }
    public class DoubleMapper : TypeMapper<double> {
        public override string DataTypeName() { return "double"; }
        
        public DoubleMapper(StoreConfig config, Type type) : base (config, type, false, true) { }

        public override void Write(JsonWriter writer, double slot) {
            writer.format.AppendDbl(ref writer.bytes, slot);
        }
        public override double Read(JsonReader reader, double slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsDoubleStd(out success);
        }
    }
    public class NullableDoubleMapper : TypeMapper<double?> {
        public override string DataTypeName() { return "double?"; }
        
        public NullableDoubleMapper(StoreConfig config, Type type) : base (config, type, true, true) { }

        public override void Write(JsonWriter writer, double? slot) {
            writer.format.AppendDbl(ref writer.bytes, (double)slot);
        }
        public override double? Read(JsonReader reader, double? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsDoubleStd(out success);
        }
    }

    // ---------------------------------------------------------------------------- float
    public class FloatMatcher : ITypeMatcher {
        public static readonly FloatMatcher Instance = new FloatMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(float))
                return config.useIL ? new FloatFieldMapper(config, type) : new FloatMapper (config, type); 
            if (type == typeof(float?))
                return config.useIL ? new NullableFloatFieldMapper(config, type) : new NullableFloatMapper (config, type); 
            return null;
        }
    }
    public class FloatMapper : TypeMapper<float> {
        public override string DataTypeName() { return "float"; }

        public FloatMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void Write(JsonWriter writer, float slot) {
            writer.format.AppendFlt(ref writer.bytes, slot);
        }
        public override float Read(JsonReader reader, float slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsFloatStd(out success);
        }
    }
    public class NullableFloatMapper : TypeMapper<float?> {
        public override string DataTypeName() { return "float?"; }

        public NullableFloatMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void Write(JsonWriter writer, float? slot) {
            writer.format.AppendFlt(ref writer.bytes, (float)slot);
        }
        public override float? Read(JsonReader reader, float? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsFloatStd(out success);
        }
    }

    // ---------------------------------------------------------------------------- long
    public class LongMatcher : ITypeMatcher {
        public static readonly LongMatcher Instance = new LongMatcher();
                
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(long))
                return config.useIL ? new LongFieldMapper(config, type) : new LongMapper (config, type);
            if (type == typeof(long?))
                return config.useIL ? new NullableLongFieldMapper(config, type) : new NullableLongMapper (config, type);
            return null;
        }
    }
    public class LongMapper : TypeMapper<long> {
        public override string DataTypeName() { return "long"; }

        public LongMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void Write(JsonWriter writer, long slot) {
            writer.format.AppendLong(ref writer.bytes, slot);
        }

        public override long Read(JsonReader reader, long slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsLong(out success);
        }
    }
    public class NullableLongMapper : TypeMapper<long?> {
        public override string DataTypeName() { return "long?"; }

        public NullableLongMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void Write(JsonWriter writer, long? slot) {
            writer.format.AppendLong(ref writer.bytes, (long)slot);
        }
        public override long? Read(JsonReader reader, long? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsLong(out success);
        }
    }
    
    // ---------------------------------------------------------------------------- int
    public class IntMatcher : ITypeMatcher {
        public static readonly IntMatcher Instance = new IntMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(int))
                return config.useIL ? new IntFieldMapper(config, type) : new IntMapper (config, type); 
            if (type == typeof(int?))
                return config.useIL ? new NullableIntFieldMapper(config, type) : new NullableIntMapper(config, type);
            return null;
        }
    }
    public class IntMapper : TypeMapper<int> {
        public override string DataTypeName() { return "int"; }

        public IntMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void Write(JsonWriter writer, int slot) {
            writer.format.AppendInt(ref writer.bytes, slot);
        }
        public override int Read(JsonReader reader, int slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsInt(out success);
        }
    }
    public class NullableIntMapper : TypeMapper<int?> {
        public override string DataTypeName() { return "int?"; }

        public NullableIntMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void Write(JsonWriter writer, int? slot) {
            writer.format.AppendInt(ref writer.bytes, (int)slot);
        }
        public override int? Read(JsonReader reader, int? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsInt(out success);
        }
    }
    
    // ---------------------------------------------------------------------------- short
    public class ShortMatcher : ITypeMatcher {
        public static readonly ShortMatcher Instance = new ShortMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(short))
                return config.useIL ? new ShortFieldMapper(config, type) : new ShortMapper (config, type);
            if (type == typeof(short?))
                return config.useIL ? new NullableShortFieldMapper(config, type) : new NullableShortMapper (config, type);
            return null;
        }
    }
    public class ShortMapper : TypeMapper<short> {
        public override string DataTypeName() { return "short"; }
        
        public ShortMapper(StoreConfig config, Type type) : base (config, type, false, true) { }

        public override void Write(JsonWriter writer, short slot) {
            writer.format.AppendInt(ref writer.bytes, slot);
        }

        public override short Read(JsonReader reader, short slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsShort(out success);
        }
    }
    public class NullableShortMapper : TypeMapper<short?> {
        public override string DataTypeName() { return "short?"; }
        
        public NullableShortMapper(StoreConfig config, Type type) : base (config, type, true, true) { }

        public override void Write(JsonWriter writer, short? slot) {
            writer.format.AppendInt(ref writer.bytes, (short)slot);
        }

        public override short? Read(JsonReader reader, short? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsShort(out success);
        }
    }
    
    
    // ---------------------------------------------------------------------------- byte
    public class ByteMatcher : ITypeMatcher {
        public static readonly ByteMatcher Instance = new ByteMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(byte))
                return config.useIL ? new ByteFieldMapper(config, type) : new ByteMapper (config, type); 
            if (type == typeof(byte?))
                return config.useIL ? new NullableByteFieldMapper(config, type) : new NullableByteMapper(config, type);
            return null;
        }
    }
    public class ByteMapper : TypeMapper<byte> {
        public override string DataTypeName() { return "byte"; }

        public ByteMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void Write(JsonWriter writer, byte slot) {
            writer.format.AppendInt(ref writer.bytes, slot);
        }

        public override byte Read(JsonReader reader, byte slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsByte(out success);
        }
    }
    public class NullableByteMapper : TypeMapper<byte?> {
        public override string DataTypeName() { return "byte?"; }

        public NullableByteMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void Write(JsonWriter writer, byte? slot) {
            writer.format.AppendInt(ref writer.bytes, (byte)slot);
        }

        public override byte? Read(JsonReader reader, byte? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsByte(out success);
        }
    }
    
    // ---------------------------------------------------------------------------- bool
    public class BoolMatcher : ITypeMatcher {
        public static readonly BoolMatcher Instance = new BoolMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(bool))
                return config.useIL ? new BoolFieldMapper(config, type) : new BoolMapper (config, type);
            if (type == typeof(bool?))
                return config.useIL ? new NullableBoolFieldMapper(config, type) : new NullableBoolMapper (config, type);
            return null;
        }
    }
    
    public class BoolMapper : TypeMapper<bool> {
        public override string DataTypeName() { return "bool"; }
        
        public BoolMapper(StoreConfig config, Type type) : base (config, type, false, true) { }

        public override void Write(JsonWriter writer, bool slot) {
            writer.format.AppendBool(ref writer.bytes, slot);
        }

        public override bool Read(JsonReader reader, bool slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsBool(out success);
        }
    }
    public class NullableBoolMapper : TypeMapper<bool?> {
        public override string DataTypeName() { return "bool?"; }
        
        public NullableBoolMapper(StoreConfig config, Type type) : base (config, type, true, true) { }

        public override void Write(JsonWriter writer, bool? slot) {
            writer.format.AppendBool(ref writer.bytes, (bool)slot);
        }

        public override bool? Read(JsonReader reader, bool? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return ValueUtils.CheckElse(reader, this, out success);
            return reader.parser.ValueAsBool(out success);
        }
    }
}