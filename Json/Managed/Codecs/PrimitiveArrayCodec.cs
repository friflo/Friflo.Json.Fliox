// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public static class ArrayUtils {
        public static StubType CreatePrimitiveHandler(Type type, Type itemType, IJsonCodec jsonCodec) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                if (elementType == itemType) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    return new CollectionType(type, elementType, jsonCodec, type.GetArrayRank(), null, constructor);
                }
            }
            return null;
        }
        
        public static bool IsArrayStart(JsonReader reader, StubType stubType) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (stubType.isNullable)
                        return false;
                    reader.ErrorNull("Type is not nullable. Type: ", stubType.type.FullName);
                    return false;
                case JsonEvent.ArrayStart:
                    return true;
                default:
                    reader.ErrorNull("Expect ArrayStart or null. Got Event: ", ev);
                    return false;
            }
        }
    }

    public class StringArrayCodec : IJsonCodec
    {
        public static readonly StringArrayCodec Interface = new StringArrayCodec();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(string), this);
        }
        
        public void Write (JsonWriter writer, object obj, StubType stubType) {
            string[] arr = (string[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                String item = arr[n];
                if (item != null)
                    writer.WriteString(item);
                else
                    writer.bytes.AppendBytes(ref writer.@null);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            String[] array = (String[]) col;
            if (array == null)
                array = new String[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                        array[index++] = reader.parser.value.ToString();
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ArrayUnexpected(reader, ev);
                }
            }
        }
    }

    public class LongArrayCodec : IJsonCodec
    {
        public static readonly LongArrayCodec Interface = new LongArrayCodec();

        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(long), this);
        }

        public void Write(JsonWriter writer, object obj, StubType stubType) {
            long[] arr = (long[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendLong(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            long[] array = (long[]) col;
            if (array == null)
                array = new long[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNumber:
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                        array[index++] = reader.parser.ValueAsLong(out bool success);
                        if (!success)
                            return reader.ValueParseError();
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ArrayUnexpected(reader, ev);
                }
            }
        }
    }

    public class IntArrayCodec : IJsonCodec
    {
        public static readonly IntArrayCodec Interface = new IntArrayCodec();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(int), this);
        }
        
        public void Write(JsonWriter writer, object obj, StubType stubType) {
            int[] arr = (int[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public  Object Read(JsonReader reader, Object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            int[] array = (int[]) col;
            if (array == null)
                array = new int[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNumber:
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                        array[index++] = reader.parser.ValueAsInt(out bool success);
                        if (!success)
                            return reader.ValueParseError();
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ArrayUnexpected(reader, ev);
                }
            }
        }
    }

    public class ShortArrayCodec : IJsonCodec
    {
        public static readonly ShortArrayCodec Interface = new ShortArrayCodec();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(short), this);
        }
        
        public void Write(JsonWriter writer, object obj, StubType stubType) {
            short[] arr = (short[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            short[] array = (short[]) col;
            if (array == null)
                array = new short[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNumber:
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                        array[index++] = reader.parser.ValueAsShort(out bool success);
                        if (!success)
                            return reader.ValueParseError();
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ArrayUnexpected(reader, ev);
                }
            }
        }
    }

    public class ByteArrayCodec : IJsonCodec
    {
        public static readonly ByteArrayCodec Interface = new ByteArrayCodec();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(byte), this);
        }
        
        public void Write(JsonWriter writer, object obj, StubType stubType) {
            byte[] arr = (byte[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            byte[] array = (byte[]) col;
            if (array == null)
                array = new byte[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNumber:
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                        array[index++] = reader.parser.ValueAsByte(out bool success);
                        if (!success)
                            return reader.ValueParseError();
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ArrayUnexpected(reader, ev);
                }
            }
        }
    }

    public class BoolArrayCodec : IJsonCodec
    {
        public static readonly BoolArrayCodec Interface = new BoolArrayCodec();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(bool), this);
        }
        
        public void Write(JsonWriter writer, object obj, StubType stubType) {
            bool[] arr =(bool[])obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendBool(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            bool[] array = (bool[]) col;
            if (array == null)
                array = new bool [JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueBool:
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                        array[index++] = reader.parser.boolValue;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ArrayUnexpected(reader, ev);
                }
            }
        }
    }

    public class DoubleArrayCodec : IJsonCodec
    {
        public static readonly DoubleArrayCodec Interface = new DoubleArrayCodec();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(double), this);
        }
        
        public void Write(JsonWriter writer, object obj, StubType stubType) {
            double[] arr = (double[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendDbl(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            double[] array = (double[]) col;
            if (array == null)
                array = new double[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNumber:
                        if (index >= len)
                            array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                        array[index++] = reader.parser.ValueAsDouble(out bool success);
                        if (!success)
                            return reader.ValueParseError();
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf(array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ArrayUnexpected(reader, ev);
                }
            }
        }
    }

    public class FloatArrayCodec : IJsonCodec
    {
        public static readonly FloatArrayCodec Interface = new FloatArrayCodec();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(float), this);
        }

        public void Write(JsonWriter writer, object obj, StubType stubType) {
            float[] arr = (float[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendFlt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return null;
            float[] array = (float[])col;
            if (array == null)
                array = new float[JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsFloat(out bool success);
                    if (!success)
                        return reader.ValueParseError();
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return reader.ArrayUnexpected(reader, ev);
                }
            }
        }
    }
}
