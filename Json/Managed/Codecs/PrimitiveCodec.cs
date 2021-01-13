// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Prop;

namespace Friflo.Json.Managed.Codecs
{
    public class StringCodec : IJsonCodec
    {
        public static readonly StringCodec Resolver = new StringCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(string))
                return null;
            return new NativeType (typeof(string), Resolver);
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
    
    public class DoubleCodec : IJsonCodec
    {
        public static readonly DoubleCodec Resolver = new DoubleCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(double))
                return null;
            return new NativeType (typeof(double), Resolver);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendDbl(ref writer.bytes, (double) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueNumber) {
                object num = reader.parser.ValueAsDouble(out bool success);
                if (success)
                    return num;
            }
            return null;
        }
    }
    
    public class FloatCodec : IJsonCodec
    {
        public static readonly FloatCodec Resolver = new FloatCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(float))
                return null;
            return new NativeType (typeof(float), Resolver);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendFlt(ref writer.bytes, (float) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueNumber) {
                object num = reader.parser.ValueAsFloat(out bool success);
                if (success)
                    return num;
            }
            return null;
        }
    }
    
    public class LongCodec : IJsonCodec
    {
        public static readonly LongCodec Resolver = new LongCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(long))
                return null;
            return new NativeType (typeof(long), Resolver);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendLong(ref writer.bytes, (long) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueNumber) {
                object num = reader.parser.ValueAsLong(out bool success);
                if (success)
                    return num;
            }
            return null;
        }
    }
    
    public class IntCodec : IJsonCodec
    {
        public static readonly IntCodec Resolver = new IntCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(int))
                return null;
            return new NativeType (typeof(int), Resolver);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendInt(ref writer.bytes, (int) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueNumber) {
                object num = reader.parser.ValueAsInt(out bool success);
                if (success)
                    return num;
            }
            return null;
        }
    }
    
    public class ShortCodec : IJsonCodec
    {
        public static readonly ShortCodec Resolver = new ShortCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(short))
                return null;
            return new NativeType (typeof(short), Resolver);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendInt(ref writer.bytes, (short) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueNumber) {
                object num = reader.parser.ValueAsShort(out bool success);
                if (success)
                    return num;
            }
            return null;
        }
    }
    
    public class ByteCodec : IJsonCodec
    {
        public static readonly ByteCodec Resolver = new ByteCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(byte))
                return null;
            return new NativeType (typeof(byte), Resolver);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendInt(ref writer.bytes, (byte) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueNumber) {
                object num = reader.parser.ValueAsByte(out bool success);
                if (success)
                    return num;
            }
            return null;
        }
    }
    
    public class BoolCodec : IJsonCodec
    {
        public static readonly BoolCodec Resolver = new BoolCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(bool))
                return null;
            return new NativeType (typeof(bool), Resolver);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            writer.format.AppendBool(ref writer.bytes, (bool) obj);
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueBool) {
                object num = reader.parser.ValueAsBool(out bool success);
                if (success)
                    return num;
            }
            return null;
        }
    }
    

    public class Primitive : NativeType {
        public Primitive(Type type) : 
            base(type, PrimitiveCodec.Resolver) {
        }

        public override object CreateInstance() {
            throw new NotSupportedException("primitives don't use a codec" + type.FullName);
        }
    }
    
    public class PrimitiveCodec : IJsonCodec
    {
        public static readonly PrimitiveCodec Resolver = new PrimitiveCodec();

        public static bool IsPrimitive(Type type) {
            return type.IsPrimitive || type == typeof(string);
        } 

        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (IsPrimitive(type))
                return new Primitive(type);
            return null;
        }

        public object Read(JsonReader reader, object obj, NativeType nativeType) {
            throw new InvalidOperationException("primitives don't use a codec. type: " + nativeType.type.FullName);
        }

        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            throw new InvalidOperationException("primitives don't use a codec. type: " + nativeType.type.FullName);
        }
    }
}