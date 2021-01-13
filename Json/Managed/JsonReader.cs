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
        public JsonParser parser;
        public readonly PropType.Cache typeCache;

        public readonly Bytes discriminator = new Bytes("$type");

        public JsonError Error => parser.error;
        public SkipInfo SkipInfo => parser.skipInfo;

        public JsonReader(TypeStore typeStore) {
            typeCache = new PropType.Cache(typeStore);
            parser = new JsonParser {error = {throwException = false}};
        }

        public void Dispose() {
            discriminator.Dispose();
            parser.Dispose();
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

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                        NativeType propType = typeCache.GetType(type); // lookup required
                        return ReadJson(null, propType, 0);
                    case JsonEvent.ArrayStart:
                        NativeType collection = typeCache.GetType(type); // lookup required 
                        return ReadJson(null, collection, 0);
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
                    case JsonEvent.Error:
                        return null;
                    default:
                        return ErrorNull("unexpected state in Read() : ", ev);
                }
            }
        }

        public Object ReadTo(Bytes bytes, Object obj) {
            int start = bytes.Start;
            int len = bytes.Len;
            return ReadTo(bytes.buffer, start, len, obj);
        }

        public Object ReadTo(ByteList bytes, int offset, int len, Object obj) {
            parser.InitParser(bytes, offset, len);

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                        NativeType propType = typeCache.GetType(obj.GetType()); // lookup required
                        return ReadJson(obj, propType, 0);
                    case JsonEvent.ArrayStart:
                        NativeType collection = typeCache.GetType(obj.GetType()); // lookup required
                        return ReadJson(obj, collection, 0);
                    case JsonEvent.Error:
                        return null;
                    default:
                        return ErrorNull("ReadTo() can only used on an JSON object or array", ev);
                }
            }
        }
        
        public Object ErrorNull(string msg, string value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value);
            return null;
        }

        public Object ErrorNull(string msg, JsonEvent ev) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + ev.ToString());
            return null;
        }

        public Object ErrorNull(string msg, ref Bytes value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value.ToStr32());
            return null;
        }
        
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        public Object ValueParseError() {
            return null; // ErrorNull(parser.parseCx.GetError().ToString());
        }

        public static readonly int minLen = 8;

        public static int Inc(int len) {
            return len < 5 ? minLen : 2 * len;
        }

        //
        public object NumberFromValue(SimpleType.Id? id, out bool success) {
            if (id == null) {
                success = false;
                return null;
            }

            switch (id) {
                case SimpleType.Id.Long:
                    return parser.ValueAsLong(out success);
                case SimpleType.Id.Integer:
                    return parser.ValueAsInt(out success);
                case SimpleType.Id.Short:
                    return parser.ValueAsShort(out success);
                case SimpleType.Id.Byte:
                    return parser.ValueAsByte(out success);
                case SimpleType.Id.Double:
                    return parser.ValueAsDouble(out success);
                case SimpleType.Id.Float:
                    return parser.ValueAsFloat(out success);
                default:
                    success = false;
                    return ErrorNull("Cant convert number to: ", id.ToString());
            }
        }

        public object BoolFromValue(SimpleType.Id? id, out bool success) {
            if (id == SimpleType.Id.Bool)
                return parser.ValueAsBool(out success);
            success = false;
            return ErrorNull("Cant convert number to: ", id.ToString());
        }
        
        public Object ArrayUnexpected (JsonReader reader, JsonEvent ev) {
            return reader.ErrorNull("unexpected state in array: ", ev);
        }
        
        /// <summary>
        /// Is called for every JSON object found during iteration 
        /// </summary>
        public Object ReadJson(Object obj, NativeType nativeType, int index) {
            if (nativeType.jsonCodec != null)
                return nativeType.jsonCodec.Read(this, obj, nativeType);
            throw new NotSupportedException("found no resolver for JSON object: " + nativeType.type.FullName);
        }
    }

    public interface IJsonCodec {
        object  Read  (JsonReader reader, object obj, NativeType nativeType);
        void    Write (JsonWriter writer, object obj, NativeType nativeType);
    }


    public class ObjectCodec : IJsonCodec {
        public static readonly ObjectCodec Resolver = new ObjectCodec();
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {

            ref var bytes = ref writer.bytes;
            ref var format = ref writer.format;
            
            PropType type = (PropType)nativeType;
            bool firstMember = true;
            bytes.AppendChar('{');
            Type objType = obj.GetType();
            if (type.type != objType) {
                type = (PropType)writer.typeCache.GetType(objType);
                firstMember = false;
                bytes.AppendBytes(ref writer.discriminator);
                Bytes subType = type.typeName;
                if (subType.buffer.IsCreated())
                    bytes.AppendBytes(ref subType);
                else
                    throw new FrifloException("Serializing derived types must be registered: " + objType.Name);
                bytes.AppendChar('\"');
            }

            PropField[] fields = type.propFields.fieldsSerializable;

            for (int n = 0; n < fields.Length; n++) {
                if (firstMember)
                    firstMember = false;
                else
                    bytes.AppendChar(',');
                PropField field = fields[n];
                switch (field.type) {
                    case SimpleType.Id.String:
                        writer.WriteKey(field);
                        String val = field.GetString(obj);
                        if (val != null)
                            writer.WriteString(val);
                        else
                            bytes.AppendBytes(ref writer.@null);
                        break;
                    case SimpleType.Id.Long:
                        writer.WriteKey(field);
                        format.AppendLong(ref bytes, field.GetLong(obj));
                        break;
                    case SimpleType.Id.Integer:
                        writer.WriteKey(field);
                        format.AppendInt(ref bytes, field.GetInt(obj));
                        break;
                    case SimpleType.Id.Short:
                        writer.WriteKey(field);
                        format.AppendInt(ref bytes, field.GetInt(obj));
                        break;
                    case SimpleType.Id.Byte:
                        writer.WriteKey(field);
                        format.AppendInt(ref bytes, field.GetInt(obj));
                        break;
                    case SimpleType.Id.Bool:
                        writer.WriteKey(field);
                        format.AppendBool(ref bytes, field.GetBool(obj));
                        break;
                    case SimpleType.Id.Double:
                        writer.WriteKey(field);
                        format.AppendDbl(ref bytes, field.GetDouble(obj));
                        break;
                    //                                                  bytes.Append(field.GetString(obj));     break;  // precise conversion
                    case SimpleType.Id.Float:
                        writer.WriteKey(field);
                        format.AppendFlt(ref bytes, field.GetFloat(obj));
                        break;
                    //                                                  bytes.Append(field.GetString(obj));     break;  // precise conversion
                    case SimpleType.Id.Object:
                        writer.WriteKey(field);
                        Object child = field.GetObject(obj);
                        if (child == null) {
                            bytes.AppendBytes(ref writer.@null);
                        }
                        else {
                            // todo: use field.GetFieldObject() - remove if, make PropField.collection private
                            NativeType fieldObject = field.GetFieldObject(writer.typeCache);
                            // NativeType subElementType = collection.GetElementType(writer.typeCache);
                            // NativeType fieldType = field.GetFieldObject(writer.typeCache);
                            writer.WriteJson(child, fieldObject);
                            /*
                            NativeType collection = field.collection;
                            if (collection == null)
                                writer.WriteJsonObject(child, field.GetFieldObject(writer.typeCache));
                            else
                                writer.WriteJsonObject(child, collection);
                                */
                        }

                        break;
                    default:
                        throw new FrifloException("invalid field type: " + field.type);
                }
            }
            bytes.AppendChar('}');
        }
            
        public Object Read(JsonReader reader, object obj, NativeType nativeType) {
            ref var parser = ref reader.parser;
            PropType propType = (PropType) nativeType;
            JsonEvent ev = parser.NextEvent();
            if (obj == null) {
                // Is first member is discriminator - "$type": "<typeName>" ?
                if (ev == JsonEvent.ValueString && reader.discriminator.IsEqualBytes(parser.key)) {
                    propType = (PropType) reader.typeCache.GetTypeByName(parser.value);
                    if (propType == null)
                        return reader.ErrorNull("Object with discriminator $type not found: ", ref parser.value);
                    ev = parser.NextEvent();
                }
                obj = propType.CreateInstance();
            }

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                        PropField field = propType.GetField(parser.key);
                        if (field == null)
                            break;
                        if (field.type == SimpleType.Id.String)
                            field.SetString(obj, parser.value.ToString());
                        else
                            return reader.ErrorNull("Expected type String. Field type: ", ref field.nameBytes);
                        break;
                    case JsonEvent.ValueNumber:
                        field = propType.GetField(parser.key);
                        if (field == null)
                            break;
                        bool success = field.SetNumber(ref parser, obj);
                        if (!success)
                            return reader.ValueParseError();
                        break;
                    case JsonEvent.ValueBool:
                        field = propType.GetField(parser.key);
                        if (field == null)
                            break;
                        field.SetBool(obj, parser.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        field = propType.GetField(parser.key);
                        if (field == null)
                            break;
                        switch (field.type) {
                            case SimpleType.Id.String:
                                field.SetString(obj, null);
                                break;
                            case SimpleType.Id.Object:
                                field.SetObject(obj, null);
                                break;
                            default:
                                return reader.ErrorNull("Field type not nullable. Field type: ", ref field.nameBytes);
                        }

                        break;
                    case JsonEvent.ObjectStart:
                        field = propType.GetField(parser.key);
                        if (field == null) {
                            if (!parser.SkipTree())
                                return null;
                        }
                        else {
                            Object sub = field.GetObject(obj);
                            NativeType fieldObject = field.GetFieldObject(reader.typeCache);
                            if (fieldObject == null)
                                throw new InvalidOperationException("Field is not compatible to JSON object: " +
                                                                    field.fieldType.FullName);

                            sub = reader.ReadJson(sub, fieldObject, 0);
                            if (sub != null)
                                field.SetObject(obj, sub);
                            else
                                return null;
                        }

                        break;
                    case JsonEvent.ArrayStart:
                        field = propType.GetField(parser.key);
                        if (field == null) {
                            if (!parser.SkipTree())
                                return null;
                        }
                        else {
                            NativeType fieldArray = field.GetFieldArray(reader.typeCache);
                            if (fieldArray == null)
                                return reader.ErrorNull("expected field with array nature: ", ref field.nameBytes);
                            Object array = field.GetObject(obj);
                            Object arrayRet = reader.ReadJson(array, fieldArray, 0);
                            if (arrayRet != null) {
                                if (array != arrayRet)
                                    field.SetObject(obj, arrayRet);
                            }
                            else
                                return null;
                        }

                        break;
                    case JsonEvent.ObjectEnd:
                        return obj;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }

                ev = parser.NextEvent();
            }
        }
    }

    public class MapCodec : IJsonCodec
    {
        public static readonly MapCodec Resolver = new MapCodec();
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            PropCollection collection = (PropCollection)nativeType;
            IDictionary map = (IDictionary) obj;

            ref var bytes = ref writer.bytes;
            bytes.AppendChar('{');
            int n = 0;
            if (collection.elementType == typeof(String)) {
                // Map<String, String>
                // @SuppressWarnings("unchecked")
                IDictionary<String, String> strMap = (IDictionary<String, String>) map;
                foreach (KeyValuePair<String, String> entry in strMap) {
                    if (n++ > 0) bytes.AppendChar(',');
                    writer.WriteString(entry.Key);
                    bytes.AppendChar(':');
                    String value = entry.Value;
                    if (value != null)
                        writer.WriteString(value);
                    else
                        bytes.AppendBytes(ref writer.@null);
                }
            }
            else {
                // Map<String, Object>
                NativeType elementType = collection.GetElementType(writer.typeCache);
                foreach (DictionaryEntry entry in map) {
                    if (n++ > 0) bytes.AppendChar(',');
                    writer.WriteString((String) entry.Key);
                    bytes.AppendChar(':');
                    Object value = entry.Value;
                    if (value != null)
                        writer.WriteJson(value, elementType);
                    else
                        bytes.AppendBytes(ref writer.@null);
                }
            }
            bytes.AppendChar('}');

        }
        
        public Object Read(JsonReader reader, object obj, NativeType nativeType) {
            PropCollection collection = (PropCollection) nativeType;
            if (obj == null)
                obj = collection.CreateInstance();
            IDictionary map = (IDictionary) obj;
            ref var parser = ref reader.parser;
            NativeType elementType = collection.GetElementType(reader.typeCache);
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueNull:
                        String key = parser.key.ToString();
                        map[key] = null;
                        break;
                    case JsonEvent.ObjectStart:
                        key = parser.key.ToString();
                        Object value = reader.ReadJson(null, elementType, 0);
                        if (value == null)
                            return null;
                        map[key] = value;
                        break;
                    case JsonEvent.ValueString:
                        key = parser.key.ToString();
                        if (collection.id != SimpleType.Id.String)
                            return reader.ErrorNull("Expect Dictionary value type string. Found: ",
                                collection.elementType.Name);
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
                    case JsonEvent.ObjectEnd:
                        return map;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }

    public class ListCodec : IJsonCodec
    {
        public static readonly ListCodec Resolver = new ListCodec();
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            IList list = (IList) obj;
            PropCollection collection = (PropCollection) nativeType;
            writer.bytes.AppendChar('[');
            NativeType elementType = collection.GetElementType(writer.typeCache);
            for (int n = 0; n < list.Count; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                Object item = list[n];
                if (item != null) {
                    switch (collection.id) {
                        case SimpleType.Id.Object:
                            writer.WriteJson(item, elementType);
                            break;
                        case SimpleType.Id.String:
                            writer.WriteString((String) item);
                            break;
                        default:
                            throw new FrifloException("List element type not supported: " + collection.elementType.Name);
                    }
                }
                else
                    writer.bytes.AppendBytes(ref writer.@null);
            }
            writer.bytes.AppendChar(']');
        }
        

        
        public Object Read(JsonReader reader, object col, NativeType nativeType) {
            PropCollection collection = (PropCollection) nativeType;
            IList list = (IList) col;
            if (list == null)
                list = (IList) collection.CreateInstance();
            NativeType elementType = collection.GetElementType(reader.typeCache);
            if (collection.id != SimpleType.Id.Object)
                list.Clear();
            int startLen = list.Count;
            int index = 0;
            while (true) {
                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        list.Add(reader.parser.value.ToString());
                        break;
                    case JsonEvent.ValueNumber:
                        object num = reader.NumberFromValue(collection.id, out bool success);
                        if (!success)
                            return null;
                        list.Add(num);
                        break;
                    case JsonEvent.ValueBool:
                        object bln = reader.BoolFromValue(collection.id, out bool boolSuccess);
                        if (!boolSuccess)
                            return null;
                        list.Add(bln);
                        break;
                    case JsonEvent.ValueNull:
                        if (index < startLen)
                            list[index] = null;
                        else
                            list.Add(null);
                        index++;
                        break;
                    case JsonEvent.ArrayStart:
                        NativeType subElementType = collection.GetElementType(reader.typeCache);
                        if (index < startLen) {
                            Object oldElement = list[index];
                            Object element = reader.ReadJson(oldElement, subElementType, 0);
                            if (element == null)
                                return null;
                            list[index] = element;
                        }
                        else {
                            Object element = reader.ReadJson(null, subElementType, 0);
                            if (element == null)
                                return null;
                            list.Add(element);
                        }

                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            Object oldElement = list[index];
                            Object element = reader.ReadJson(oldElement, elementType, 0);
                            if (element == null)
                                return null;
                            list[index] = element;
                        }
                        else {
                            Object element = reader.ReadJson(null, elementType, 0);
                            if (element == null)
                                return null;
                            list.Add(element);
                        }

                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        for (int n = startLen - 1; n >= index; n--)
                            list.Remove(n);
                        return list;
                    case JsonEvent.Error:
                        return null;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }

}
