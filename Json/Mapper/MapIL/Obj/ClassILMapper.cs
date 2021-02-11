// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Map.Utils;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.MapIL.Obj
{
   
    [CLSCompliant(true)]
    public class ClassILMapper<T> : ClassMapper<T> {
        
        public ClassILMapper (StoreConfig config, Type type, ConstructorInfo constructor, bool isValueType) :
            base (config, type, constructor, isValueType)
        {
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout = new ClassLayout<T>(this, typeStore.config);
        }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            object obj = mirror.LoadObj(objPos);
            if (obj == null)
                WriteUtils.AppendNull(writer);
            else
                Write(writer, (T) obj);
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            T src = (T) mirror.LoadObj(objPos);
            T value = Read(reader, src, out bool success);
            mirror.StoreObj(objPos, value);
            return success;
        }
        
        // ----------------------------------- Write / Read -----------------------------------
        public override void Write(JsonWriter writer, T slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            T obj = slot;
            TypeMapper classMapper = this;
            bool firstMember = true;
            
            ClassMirror mirror = writer.InstanceLoad(ref classMapper, obj);

            if (this != classMapper) {
                WriteUtils.WriteDiscriminator(writer, classMapper);
                firstMember = false;
            }
            PropField[] fields = classMapper.propFields.fields;
            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                WriteUtils.WriteMemberKey(writer, field, ref firstMember);

                // check for JSON value: null is done in WriteValueIL() struct's requires different handling than reference types
                if (field.fieldType.isValueType) {
                    field.fieldType.WriteValueIL(writer, mirror, field.primIndex, field.objIndex);
                } else {
                    object fieldObj = mirror.LoadObj(field.objIndex);
                    field.fieldType.WriteObject(writer, fieldObj);
                }
            }
            writer.InstancePop();
            WriteUtils.WriteObjectEnd(writer, firstMember);

            WriteUtils.DecLevel(writer, startLevel);
        }

        public override T Read(JsonReader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!ObjectUtils.StartObject(reader, this, out success))
                return default;

            T obj = slot;
            TypeMapper classType = this;
            classType = GetPolymorphType(reader, classType, ref obj, out success);
            if (!success)
                return default;
            
            ClassMirror mirror = reader.intern.InstanceLoad(ref classType, obj);
            if (!ReadClassMirror(reader, mirror, classType, 0, 0))
                return default;
            reader.intern.InstanceStore(mirror, obj);
            return obj;
        }

        internal static bool ReadClassMirror(JsonReader reader, ClassMirror mirror, TypeMapper classType, int primPos, int objPos) {
            JsonEvent ev = reader.parser.Event;
            var propFields = classType.propFields;

            while (true) {
                bool success;
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        PropField field;
                        if ((field = ObjectUtils.GetField32(reader, propFields)) == null)
                            break;
                        TypeMapper fieldType = field.fieldType;
                        if (fieldType.isValueType) {
                            if (!fieldType.ReadValueIL(reader, mirror, primPos + field.primIndex, objPos + field.objIndex))
                                return default;
                        } else {
                            object fieldVal = mirror.LoadObj(field.objIndex);
                            fieldVal = fieldType.ReadObject(reader, fieldVal, out success);
                            if (!success)
                                return false;
                            mirror.StoreObj(field.objIndex, fieldVal);
                            if (!fieldType.isNullable && fieldVal == null)
                                return ObjectUtils.ErrorIncompatible<bool>(reader, classType, field, out success);
                        }
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = ObjectUtils.GetField32(reader, propFields)) == null)
                            break;
                        if (!field.fieldType.isNullable)
                            return ObjectUtils.ErrorIncompatible<bool>(reader, classType, field, out success);
                        
                        if (field.fieldType.isValueType)
                            mirror.StorePrimitiveNull(field.primIndex);
                        else
                            mirror.StoreObj(field.objIndex, null);
                        break;
                    case JsonEvent.ObjectEnd:
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ReadUtils.ErrorMsg<bool>(reader, "unexpected state: ", ev, out success);
                }
                ev = reader.parser.NextEvent();
            }
        }

    }
}

#endif
