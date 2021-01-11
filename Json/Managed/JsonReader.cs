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
        
        public JsonReader(PropType.Store store) {
            typeCache   = new PropType.Cache(store);
            parser      = new JsonParser();
            parser.error.throwException = false;
        }
        
        public void Dispose() {
            type.Dispose();
            parser.Dispose();
        }

        public Object ErrorNull (String msg) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg);
            return null;
        }

        private static readonly int minLen = 8;

        private static int Inc (int len) {
            return len < 5 ? minLen : 2 * len;      
        }
        
        public T Read<T>(Bytes bytes) {
            return (T)Read(bytes.buffer, bytes.Start, bytes.Len, typeof(T));
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
                    return ReadObject(null, type);
                case JsonEvent. ArrayStart:
                    return ReadJsonArray(null, PropCollection.Info.CreateCollection(type));
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state in Read() : " + ev. ToString());
                }
            }
        }

        public Object ReadTo (Bytes bytes, Object obj) {
            return ReadTo(bytes.buffer, bytes.Start, bytes.Len, obj);
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
                    return ReadObject(obj, obj. GetType());
                case JsonEvent. ArrayStart:
                    return ReadJsonArray(null, null);
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state Read() : " + ev. ToString());
                }
            }
        }
    
        protected Object ReadObject (Object obj, Type type) {
            PropType propType = typeCache.Get (type );
            return ReadObjectType(obj, propType);
        }

        private Object ReadObjectType (Object obj, PropType propType) {
            JsonEvent ev = parser.NextEvent();
            if (obj == null)
            {
                // Is first member "$type": "<typeName>" ?
                if (ev == JsonEvent.ValueString && type.IsEqualBytes(parser.key))
                {
                    propType = typeCache.GetByName (parser.value);
                    if (propType == null)
                        return ErrorNull("Object $type not found: " + parser.value);
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
                        return ErrorNull("Expected type String. Field type: " + field.type);
                    break;
                case JsonEvent. ValueNumber:
                    field = propType.GetField(parser.key);
                    if (field == null)
                        break;
                    if (!SimpleType.IsNumber(field.type))
                        return ErrorNull("Field is not a number. Field type: " + field.type);
                    if (parser.isFloat) {
                        double val = parser.ValueAsDouble(out bool success);
                        field.SetDouble(obj, val);
                        if (!success)
                            return ValueParseError();
                    } else {
                        long val = parser.ValueAsLong(out bool success);
                        field.SetLong(obj, val);
                        if (!success)
                            return ValueParseError();
                    }
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
                    default:            return ErrorNull("Field type not nullable. Field type: " + field.type);
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
                        if (collection != null)
                        {
                            Type collectionInterface = collection.typeInterface;
                            if (collectionInterface == typeof( IDictionary<,> ))
                            {
                                if (field.collection.elementType == typeof( String ))
                                    sub = ReadMapString(sub, field);
                                else
                                    sub = ReadMapType(sub, field);
                            }
                            else
                                return ErrorNull("unsupported collection Type: " + collectionInterface. Name);
                        }
                        else
                        {
                            sub = ReadObjectType (sub, field.GetFieldPropType(typeCache));
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
                            return ErrorNull("expected field with array nature: " + field.name);
                        Object array = field.GetObject(obj);
                        Object arrayRet = ReadJsonArray( array, field.collection);
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
                    return ErrorNull("unexpected state: " + ev. ToString());
                }
                ev = parser.NextEvent();
            }
        }

        // @SuppressWarnings("unchecked")
        private Object ReadMapType (Object obj, PropField field) {
            if (obj == null)
                obj = field.CreateCollection();
        
            PropCollection collection = field.collection;
            if (collection.elementPropType == null)
                collection.elementPropType = typeCache.Get(collection.elementType);
            IDictionary map = (IDictionary) obj;        
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
                    Object value = ReadObjectType(null, collection.elementPropType);
                    if (value == null)
                        return null;
                    map [ key ] = value ;
                    break;
                case JsonEvent. ObjectEnd:
                    return obj;
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state: " + ev. ToString());
                }
            }
        }


        private Object ReadMapString (Object obj, PropField field) {
            if (obj == null)
                obj = field.CreateCollection();
            
            IDictionary <String,String> map = (IDictionary <String,String>) obj;        
            while (true)
            {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueNull:
                    String key = parser.key.ToString();
                    map [ key ]= null ;
                    break;
                case JsonEvent. ValueString:
                    key = parser.key.ToString();
                    map [ key ]= parser.value.ToString() ;
                    break;
                case JsonEvent. ObjectEnd:
                    return obj;
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state: " + ev. ToString());
                }
            }
        }

        private Object ReadJsonArray (Object col, PropCollection collection) {
            Type typeInterface = collection.typeInterface;
            if (typeInterface == typeof( Array ))
            {
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
                    return ErrorNull("unsupported array type: " + collection.id);
                }
            }
            if (typeInterface == typeof( IList<> ))
                return ReadList (col, collection);
            return ErrorNull("unsupported collection interface: " + typeInterface);
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
                collection.elementPropType = typeCache.Get(collection.elementType);
            int index = 0;
            while (true)
            {
                JsonEvent ev = parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueString:
                case JsonEvent. ValueNumber:
                case JsonEvent. ValueBool:
                    return ErrorNull("expect array item of type: " + collection.elementType. Name);
                case JsonEvent. ValueNull:
                    if (index >= len)
                        array = Arrays. CopyOfType(collection.elementType, array, len = Inc(len));
                    array.SetValue (null, index++ );
                    break;
                case JsonEvent. ObjectStart:
                    if (index < startLen) {
                        Object oldElement = array .GetValue( index );
                        Object element = ReadObjectType(oldElement, collection.elementPropType);
                        if (element == null)
                            return null;
                        array.SetValue (element, index);
                    } else {
                        Object element = ReadObjectType(null, collection.elementPropType);
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
                    return ErrorNull("unexpected state: " + ev. ToString());
                }
            }
        }

        // @SuppressWarnings("unchecked")
        private Object ReadList (Object col, PropCollection collection) {
            if (col == null)
                col = collection.CreateInstance();
            if (collection.elementPropType == null)
                collection.elementPropType = typeCache.Get(collection.elementType);
            IList list = (IList) col;
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
                case JsonEvent. ValueBool:
                    return ErrorNull("expect array item of type: " + collection.elementType. Name);
                case JsonEvent. ValueNull:
                    if (index < startLen)
                        list [ index ] = null ;
                    else
                        list. Add (null);
                    index++;
                    break;
                case JsonEvent. ObjectStart:
                    if (index < startLen) {
                        Object oldElement = list [ index ];
                        Object element = ReadObjectType(oldElement, collection.elementPropType);
                        if (element == null)
                            return null;
                        list [ index ] = element ;
                    } else {
                        Object element = ReadObjectType(null, collection.elementPropType);
                        if (element == null)
                            return null;
                        list. Add (element);
                    }
                    index++;
                    break;
                case JsonEvent. ArrayEnd:
                    for (int n = startLen - 1; n >= index; n--)
                        list. Remove (n);
                    return col;
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("unexpected state: " + ev. ToString());
                }
            }
        }

        /* ----------------------------------------- array readers -------------------------------------------- */
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        private Object ValueParseError () {
            return null; // ErrorNull(parser.parseCx.GetError().ToString());
        }
        
        private Object ArrayUnexpected (JsonEvent ev) {
            return ErrorNull("unexpected state in array: " + ev. ToString());
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
                    array[index++] = (short)parser.ValueAsInt(out bool success);
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
                    array[index++] = (byte) parser.ValueAsInt(out bool success);
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
                    array[index++] = (float) parser.ValueAsDouble(out bool success);
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
    }
}
