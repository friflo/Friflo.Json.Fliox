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
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            object obj = mirror.LoadObj(objPos);
            if (obj == null)
                WriteUtils.AppendNull(ref writer);
            else
                Write(ref writer, (T) obj);
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            T src = (T) mirror.LoadObj(objPos);
            T value = Read(ref reader, src, out bool success);
            mirror.StoreObj(objPos, value);
            return success;
        }
        
        // ----------------------------------- Write / Read -----------------------------------
        public override void Write(ref Writer writer, T slot) {
            int startLevel = WriteUtils.IncLevel(ref writer);
            T obj = slot;
            TypeMapper classMapper = this;
            bool firstMember = true;
            
            ClassMirror mirror = writer.InstanceLoad(ref classMapper, obj);

            if (this != classMapper) {
                WriteUtils.WriteDiscriminator(ref writer, classMapper);
                firstMember = false;
            }
            PropField[] fields = classMapper.propFields.fields;
            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                WriteUtils.WriteMemberKey(ref writer, field, ref firstMember);

                // check for JSON value: null is done in WriteValueIL() struct's requires different handling than reference types
                if (field.fieldType.isValueType) {
                    field.fieldType.WriteValueIL(ref writer, mirror, field.primIndex, field.objIndex);
                } else {
                    object fieldObj = mirror.LoadObj(field.objIndex);
                    field.fieldType.WriteObject(ref writer, fieldObj);
                }
            }
            writer.InstancePop();
            WriteUtils.WriteObjectEnd(ref writer, firstMember);

            WriteUtils.DecLevel(ref writer, startLevel);
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!ObjectUtils.StartObject(ref reader, this, out success))
                return default;

            T obj = slot;
            TypeMapper classType = this;
            classType = GetPolymorphType(ref reader, classType, ref obj, out success);
            if (!success)
                return default;
            
            ClassMirror mirror = reader.InstanceLoad(ref classType, obj);
            if (!ReadClassMirror(ref reader, mirror, classType, 0, 0))
                return default;
            reader.InstanceStore(mirror, obj);
            return obj;
        }

        internal static bool ReadClassMirror(ref Reader reader, ClassMirror mirror, TypeMapper classType, int primPos, int objPos) {
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
                        if ((field = ObjectUtils.GetField32(ref reader, propFields)) == null)
                            break;
                        TypeMapper fieldType = field.fieldType;
                        if (fieldType.isValueType) {
                            if (!fieldType.ReadValueIL(ref reader, mirror, primPos + field.primIndex, objPos + field.objIndex))
                                return default;
                        } else {
                            object fieldVal = mirror.LoadObj(field.objIndex);
                            fieldVal = fieldType.ReadObject(ref reader, fieldVal, out success);
                            if (!success)
                                return false;
                            mirror.StoreObj(field.objIndex, fieldVal);
                            if (!fieldType.isNullable && fieldVal == null)
                                return ObjectUtils.ErrorIncompatible<bool>(ref reader, classType, field, out success);
                        }
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = ObjectUtils.GetField32(ref reader, propFields)) == null)
                            break;
                        if (!field.fieldType.isNullable)
                            return ObjectUtils.ErrorIncompatible<bool>(ref reader, classType, field, out success);
                        
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
                        return ReadUtils.ErrorMsg<bool>(ref reader, "unexpected state: ", ev, out success);
                }
                ev = reader.parser.NextEvent();
            }
        }

    }
}

#endif
