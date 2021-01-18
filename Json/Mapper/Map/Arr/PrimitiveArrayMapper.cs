// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class StringArrayMapper : IJsonMapper
    {
        public static readonly StringArrayMapper Interface = new StringArrayMapper();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(string), this);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            string[] arr = (string[]) slot.Obj;
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

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            String[] array = (String[]) slot.Obj;
            if (array == null)
                array = new String[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueString) {
                    if (index >= len)
                        array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.value.ToString();
                } else 
                    return ArrayUtils.ArrayElse(reader, ref slot, stubType, array, index, len);
            }
        }
    }

    public class LongArrayMapper : IJsonMapper
    {
        public static readonly LongArrayMapper Interface = new LongArrayMapper();

        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(long), this);
        }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            long[] arr = (long[]) slot.Obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendLong(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            long[] array = (long[]) slot.Obj;
            if (array == null)
                array = new long[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueNumber) {
                    if (index >= len)
                        array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsLong(out bool success);
                    if (!success)
                        return reader.ValueParseError();
                } else 
                    return ArrayUtils.ArrayElse(reader, ref slot, stubType, array, index, len);
            }
        }
    }

    public class IntArrayMapper : IJsonMapper
    {
        public static readonly IntArrayMapper Interface = new IntArrayMapper();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(int), this);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            int[] arr = (int[]) slot.Obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            int[] array = (int[]) slot.Obj;
            if (array == null)
                array = new int[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueNumber) {
                    if (index >= len)
                        array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsInt(out bool success);
                    if (!success)
                        return reader.ValueParseError();
                } else
                    return ArrayUtils.ArrayElse(reader, ref slot, stubType, array, index, len);
            }
        }
    }

    public class ShortArrayMapper : IJsonMapper
    {
        public static readonly ShortArrayMapper Interface = new ShortArrayMapper();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(short), this);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            short[] arr = (short[]) slot.Obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            short[] array = (short[]) slot.Obj;
            if (array == null)
                array = new short[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueNumber) {
                    if (index >= len)
                        array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsShort(out bool success);
                    if (!success)
                        return reader.ValueParseError();
                } else 
                    return ArrayUtils.ArrayElse(reader, ref slot, stubType, array, index, len);
            }
        }
    }

    public class ByteArrayMapper : IJsonMapper
    {
        public static readonly ByteArrayMapper Interface = new ByteArrayMapper();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(byte), this);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            byte[] arr = (byte[]) slot.Obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendInt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            byte[] array = (byte[]) slot.Obj;
            if (array == null)
                array = new byte[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueNumber) {
                    if (index >= len)
                        array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsByte(out bool success);
                    if (!success)
                        return reader.ValueParseError();
                } else
                    return ArrayUtils.ArrayElse(reader, ref slot, stubType, array, index, len);
            }
        }
    }

    public class BoolArrayMapper : IJsonMapper
    {
        public static readonly BoolArrayMapper Interface = new BoolArrayMapper();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(bool), this);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            bool[] arr =(bool[])slot.Obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendBool(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            bool[] array = (bool[]) slot.Obj;
            if (array == null)
                array = new bool [JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueBool) {
                    if (index >= len)
                        array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.boolValue;
                } else 
                    return ArrayUtils.ArrayElse(reader, ref slot, stubType, array, index, len);
            }
        }
    }

    public class DoubleArrayMapper : IJsonMapper
    {
        public static readonly DoubleArrayMapper Interface = new DoubleArrayMapper();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(double), this);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            double[] arr = (double[]) slot.Obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendDbl(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            double[] array = (double[]) slot.Obj;
            if (array == null)
                array = new double[JsonReader.minLen];
            int len = array.Length;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueNumber) {
                    if (index >= len)
                        array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsDouble(out bool success);
                    if (!success)
                        return reader.ValueParseError();
                } else 
                    return ArrayUtils.ArrayElse(reader, ref slot, stubType, array, index, len);
            }
        }
    }

    public class FloatArrayMapper : IJsonMapper
    {
        public static readonly FloatArrayMapper Interface = new FloatArrayMapper();
        
        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveHandler(type, typeof(float), this);
        }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            float[] arr = (float[]) slot.Obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendFlt(ref writer.bytes, arr[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            float[] array = (float[])slot.Obj;
            if (array == null)
                array = new float[JsonReader.minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueNumber) {
                    if (index >= len)
                        array = Arrays.CopyOf(array, len = JsonReader.Inc(len));
                    array[index++] = reader.parser.ValueAsFloat(out bool success);
                    if (!success)
                        return reader.ValueParseError();
                } else 
                    return ArrayUtils.ArrayElse(reader, ref slot, stubType, array, index, len);
            }
        }
    }
}
