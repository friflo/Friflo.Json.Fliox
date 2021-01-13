// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public class ObjectCodec : IJsonCodec {
        public static readonly ObjectCodec Resolver = new ObjectCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type.IsPrimitive)
                return null;
            if (type.IsClass)
                return new PropType(resolver, type, this);
            if (type.IsValueType)
                return new PropType(resolver, type, this);
            return null;
        }
        
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
                writer.typeCache.AppendDiscriminator(ref bytes, type);
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
                    case SimpleType.Id.Float:
                        writer.WriteKey(field);
                        format.AppendFlt(ref bytes, field.GetFloat(obj));
                        break;
                    case SimpleType.Id.Object:
                        writer.WriteKey(field);
                        Object child = field.GetObject(obj);
                        if (child == null) {
                            bytes.AppendBytes(ref writer.@null);
                        } else {
                            NativeType fieldObject = field.GetFieldObject(writer.typeCache);
                            writer.WriteJson(child, fieldObject);
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
                if (ev == JsonEvent.ValueString && reader.discriminator.IsEqualBytes(ref parser.key)) {
                    propType = (PropType) reader.typeCache.GetTypeByName(ref parser.value);
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
                                throw new InvalidOperationException("Field is not compatible to JSON object: " + field.fieldType.FullName);

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
}