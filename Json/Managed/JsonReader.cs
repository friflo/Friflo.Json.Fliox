// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Prop.Resolver;


namespace Friflo.Json.Managed
{
    // JsonReader
    public class JsonReader : IDisposable
    {
        public              JsonParser          parser;
        public readonly    PropType.Cache      typeCache;

        private readonly    Bytes               discriminator = new Bytes ("$type"); 

        public              JsonError           Error  =>  parser.error;
        public              SkipInfo            SkipInfo  =>  parser.skipInfo;
        
        public JsonReader(TypeStore typeStore) {
            typeCache   = new PropType.Cache(typeStore);
            parser = new JsonParser {error = {throwException = false}};
        }
        
        public void Dispose() {
            discriminator.Dispose();
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

        public static readonly int minLen = 8;

        public static int Inc (int len) {
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
                    NativeType propType = typeCache.GetType (type);               // lookup required
                    return ReadJsonObject(null, propType);
                case JsonEvent. ArrayStart:
                    NativeType collection = typeCache.GetType(type);  // lookup required 
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
                    NativeType propType = typeCache.GetType (obj.GetType());              // lookup required
                    return ReadJsonObject(obj, propType);
                case JsonEvent. ArrayStart:
                    NativeType collection = typeCache.GetType(obj.GetType()); // lookup required
                    return ReadJsonArray(obj, collection, 0);
                case JsonEvent. Error:
                    return null;
                default:
                    return ErrorNull("ReadTo() can only used on an JSON object or array", ev);
                }
            }
        }
    
        public Object ReadJsonObject (Object obj, NativeType nativeType) {
            if (nativeType.objectResolver != null)
                return nativeType.objectResolver(this, obj, nativeType);
            throw new InvalidOperationException("No object resolver for type: " + nativeType.type.FullName);
        }
            
