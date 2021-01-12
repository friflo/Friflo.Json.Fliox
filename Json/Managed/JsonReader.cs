// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed
{
    // JsonReader
    public class JsonReader : IDisposable
    {
        private             JsonParser          parser;
        private readonly    PropType.Cache      typeCache;

        private readonly    Bytes               type = new Bytes ("$type"); 

        public              JsonError           Error  =>  parser.error;
        public              SkipInfo            SkipInfo  =>  parser.skipInfo;
        
        public JsonReader(TypeStore typeStore) {
            typeCache   = new PropType.Cache(typeStore);
            parser = new JsonParser {error = {throwException = false}};
        }
        
        public void Dispose() {
            type.Dispose();
            parser.Dispose();
        }

        public Object ErrorNull (string msg, string value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value);
            return null;
        }
        
        public Object ErrorNull (string msg, JsonEvent ev) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + ev.ToString());
            return null;
        }
        
        public Object ErrorNull (string msg, ref Bytes value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value.ToStr32());
            return null;
        }

        private static readonly int minLen = 8;

        private static int Inc (int len) {
            return len < 5 ? minLen : 2 * len;      
        }
        
        public T Read<T>(Bytes bytes) {
            int start = bytes.Start;
            int len = bytes.Len;
            var ret = Read(bytes.buffer, start, len, typeof(T));
            return (T) ret;
        }
        
        public Object Read(Bytes bytes, Type type) {
            return Read(bytes.buffer, bytes.Start, bytes.Len, type);
        }

        public Object Read(ByteList bytes, int offset, int len, Type type) {           
            parser.InitParser(bytes, offset, len);
        
            while (true)
            {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ObjectStart:
                    PropType propType = typeCache.GetType (type);
                    return ReadJsonObject(null, propType);
                case JsonEvent. ArrayStart:
                    PropCollection collection = typeCache.GetCollection(type); 
                    return ReadJsonArray(null, collection, 0);
                case JsonEvent.ValueString:
                    return parser.value.ToString();
                case JsonEvent.ValueNumber:
                    object num = NumberFromValue(SimpleType.IdFromType(type), out bool success);
                    if (success)
                        return num;
                    return null;
                case JsonEvent.ValueBool:
                    object bln = BoolFromValue(SimpleType.IdFromType(type), out bool successBool);
                    if (successBool)
                        return bln;
                    return parser.boolValue;
                case JsonEvent.ValueNull:
                    if (parser.error.ErrSet)
                        return null;
                    return null;
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state in Read() : ", ev);
                }
            }
        }

        public Object ReadTo (Bytes bytes, Object obj) {
            int start = bytes.Start;
            int len = bytes.Len;
            return ReadTo(bytes.buffer, start, len, obj);
        }
        
        public Object ReadTo (ByteList bytes, int offset, int len, Object obj)
        {
            parser.InitParser(bytes, offset, len);
            
            while (true)
            {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ObjectStart:
                    PropType propType = typeCache.GetType (obj.GetType());
                    return ReadJsonObject(obj, propType);
                case JsonEvent. ArrayStart:
                    PropCollection collection = typeCache.GetCollection(obj.GetType()); 
                    return ReadJsonArray(obj, collection, 0);
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("ReadTo() can only used on an JSON object or array", ev);
                }
            }
        }
    
        private Object ReadJsonObject (Object obj, PropType propType) {
            // support map in maps in ...
            if (typeof(IDictionary).IsAssignableFrom(propType.nativeType)) { //typeof( IDictionary<,> )
                PropCollection collection = typeCache.GetCollection(propType.nativeType);
                obj = collection.CreateInstance();
                return ReadMapType((IDictionary)obj, collection);
            }
            JsonEvent ev = parser.NextEvent();
            if (obj == null)
            {
                // Is first member "$type": "<typeName>" ?
                if (ev == JsonEvent.ValueString && type.IsEqualBytes(parser.key))
                {
                    propType = typeCache.GetByName (parser.value);
                    if (propType == null)
                        return ErrorNull("Object $type not found: ", ref parser.value);
                    ev = parser.NextEvent();
                }
                obj = propType.CreateInstance();
            }

            while (true)
            {
                switch (ev)
                {
                case JsonEvent. ValueString:
                    PropField field = propType.GetField(parser.key);
                    if (field == null)
                        break;
                    if (field.type == SimpleType.Id.String)
                        field.SetString(obj, parser.value.ToString());
                    else
                        return ErrorNull("Expected type String. Field type: ", ref field.nameBytes);
                    break;
                case JsonEvent. ValueNumber:
                    field = propType.GetField(parser.key);
                    if (field == null)
                        break;
                    bool success = field.SetNumber(ref parser, obj);
                    if (!success)
                        return ValueParseError();
                    break;
                case JsonEvent. ValueBool:
                    field = propType.GetField(parser.key);
                    if (field == null)
                        break;
                    field.SetBool(obj, parser.boolValue);
                    break;
                case JsonEvent. ValueNull:
                    field = propType.GetField(parser.key);
                    if (field == null)
                        break;
                    switch (field.type)
                    {
                    case SimpleType.Id. String: field.SetString(obj, null); break;
                    case SimpleType.Id. Object: field.SetObject(obj, null); break;
                    default:            return ErrorNull("Field type not nullable. Field type: ", ref field.nameBytes);
                    }   
                    break;
                case JsonEvent. ObjectStart:
                    field = propType.GetField(parser.key);
                    if (field == null)
                    {
                        if (!parser.SkipTree())
                            return null;
                    }
                    else
                    {
                        Object sub = field.GetObject(obj);
                        PropCollection collection = field.collection;
                        if (collection != null) {
                            Type collectionInterface = collection.typeInterface;
                            if (collectionInterface == typeof( IDictionary<,> )) {
                                if (sub == null)
                                    sub = field.CreateCollection();
                                sub = ReadMapType((IDictionary)sub, collection);
                            } else
                                return ErrorNull("unsupported collection Type: ", collectionInterface. Name);
                        }
                        else
                        {
                            sub = ReadJsonObject (sub, field.GetFieldPropType(typeCache));
                        }
                        if (sub != null)
                            field.SetObject(obj, sub);
                        else
                            return null;
                    }
                    break;
                case JsonEvent. ArrayStart:
                    field = propType.GetField(parser.key);
                    if (field == null)
                    {
                        if (!parser.SkipTree())
                            return null;
                    }
                    else
                    {
                        if (field.collection == null)
                            return ErrorNull("expected field with array nature: ", ref field.nameBytes);
                        Object array = field.GetObject(obj);
                        Object arrayRet = ReadJsonArray( array, field.collection, 0);
                        if (arrayRet != null)
                        {
                            if (array != arrayRet)
                                field.SetObject(obj, arrayRet);
                        }
                        else
                            return null;
                    }
                    break;
                case JsonEvent. ObjectEnd:
                    return obj;
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state: ", ev);
                }
                ev = parser.NextEvent();
            }
        }

        private Object ReadMapType (IDictionary map, PropCollection collection) {
            if (collection.elementPropType == null)
                collection.elementPropType = typeCache.GetType(collection.elementType);
            while (true)
            {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNull:
                    String key = parser.key.ToString();
                    map [ key ] = null ;
                    break;
                case JsonEvent. ObjectStart:
                    key = parser.key.ToString();
                    Object value = ReadJsonObject(null, collection.elementPropType);
                    if (value == null)
                        return null;
                    map [ key ] = value ;
                    break;
                case JsonEvent.ValueString:
                    key = parser.key.ToString();
                    if (collection.id != SimpleType.Id.String)
                        return ErrorNull("Expect Dictionary value type string. Found: ", collection.elementType.Name);
                    map[key] = parser.value.ToString();
                    break;
                case JsonEvent.ValueNumber:
                    key = parser.key.ToString();
                    map[key] = NumberFromValue(collection.id, out bool successNum);
                    if (!successNum)
                        return null;
                    break;
                case JsonEvent.ValueBool:
                    key = parser.key.ToString();
                    map[key] = BoolFromValue(collection.id, out bool successBool);
                    if (!successBool)
                        return null;
                    break;
                case JsonEvent. ObjectEnd:
                    return map;
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state: ", ev);
                }
            }
        }

        private Object ReadJsonArray (Object col, PropCollection collection, int index) {
            Type typeInterface = collection.typeInterface;
            if (typeInterface == typeof( Array )) {
                if (collection.rank > 1)
                    throw new NotSupportedException("multidimensional arrays not supported. Type" + collection.type);
                switch (collection.id)
                {
                    case SimpleType.Id. String:     return ReadArrayString  ((String    [])col);
                    case SimpleType.Id. Long:       return ReadArrayLong    ((long      [])col);
                    case SimpleType.Id. Integer:    return ReadArrayInt     ((int       [])col);
                    case SimpleType.Id. Short:      return ReadArrayShort   ((short     [])col);
                    case SimpleType.Id. Byte:       return ReadArrayByte    ((byte      [])col);
                    case SimpleType.Id. Bool:       return ReadArrayBool    ((bool      [])col);
                    case SimpleType.Id. Double:     return ReadArrayDouble  ((double    [])col);
                    case SimpleType.Id. Float:      return ReadArrayFloat   ((float     [])col);
                    case SimpleType.Id. Object:     return ReadArray        (col, collection);
                    default:
                        return ErrorNull("unsupported array type: ", collection.id.ToString());
                }
            }
            if (typeInterface == typeof( IList<> ))
                return ReadList ((IList)col, collection);
            return ErrorNull("unsupported collection interface: ", typeInterface.Name);
        }
    

        private Object ReadArray (Object col, PropCollection collection) {
            int startLen;
            int len;
            Array array;
            if (col == null)
            {
                startLen = 0;
                len = minLen;
                array = Arrays.CreateInstance(collection.elementType, len);
            }
            else
            {
                array = (Array) col;
                startLen = len = array.Length;
            }
            
            if (collection.elementPropType == null)
                collection.elementPropType = typeCache.GetType(collection.elementType);
            int index = 0;
            while (true)
            {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                    case JsonEvent. ValueString:
                    case JsonEvent. ValueNumber:
                    case JsonEvent. ValueBool:
                        // array of string, bool, int, long, float, double, short, byte are handled in ReadJsonArray()
                        return ErrorNull("expect array item of type: ", collection.elementType. Name);
                    case JsonEvent. ValueNull:
                        if (index >= len)
                            array = Arrays. CopyOfType(collection.elementType, array, len = Inc(len));
                        array.SetValue (null, index++ );
                        break;
                    case JsonEvent. ArrayStart:
                        PropCollection elementCollection = typeCache.GetCollection(collection.elementType);
                        if (index < startLen) {
                            Object oldElement = array .GetValue( index );
                            Object element = ReadJsonArray(oldElement, elementCollection, 0);
                            if (element == null)
                                return null;
                            array.SetValue (element, index);
                        } else {
                            Object element = ReadJsonArray(null, elementCollection, 0);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays. CopyOfType(collection.elementType, array, len = Inc(len));
                            array.SetValue (element, index);
                        }
                        index++;
                        break;
                    case JsonEvent. ObjectStart:
                        if (index < startLen) {
                            Object oldElement = array .GetValue( index );
                            Object element = ReadJsonObject(oldElement, collection.elementPropType);
                            if (element == null)
                                return null;
                            array.SetValue (element, index);
                        } else {
                            Object element = ReadJsonObject(null, collection.elementPropType);
                            if (element == null)
                                return null;
                            if (index >= len)
                                array = Arrays. CopyOfType(collection.elementType, array, len = Inc(len));
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
                        return ErrorNull("unexpected state: ", ev);
                }
            }
        }

        private Object ReadList (IList list, PropCollection collection) {
            if (list == null)
                list = (IList)collection.CreateInstance();
            if (collection.elementPropType == null)
                collection.elementPropType = typeCache.GetType(collection.elementType);

            if (collection.id != SimpleType.Id.Object)
                list. Clear();
            int startLen = list. Count;
            int index = 0;
            while (true)
            {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueString:
                    list. Add (parser.value.ToString());
                    break;
                case JsonEvent. ValueNumber:
                    object num = NumberFromValue(collection.id, out bool success);
                    if (!success)
                        return null;
                    list.Add(num);
                    break;
                case JsonEvent. ValueBool:
                    object bln = BoolFromValue(collection.id, out bool boolSuccess);
                    if (!boolSuccess)
                        return null;
                    list.Add(bln);
                    break;
                case JsonEvent. ValueNull:
                    if (index < startLen)
                        list [ index ] = null ;
                    else
                        list. Add (null);
                    index++;
                    break;
                case JsonEvent. ArrayStart:
                    PropCollection elementCollection = typeCache.GetCollection(collection.elementType);
                    if (index < startLen) {
                        Object oldElement = list [ index ];
                        Object element = ReadJsonArray(oldElement, elementCollection, 0);
                        if (element == null)
                            return null;
                        list [ index ] = element ;
                    } else {
                        Object element = ReadJsonArray(null, elementCollection, 0);
                        if (element == null)
                            return null;
                        list. Add (element);
                    }
                    index++;
                    break;            
                case JsonEvent. ObjectStart:
                    if (index < startLen) {
                        Object oldElement = list [ index ];
                        Object element = ReadJsonObject(oldElement, collection.elementPropType);
                        if (element == null)
                            return null;
                        list [ index ] = element ;
                    } else {
                        Object element = ReadJsonObject(null, collection.elementPropType);
                        if (element == null)
                            return null;
                        list. Add (element);
                    }
                    index++;
                    break;
                case JsonEvent. ArrayEnd:
                    for (int n = startLen - 1; n >= index; n--)
                        list. Remove (n);
                    return list;
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state: ", ev);
                }
            }
        }

        /* ----------------------------------------- array readers -------------------------------------------- */
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        private Object ValueParseError () {
            return null; // ErrorNull(parser.parseCx.GetError().ToString());
        }
        
        private Object ArrayUnexpected (JsonEvent ev) {
            return ErrorNull("unexpected state in array: ", ev);
        }
        
        private Object ReadArrayString (String[] array) {
            if (array == null)
                array = new String[minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                    case JsonEvent. ValueString:
                        if (index >= len)
                            array = Arrays.CopyOf (array, len = Inc(len));
                        array[index++] = parser.value.ToString();
                        break;
                    case JsonEvent. ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOf (array, index);
                        return array;
                    case JsonEvent. Error:
                        return null;
                    default:
                        return ArrayUnexpected(ev);
                }
            }
        }

        private Object ReadArrayLong (long[] array) {
            if (array == null)
                array = new long[minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {   
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = Inc(len));
                    array[index++] = parser.ValueAsLong(out bool success);
                    if (!success)
                        return ValueParseError();
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return ArrayUnexpected(ev);
                }
            }
        }

        private Object ReadArrayInt (int[] array) {
            if (array == null)
                array = new int[minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = Inc(len));
                    array[index++] = parser.ValueAsInt(out bool success);
                    if (!success)
                        return ValueParseError();
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return ArrayUnexpected(ev);
                }
            }
        }

        private Object ReadArrayShort (short[] array) {
            if (array == null)
                array = new short[minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = Inc(len));
                    array[index++] = parser.ValueAsShort(out bool success);
                    if (!success)
                        return ValueParseError();
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return ArrayUnexpected(ev);
                }
            }
        }

        private Object ReadArrayByte (byte[] array) {
            if (array == null)
                array = new byte[minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = Inc(len));
                    array[index++] = parser.ValueAsByte(out bool success);
                    if (!success)
                        return ValueParseError();
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return ArrayUnexpected(ev);
                }
            }
        }

        private Object ReadArrayBool (bool[] array) {
            if (array == null)
                array = new bool [minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueBool:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = Inc(len));
                    array[index++] = parser.boolValue;
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return ArrayUnexpected(ev);
                }
            }
        }

        private Object ReadArrayDouble (double[] array) {
            if (array == null)
                array = new double[minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = Inc(len));
                    array[index++] = parser.ValueAsDouble(out bool success);
                    if (!success)
                        return ValueParseError();
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return ArrayUnexpected(ev);
                }
            }
        }

        private Object ReadArrayFloat (float[] array) {
            if (array == null)
                array = new float[minLen];
            int len = array. Length;
            int index = 0;
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNumber:
                    if (index >= len)
                        array = Arrays.CopyOf (array, len = Inc(len));
                    array[index++] = parser.ValueAsFloat(out bool success);
                    if (!success)
                        return ValueParseError();
                    break;
                case JsonEvent. ArrayEnd:
                    if (index != len)
                        array = Arrays.CopyOf (array, index);
                    return array;
                case JsonEvent. Error:
                    return null;
                default:
                    return ArrayUnexpected(ev);
                }
            }
        }
        //
        private object NumberFromValue(SimpleType.Id? id, out bool success) {
            if (id == null) {
                success = false;
                return null;
            }
            switch (id) {
                case SimpleType.Id. Long:
                    return parser.ValueAsLong(out success);
                case SimpleType.Id. Integer:
                    return parser.ValueAsInt(out success);
                case SimpleType.Id. Short:
                    return parser.ValueAsShort(out success);
                case SimpleType.Id. Byte:
                    return parser.ValueAsByte(out success);
                case SimpleType.Id. Double:
                    return  parser.ValueAsDouble(out success);
                case SimpleType.Id. Float:
                    return  parser.ValueAsFloat(out success);
                default:
                    success = false;
                    return ErrorNull("Cant convert number to: ", id.ToString());
            }
        }

        private object BoolFromValue(SimpleType.Id? id, out bool success) {
            if (id == SimpleType.Id.Bool)
                return parser.ValueAsBool(out success);
            success = false;
            return ErrorNull("Cant convert number to: ", id.ToString());
        }
    }
}
