// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;
using Friflo.Json.Mapper.Map.Utils;

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    // only used for IL
    public class StructILMapper<T> : ClassMapper<T>
    {
        public StructILMapper(Type type, ConstructorInfo constructor) :
            base(type, constructor)
        {
        
        }
        
        public override void WriteFieldIL(JsonWriter writer, ClassMirror mirror, PropField structField, int primPos, int objPos) {
            int startLevel = WriteUtils.IncLevel(writer);
            ref var bytes = ref writer.bytes;
            TypeMapper classMapper = structField.fieldType;
            PropField[] fields = classMapper.GetPropFields().fieldsSerializable;
            bool firstMember = true;
            bytes.AppendChar('{');

            for (int n = 0; n < fields.Length; n++) {
                if (firstMember)
                    firstMember = false;
                else
                    bytes.AppendChar(',');
                PropField field = fields[n];
                WriteUtils.WriteKey(writer, field);
                
                field.fieldType.WriteFieldIL(writer, mirror, field, primPos + structField.primIndex, objPos + structField.objIndex);
            }
            bytes.AppendChar('}');
            WriteUtils.DecLevel(writer, startLevel);
        }
        
        public override bool ReadFieldIL(JsonReader reader, ClassMirror mirror, PropField structField, int primPos, int objPos) {
            if (this != structField.fieldType)
                throw new InvalidOperationException("expect this == structField.fieldType");
            // Ensure preconditions are fulfilled
            if (!ObjectUtils.StartObject(reader, this, out bool success))
                return success;
                
            ref var parser = ref reader.parser;
            JsonEvent ev = parser.NextEvent();
 
            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                        PropField field = GetField(ref parser.key);
                        if (field == null) {
                            if (!reader.discriminator.IsEqualBytes(ref parser.key)) // dont count discriminators
                                parser.SkipEvent();
                            break;
                        }
                        var fieldType = field.fieldType;
                        if (!fieldType.ReadFieldIL(reader, mirror, field, primPos, objPos))
                            return default;
                        break;
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        if ((field = ObjectUtils.GetField(reader, this)) == null)
                            break;
                        fieldType = field.fieldType;
                        if (!fieldType.ReadFieldIL(reader, mirror, field, primPos, objPos))
                            return default;
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = ObjectUtils.GetField(reader, this)) == null)
                            break;
                        if (!field.fieldType.isNullable) {
                            ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, field.fieldType, ref parser, out success);
                            return default;
                        }
                        // field.SetField(obj, null);
                        throw new NotImplementedException();
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        if ((field = ObjectUtils.GetField(reader, this)) == null)
                            break;
                        fieldType = field.fieldType;

                        if (!fieldType.ReadFieldIL(reader, mirror, field, primPos, objPos))
                            return default;
                        object subRet = mirror.LoadObj(field.objIndex);
                        if (!fieldType.isNullable && subRet == null) {
                            ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, fieldType, ref parser, out success);
                            return default;
                        }
                        break;
                    case JsonEvent.ObjectEnd:
                        // reader.InstanceStore(mirror, obj);
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ReadUtils.ErrorMsg<bool>(reader, "unexpected state: ", ev, out success);
                }
                ev = parser.NextEvent();
            }
        }
    }
}