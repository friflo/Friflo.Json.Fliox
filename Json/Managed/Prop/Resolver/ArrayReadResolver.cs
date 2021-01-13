using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop.Resolver
{
    public class ObjectArrayCodec : IJsonCodec
    {
        public static readonly ObjectArrayCodec Resolver = new ObjectArrayCodec();
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            PropCollection collection = (PropCollection) nativeType;
            Array arr = (Array) obj;
            writer.bytes.AppendChar('[');
            NativeType elementType = collection.GetElementType(writer.typeCache);
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                object item = arr.GetValue(n);
                if (item == null)
                    writer.bytes.AppendBytes(ref writer.@null);
                else
                    writer.WriteJson(item, elementType);
            }
            writer.bytes.AppendChar(']');
        }

        public object Read(JsonReader reader, object col, NativeType nativeType) {
            var collection = (PropCollection) nativeType;
            int startLen;
            int len;
            Array array;
            if (col == null) {
                startLen = 0;
                len = JsonReader.minLen;
                array = Arrays.CreateInstance(collection.elementType, len);
            }
            else {
                array = (Array) col;
                startLen = len = array.Length;
            }

            NativeType elementType = collection.GetElementType(reader.typeCache);
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        // array of string, bool, int, long, float, double, short, byte are handled in ReadJsonArray()
                        return reader.ErrorNull("expect array item of type: ", collection.elementType.Name);
                    case JsonEvent.ValueNull:
                        if (index >= len)
                            array = Arrays.CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                        array.SetValue(null, index++);
                        break;
                    case JsonEvent.ArrayStart:
                        NativeType subElementArray = collection.GetElementType(reader.typeCache);
                        if (index < startLen) {
                            Object oldElement = array.GetValue(index);
                            Object element = reader.ReadJson(oldElement, subElementArray, 0);
                            if (element == null)
                                return null;
                            array.SetValue(element, index);
                        }
                        else {
                            Object element = reader.ReadJson(null, subElementArray, 0);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                            array.SetValue(element, index);
                        }

                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            Object oldElement = array.GetValue(index);
                            Object element = reader.ReadJson(oldElement, elementType, 0);
                            if (element == null)
                                return null;
                            array.SetValue(element, index);
                        }
                        else {
                            Object element = reader.ReadJson(null, elementType, 0);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                            array.SetValue(element, index);
                        }

                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOfType(collection.elementType, array, index);
                        return array;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }

 

    public class StringArrayCodec : IJsonCodec
    {
        public static readonly StringArrayCodec Resolver = new StringArrayCodec();
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
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

        public Object Read(JsonReader reader, Object col, NativeType nativeType) {
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
        public static readonly LongArrayCodec Resolver = new LongArrayCodec();

        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            long[] arr = (long[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendLong(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, NativeType nativeType) {
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
        public static readonly IntArrayCodec Resolver = new IntArrayCodec();
        
        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            int[] arr = (int[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public  Object Read(JsonReader reader, Object col, NativeType nativeType) {
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
        public static readonly ShortArrayCodec Resolver = new ShortArrayCodec();
        
        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            short[] arr = (short[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, NativeType nativeType) {
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
        public static readonly ByteArrayCodec Resolver = new ByteArrayCodec();
        
        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            byte[] arr = (byte[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, NativeType nativeType) {
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
        public static readonly BoolArrayCodec Resolver = new BoolArrayCodec();
        
        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            bool[] arr =(bool[])obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendBool(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public Object Read(JsonReader reader, Object col, NativeType nativeType) {
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
        public static readonly DoubleArrayCodec Resolver = new DoubleArrayCodec();
        
        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            double[] arr = (double[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendDbl(ref writer.bytes, arr[n]);
                //  bytes.Append( arr[n] .ToString());  // precise conversion
            }
            writer.bytes.AppendChar(']');
        }


        public Object Read(JsonReader reader, Object col, NativeType nativeType) {
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
        public static readonly FloatArrayCodec Resolver = new FloatArrayCodec();
        
        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            float[] arr = (float[]) obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendFlt(ref writer.bytes, arr[n]);
                //  bytes.Append( arr[n] .ToString());  // precise conversion
            }
            writer.bytes.AppendChar(']');
        }

        
        public Object Read(JsonReader reader, Object col, NativeType nativeType) {
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