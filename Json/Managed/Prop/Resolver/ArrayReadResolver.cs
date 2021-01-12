using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop.Resolver
{
    public class ReadArrayObject : IJsonArray
    {
        public static readonly ReadArrayObject Resolver = new ReadArrayObject();

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
                            Object element = reader.ReadJsonArray(oldElement, subElementArray, 0);
                            if (element == null)
                                return null;
                            array.SetValue(element, index);
                        }
                        else {
                            Object element = reader.ReadJsonArray(null, subElementArray, 0);
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
                            Object element = reader.ReadJsonObject(oldElement, elementType);
                            if (element == null)
                                return null;
                            array.SetValue(element, index);
                        }
                        else {
                            Object element = reader.ReadJsonObject(null, elementType);
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

 

    public class ReadArrayString : IJsonArray
    {
        public static readonly ReadArrayString Resolver = new ReadArrayString();

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

    public class ReadArrayLong : IJsonArray
    {
        public static readonly ReadArrayLong Resolver = new ReadArrayLong();

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

    public class ReadArrayInt : IJsonArray
    {
        public static readonly ReadArrayInt Resolver = new ReadArrayInt();
            
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

    public class ReadArrayShort : IJsonArray
    {
        public static readonly ReadArrayShort Resolver = new ReadArrayShort();

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

    public class ReadArrayByte : IJsonArray
    {
        public static readonly ReadArrayByte Resolver = new ReadArrayByte();

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

    public class ReadArrayBool : IJsonArray
    {
        public static readonly ReadArrayBool Resolver = new ReadArrayBool();

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

    public class ReadArrayDouble : IJsonArray
    {
        public static readonly ReadArrayDouble Resolver = new ReadArrayDouble();

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

    public class ReadArrayFloat : IJsonArray
    {
        public static readonly ReadArrayFloat Resolver = new ReadArrayFloat();
        
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