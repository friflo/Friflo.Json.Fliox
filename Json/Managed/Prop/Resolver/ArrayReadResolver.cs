using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop.Resolver
{
    public class ArrayReadResolver
    {
        public Func<JsonReader, object, PropCollection, object> GetReadResolver(PropCollection collection) {
            if (collection.typeInterface != typeof(Array))
                return null;
            if (collection.rank > 1)
                throw new NotSupportedException("multidimensional arrays not supported. Type" + collection.type);
            switch (collection.id)
            {
                case SimpleType.Id. String:     return ReadArrayString;
                case SimpleType.Id. Long:       return ReadArrayLong;
                case SimpleType.Id. Integer:    return ReadArrayInt;
                case SimpleType.Id. Short:      return ReadArrayShort;
                case SimpleType.Id. Byte:       return ReadArrayByte;
                case SimpleType.Id. Bool:       return ReadArrayBool;
                case SimpleType.Id. Double:     return ReadArrayDouble;
                case SimpleType.Id. Float:      return ReadArrayFloat;
                case SimpleType.Id. Object:     return ReadArrayObject;
                default:
                    throw new NotSupportedException ("unsupported array type: " + collection.id.ToString());
            }
        }
        
        private static object ReadArrayObject (JsonReader reader, object col, PropCollection collection) {
            int startLen;
            int len;
            Array array;
            if (col == null)
            {
                startLen = 0;
                len = JsonReader.minLen;
                array = Arrays.CreateInstance(collection.elementType, len);
            }
            else
            {
                array = (Array) col;
                startLen = len = array.Length;
            }
            
            PropType elementPropType = collection.GetElementPropType(reader.typeCache);
            int index = 0;
            while (true)
            {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                    case JsonEvent. ValueString:
                    case JsonEvent. ValueNumber:
                    case JsonEvent. ValueBool:
                        // array of string, bool, int, long, float, double, short, byte are handled in ReadJsonArray()
                        return reader.ErrorNull("expect array item of type: ", collection.elementType. Name);
                    case JsonEvent. ValueNull:
                        if (index >= len)
                            array = Arrays. CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                        array.SetValue (null, index++ );
                        break;
                    case JsonEvent. ArrayStart:
                        PropCollection elementCollection = reader.typeCache.GetCollection(collection.elementType);
                        if (index < startLen) {
                            Object oldElement = array .GetValue( index );
                            Object element = reader.ReadJsonArray(oldElement, elementCollection, 0);
                            if (element == null)
                                return null;
                            array.SetValue (element, index);
                        } else {
                            Object element = reader.ReadJsonArray(null, elementCollection, 0);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays. CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                            array.SetValue (element, index);
                        }
                        index++;
                        break;
                    case JsonEvent. ObjectStart:
                        if (index < startLen) {
                            Object oldElement = array .GetValue( index );
                            Object element = reader.ReadJsonObject(oldElement, elementPropType);
                            if (element == null)
                                return null;
                            array.SetValue (element, index);
                        } else {
                            Object element = reader.ReadJsonObject(null, elementPropType);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays. CopyOfType(collection.elementType, array, len = JsonReader.Inc(len));
                            array.SetValue (element, index);
                        }
                        index++;
                        break;
                    case JsonEvent. ArrayEnd:
                        if (index != len)
                            array = Arrays.  CopyOfType (collection.elementType, array, index);
                        return array;
                    case JsonEvent. Error:
                        return null;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }

        private static Object ArrayUnexpected (JsonReader reader, JsonEvent ev) {
            return reader.ErrorNull("unexpected state in array: ", ev);
        }
        
        private static Object ReadArrayString (JsonReader reader, Object col, PropCollection collection) {
            String[] array = (String[])col;
            if (array == null)
                array = new String[JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                    case JsonEvent. ValueString:
                        if (index >= len)
                            array = Arrays.CopyOf (array, len = JsonReader.Inc(len));
                        array[index++] = reader.parser.value.ToString();
                        break;
                    case JsonEvent. ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf (array, index);
                        return array;
                    case JsonEvent. Error:
                        return null;
                    default:
                        return ArrayUnexpected(reader, ev);
                }
            }
        }

        private static Object ReadArrayLong (JsonReader reader, Object col, PropCollection collection) {
            long[] array = (long[])col;
            if (array == null)
                array = new long[JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {   
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsLong(out bool success);
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
                    return ArrayUnexpected(reader, ev);
                }
            }
        }

        private static Object ReadArrayInt (JsonReader reader, Object col, PropCollection collection) {
            int[] array = (int[])col;
            if (array == null)
                array = new int[JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsInt(out bool success);
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
                    return ArrayUnexpected(reader, ev);
                }
            }
        }

        private static Object ReadArrayShort (JsonReader reader, Object col, PropCollection collection) {
            short[] array = (short[])col;
            if (array == null)
                array = new short[JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsShort(out bool success);
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
                    return ArrayUnexpected(reader, ev);
                }
            }
        }

        private static Object ReadArrayByte (JsonReader reader, Object col, PropCollection collection) {
            byte[] array = (byte[])col;
            if (array == null)
                array = new byte[JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsByte(out bool success);
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
                    return ArrayUnexpected(reader, ev);
                }
            }
        }

        private static Object ReadArrayBool (JsonReader reader, Object col, PropCollection collection) {
            bool[] array = (bool[])col;
            if (array == null)
                array = new bool [JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueBool:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.boolValue;
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return ArrayUnexpected(reader, ev);
                }
            }
        }

        private static Object ReadArrayDouble (JsonReader reader, Object col, PropCollection collection) {
            double[] array = (double[]) col;
            if (array == null)
                array = new double[JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsDouble(out bool success);
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
                    return ArrayUnexpected(reader, ev);
                }
            }
        }

        private static Object ReadArrayFloat(JsonReader reader, Object col, PropCollection collection) {
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
                    return ArrayUnexpected(reader, ev);
                }
            }
        }
        
    }
}