        public static Object ReadObject (JsonReader reader, object obj, NativeType nativeType) {
            ref var parser = ref reader.parser;
            PropType propType = (PropType)nativeType;
            JsonEvent ev = parser.NextEvent();
            if (obj == null) {
                // Is first member is discriminator - "$type": "<typeName>" ?
                if (ev == JsonEvent.ValueString && reader.discriminator.IsEqualBytes(parser.key)) {
                    propType = (PropType)reader.typeCache.GetTypeByName (parser.value);
                    if (propType == null)
                        return reader.ErrorNull("Object with discriminator $type not found: ", ref parser.value);
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
                        return reader.ErrorNull("Expected type String. Field type: ", ref field.nameBytes);
                    break;
                case JsonEvent. ValueNumber:
                    field = propType.GetField(parser.key);
                    if (field == null)
                        break;
                    bool success = field.SetNumber(ref parser, obj);
                    if (!success)
                        return reader.ValueParseError();
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
                    default:            return reader.ErrorNull("Field type not nullable. Field type: ", ref field.nameBytes);
                    }   
                    break;
                case JsonEvent. ObjectStart:
                    field = propType.GetField(parser.key);
                    if (field == null) {
                        if (!parser.SkipTree())
                            return null;
                    } else {
                        Object sub = field.GetObject(obj);
                        NativeType fieldObject = field.GetFieldObject(reader.typeCache);
                        if (fieldObject == null)
                            throw new InvalidOperationException("Field is not compatible to JSON object: " + field.fieldType.FullName);
                        
                        sub = reader.ReadJsonObject(sub, fieldObject);
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
                    else {
                        NativeType fieldArray = field.GetFieldArray(reader.typeCache); 
                        if (fieldArray == null)
                            return reader.ErrorNull("expected field with array nature: ", ref field.nameBytes);
                        Object array = field.GetObject(obj);
                        Object arrayRet = reader.ReadJsonArray( array, fieldArray, 0);
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
                    return reader.ErrorNull("unexpected state: ", ev);
                }
                ev = parser.NextEvent();
            }
        }

        public static Object ReadMapType (JsonReader reader, object obj, NativeType nativeType) {
            PropCollection collection = (PropCollection)nativeType;
            if (obj == null)
                obj = collection.CreateInstance();
            IDictionary map = (IDictionary) obj;
            ref var parser = ref reader.parser;
            NativeType elementPropType = collection.GetElementPropType(reader.typeCache);
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
                    Object value = reader.ReadJsonObject(null, elementPropType);
                    if (value == null)
                        return null;
                    map [ key ] = value ;
                    break;
                case JsonEvent.ValueString:
                    key = parser.key.ToString();
                    if (collection.id != SimpleType.Id.String)
                        return reader.ErrorNull("Expect Dictionary value type string. Found: ", collection.elementType.Name);
                    map[key] = parser.value.ToString();
                    break;
                case JsonEvent.ValueNumber:
                    key = parser.key.ToString();
                    map[key] = reader.NumberFromValue(collection.id, out bool successNum);
                    if (!successNum)
                        return null;
                    break;
                case JsonEvent.ValueBool:
                    key = parser.key.ToString();
                    map[key] = reader.BoolFromValue(collection.id, out bool successBool);
                    if (!successBool)
                        return null;
                    break;
                case JsonEvent. ObjectEnd:
                    return map;
                case JsonEvent. Error:
                    return null;
                default:
                    return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }

        private readonly ReadJsonArrayResolver readJsonArrayResolver = new ReadJsonArrayResolver();

        // ReSharper disable once UnusedParameter.Local
        public Object ReadJsonArray(Object col, NativeType nativeType, int index) {
            PropCollection collection = (PropCollection) nativeType;
            Func<JsonReader, object, NativeType, object> resolver = readJsonArrayResolver.GetReadResolver(collection);
            if (resolver != null)
                return resolver(this, col, collection);

            return ErrorNull("unsupported collection interface: ", collection.typeInterface.Name);
        }
    
        public static Object ReadList (JsonReader reader, object col, NativeType nativeType) {
            PropCollection collection = (PropCollection) nativeType;
            IList list = (IList) col;
            if (list == null)
                list = (IList)collection.CreateInstance();
            NativeType elementPropType = collection.GetElementPropType(reader.typeCache);
            if (collection.id != SimpleType.Id.Object)
                list. Clear();
            int startLen = list. Count;
            int index = 0;
            while (true)
            {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev)
                {
                case JsonEvent. ValueString:
                    list. Add (reader.parser.value.ToString());
                    break;
                case JsonEvent. ValueNumber:
                    object num = reader.NumberFromValue(collection.id, out bool success);
                    if (!success)
                        return null;
                    list.Add(num);
                    break;
                case JsonEvent. ValueBool:
                    object bln = reader.BoolFromValue(collection.id, out bool boolSuccess);
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
                    NativeType elementCollection = reader.typeCache.GetType(collection.elementType);
                    if (index < startLen) {
                        Object oldElement = list [ index ];
                        Object element = reader.ReadJsonArray(oldElement, elementCollection, 0);
                        if (element == null)
                            return null;
                        list [ index ] = element ;
                    } else {
                        Object element = reader.ReadJsonArray(null, elementCollection, 0);
                        if (element == null)
                            return null;
                        list. Add (element);
                    }
                    index++;
                    break;            
                case JsonEvent. ObjectStart:
                    if (index < startLen) {
                        Object oldElement = list [ index ];
                        Object element = reader.ReadJsonObject(oldElement, elementPropType);
                        if (element == null)
                            return null;
                        list [ index ] = element ;
                    } else {
                        Object element = reader.ReadJsonObject(null, elementPropType);
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
                    return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }

        /* ----------------------------------------- array readers -------------------------------------------- */
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        public Object ValueParseError () {
            return null; // ErrorNull(parser.parseCx.GetError().ToString());
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
