// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public class ObjectCodec : IJsonCodec {
        public static readonly ObjectCodec Interface = new ObjectCodec();

        public StubType CreateStubType(Type type) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            if (StubType.IsGenericType(type)) // dont handle generic types like List<> or Dictionary<,>
                return null;
            
            ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
            if (type.IsClass)
                return new ClassType(type, this, constructor);
            if (type.IsValueType)
                return new ClassType(type, this, constructor);
            return null;
        }
        
        public void Write (JsonWriter writer, object obj, StubType stubType) {

            ref var bytes = ref writer.bytes;
            ref var format = ref writer.format;
            
            ClassType type = (ClassType)stubType;
            bool firstMember = true;
            bytes.AppendChar('{');
            Type objType = obj.GetType();
            if (type.type != objType) {
                type = (ClassType)writer.typeCache.GetType(objType);
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
                            StubType fieldObject = field.FieldType;
                            fieldObject.codec.Write(writer, child, fieldObject);
                        }
                        break;
                    default:
                        throw new FrifloException("invalid field type: " + field.type);
                }
            }
            bytes.AppendChar('}');
        }
            
        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            ref var parser = ref reader.parser;
            // Ensure preconditions are fulfilled
            switch (parser.Event) {
                case JsonEvent.ValueNull:
                    if (stubType.isNullable)
                        return false;
                    return reader.ErrorIncompatible("Type", stubType, ref parser);
                case JsonEvent.ObjectStart:
                    break;
                default:
                    return reader.ErrorNull("Expect ObjectStart but found", parser.Event);
            }
            
            object obj = slot.Obj;
            ClassType classType = (ClassType) stubType;
            JsonEvent ev = parser.NextEvent();
            if (obj == null) {
                // Is first member is discriminator - "$type": "<typeName>" ?
                if (ev == JsonEvent.ValueString && reader.discriminator.IsEqualBytes(ref parser.key)) {
                    classType = (ClassType) reader.typeCache.GetTypeByName(ref parser.value);
                    if (classType == null)
                        return reader.ErrorNull("Object with discriminator $type not found: ", ref parser.value);
                    ev = parser.NextEvent();
                }
                obj = classType.CreateInstance();
            }
            Slot elemSlot = new Slot();

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                        PropField field = classType.GetField(parser.key);
                        if (field == null) {
                            if (!reader.discriminator.IsEqualBytes(ref parser.key)) // dont count discriminators
                                parser.SkipEvent();
                            break;
                        }
                        StubType valueType = field.FieldType;
                        if (valueType.typeCat != TypeCat.String)
                            return reader.ErrorIncompatible("class field: " + field.name, valueType, ref parser);
                        elemSlot.Clear();
                        if (!valueType.codec.Read(reader, ref elemSlot, valueType))
                            return false;
                        field.SetObject(obj, elemSlot.Get()); // set also to null in error case
                        break;
                    case JsonEvent.ValueNumber:
                        field = classType.GetField(parser.key);
                        if (field == null) {
                            parser.SkipEvent(); // todo: check in EncodeJsonToComplex, why listObj[0].i64 & subType.i64 are skipped
                            break;
                        }
                        valueType = field.FieldType;
                        if (valueType.typeCat != TypeCat.Number)
                            return reader.ErrorIncompatible("class field: " + field.name, valueType, ref parser);
                        // todo room for improvement - in case of primitives codec.Read() should not be called.
                        elemSlot.Clear();
                        if (!valueType.codec.Read(reader, ref elemSlot, valueType))
                            return false;
                        field.SetObject(obj, elemSlot.Get()); // set also to null in error case
                        break;
                    case JsonEvent.ValueBool:
                        field = classType.GetField(parser.key);
                        if (field == null) {
                            parser.SkipEvent();
                            break;
                        }
                        valueType = field.FieldType;
                        if (valueType.typeCat != TypeCat.Bool)
                            return reader.ErrorIncompatible("class field: " + field.name, valueType, ref parser);
                        field.SetBool(obj, parser.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        field = classType.GetField(parser.key);
                        if (field == null) {
                            parser.SkipEvent(); // count skipping
                            break;
                        }
                        if (!field.FieldType.isNullable)
                            return reader.ErrorIncompatible("class field: " + field.name, field.FieldType, ref parser);
                        field.SetObject(obj, null);
                        break;
                    case JsonEvent.ObjectStart:
                        field = classType.GetField(parser.key);
                        if (field == null) {
                            if (!parser.SkipTree())
                                return false;
                        } else {
                            object sub = field.GetObject(obj);
                            StubType fieldObject = field.FieldType;
                            elemSlot.Clear();
                            if (!fieldObject.codec.Read(reader, ref elemSlot, fieldObject))
                                return false;
                            //
                            object subRet = elemSlot.Obj;
                            if (!field.FieldType.isNullable && subRet == null)
                                return reader.ErrorIncompatible("class field: " + field.name, field.FieldType, ref parser);
                            if (sub != subRet)
                                field.SetObject(obj, subRet);
                        }
                        break;
                    case JsonEvent.ArrayStart:
                        field = classType.GetField(parser.key);
                        if (field == null) {
                            if (!parser.SkipTree())
                                return false;
                        } else {
                            StubType fieldArray = field.FieldType;
                            if (fieldArray == null)
                                return reader.ErrorIncompatible("class field: " + field.name, field.FieldType, ref parser);
                            object array = field.GetObject(obj);
                            elemSlot.Clear();
                            if (!fieldArray.codec.Read(reader, ref elemSlot, fieldArray))
                                return false;
                            //
                            object arrayRet = elemSlot.Obj;
                            if (!field.FieldType.isNullable && arrayRet == null)
                                return reader.ErrorIncompatible("class field: " + field.name, field.FieldType, ref parser);
                            if (array != arrayRet)
                                field.SetObject(obj, arrayRet);
                        }
                        break;
                    case JsonEvent.ObjectEnd:
                        slot.Obj = obj;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
                ev = parser.NextEvent();
            }
        }
    }
}