// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{

    public class StringCodec : IJsonCodec
    {
        public static readonly StringCodec Interface = new StringCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(string))
                return null;
            return new NativeType(type, Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString((string) obj);
            writer.bytes.AppendChar('\"');
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueString) {
                return reader.parser.value.ToString();
            }
            return null;
        }
    }
    
    public class PrimitiveType : NativeType
    {
        public readonly bool nullable;
        
        public PrimitiveType(Type type, IJsonCodec codec)
            : base(type, codec) {
            nullable = Nullable.GetUnderlyingType(type) != null;
        }

        public static object CheckElse(JsonReader reader, NativeType nativeType) {
            switch (reader.parser.Event) {
                case JsonEvent.ValueNull:
                    if (((PrimitiveType) nativeType).nullable)
                        return null;
                    return reader.ErrorNull("primitive is not nullable. type: ", nativeType.type.FullName);
                case JsonEvent.Error:
                    return null;
                default:
                    return reader.ErrorNull("primitive cannot be used within: ", reader.parser.Event);
            }
        }
    }
    
    public class DoubleCodec : IJsonCodec
    {
        public static readonly DoubleCodec Interface = new DoubleCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(double) && type != typeof(double?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendDbl(ref writer.bytes, (double) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveType.CheckElse(reader, nativeType);
            object num = reader.parser.ValueAsDoubleStd(out bool success);
            if (success)
                return num;
            return null;
        }
    }
    
    public class FloatCodec : IJsonCodec
    {
        public static readonly FloatCodec Interface = new FloatCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(float) && type != typeof(float?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendFlt(ref writer.bytes, (float) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveType.CheckElse(reader, nativeType);
            object num = reader.parser.ValueAsFloatStd(out bool success);
            if (success)
                return num;
            return null;
        }
    }
    
    public class LongCodec : IJsonCodec
    {
        public static readonly LongCodec Interface = new LongCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(long) && type != typeof(long?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendLong(ref writer.bytes, (long) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveType.CheckElse(reader, nativeType);
            object num = reader.parser.ValueAsLong(out bool success);
            if (success)
                return num;
            return null;
        }
    }
    
    public class IntCodec : IJsonCodec
    {
        public static readonly IntCodec Interface = new IntCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(int) && type != typeof(int?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendInt(ref writer.bytes, (int) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveType.CheckElse(reader, nativeType);
            object num = reader.parser.ValueAsInt(out bool success);
            if (success)
                return num;
            return null;
        }
    }
    
    public class ShortCodec : IJsonCodec
    {
        public static readonly ShortCodec Interface = new ShortCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(short) && type != typeof(short?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendInt(ref writer.bytes, (short) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveType.CheckElse(reader, nativeType);
            object num = reader.parser.ValueAsShort(out bool success);
            if (success)
                return num;
            return null;
        }
    }
    
    public class ByteCodec : IJsonCodec
    {
        public static readonly ByteCodec Interface = new ByteCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(byte) && type != typeof(byte?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendInt(ref writer.bytes, (byte) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return PrimitiveType.CheckElse(reader, nativeType);
            object num = reader.parser.ValueAsByte(out bool success);
            if (success)
                return num;
            return null;
        }
    }
    
    public class BoolCodec : IJsonCodec
    {
        public static readonly BoolCodec Interface = new BoolCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(bool) && type != typeof(bool?))
                return null;
            return new PrimitiveType (type, Interface);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendBool(ref writer.bytes, (bool) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return PrimitiveType.CheckElse(reader, nativeType);
            object num = reader.parser.ValueAsBool(out bool success);
            if (success)
                return num;
            return null;
        }
    }
}