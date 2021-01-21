// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj
{
    public class ClassMapper : IJsonMapper {
        public static readonly ClassMapper Interface = new ClassMapper();
        
        public string DataTypeName() { return "class"; }

        public StubType CreateStubType(Type type) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            if (StubType.IsGenericType(type)) // dont handle generic types like List<> or Dictionary<,>
                return null;
            if (EnumType.IsEnum(type, out bool _))
                return null;
            
            ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
            if (type.IsClass)
                return new ClassType(type, this, constructor);
            if (type.IsValueType)
                return new ClassType(type, this, constructor);
            return null;
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            ref var bytes = ref writer.bytes;
            object obj = slot.Obj;
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
            Var elemVar = new Var();
            for (int n = 0; n < fields.Length; n++) {
                if (firstMember)
                    firstMember = false;
                else
                    bytes.AppendChar(',');
                
                PropField field = fields[n];
                field.GetField(obj, ref elemVar);
                writer.WriteKey(field);
                if (field.FieldType.varType == VarType.Object && elemVar.Obj == null) {
                    bytes.AppendBytes(ref writer.@null);
                } else {
                    StubType fieldType = field.FieldType;
                    fieldType.map.Write(writer, ref elemVar, fieldType);
                }
            }
            bytes.AppendChar('}');
        }
            
        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            // Ensure preconditions are fulfilled
            if (!ObjectUtils.StartObject(reader, ref slot, stubType, out bool success))
                return success;
                
            ref var parser = ref reader.parser;
            object obj = slot.Obj;
            ClassType classType = (ClassType) stubType;
            JsonEvent ev = parser.NextEvent();
            if (obj == null) {
                // Is first member is discriminator - "$type": "<typeName>" ?
                if (ev == JsonEvent.ValueString && reader.discriminator.IsEqualBytes(ref parser.key)) {
                    classType = (ClassType) reader.typeCache.GetTypeByName(ref parser.value);
                    if (classType == null)
                        return JsonReader.ErrorMsg(reader, "Object with discriminator $type not found: ", ref parser.value);
                    ev = parser.NextEvent();
                }
                obj = classType.CreateInstance();
            }
            Var elemVar = new Var();

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                        PropField field = classType.GetField(ref parser.key);
                        if (field == null) {
                            if (!reader.discriminator.IsEqualBytes(ref parser.key)) // dont count discriminators
                                parser.SkipEvent();
                            break;
                        }
                        StubType valueType = field.FieldType;
                        if (valueType.expectedEvent != JsonEvent.ValueString)
                            return JsonReader.ErrorIncompatible(reader, "class field: ", field.name, valueType, ref parser);
                        
                        elemVar.Clear();
                        if (!valueType.map.Read(reader, ref elemVar, valueType))
                            return false;
                        field.SetField(obj, ref elemVar); // set also to null in error case
                        break;
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        // todo: check in EncodeJsonToComplex, why listObj[0].i64 & subType.i64 are skipped
                        if ((field = GetField(reader, classType)) == null)
                            break;
                        valueType = field.FieldType;
                        if (valueType.expectedEvent != ev)
                            return JsonReader.ErrorIncompatible(reader, "class field: ", field.name, valueType, ref parser);
                        
                        elemVar.Clear();
                        if (!valueType.map.Read(reader, ref elemVar, valueType))
                            return false;
                        field.SetField(obj, ref elemVar); // set also to null in error case
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = GetField(reader, classType)) == null)
                            break;
                        if (!field.FieldType.isNullable)
                            return JsonReader.ErrorIncompatible(reader, "class field: ", field.name, field.FieldType, ref parser);
                        elemVar.Obj = null;
                        field.SetField(obj, ref elemVar);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        if ((field = GetField(reader, classType)) == null)
                            break;
                        field.GetField(obj, ref elemVar);
                        if (elemVar.VarType != VarType.Object)
                            return JsonReader.ErrorMsg(reader, "Expect field of type object. Type: ", field.FieldType.type.ToString());
                        object sub = elemVar.Obj;
                        StubType fieldType = field.FieldType;
                        if (!fieldType.map.Read(reader, ref elemVar, fieldType))
                            return false;
                        //
                        object subRet = elemVar.Obj;
                        if (!field.FieldType.isNullable && subRet == null)
                            return JsonReader.ErrorIncompatible(reader, "class field: ", field.name, field.FieldType, ref parser);
                        if (sub != subRet)
                            field.SetField(obj, ref elemVar);
                        break;
                    case JsonEvent.ObjectEnd:
                        slot.Obj = obj;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return JsonReader.ErrorMsg(reader, "unexpected state: ", ev);
                }
                ev = parser.NextEvent();
            }
        }

        private static PropField GetField(JsonReader reader, ClassType classType) {
            PropField field = classType.GetField(ref reader.parser.key);
            if (field != null)
                return field;
            reader.parser.SkipEvent();
            return null;
        }
    }
}