// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;
using Friflo.Json.Mapper.Map.Utils;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
   
    [CLSCompliant(true)]
    public class ClassILMapper<T> : ClassMapper<T> {

        private     ClassLayout<T>                    layout;    // todo readonly

        public override ClassLayout GetClassLayout() { return layout; }
        
        public ClassILMapper (Type type, ConstructorInfo constructor, bool isValueType) :
            base (type, constructor, isValueType)
        {
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout = new ClassLayout<T>(propFields);
            layout.InitClassLayout(propFields, typeStore.typeResolver.GetConfig());
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
            ref var bytes = ref writer.bytes;
            T obj = slot;
            TypeMapper classMapper = this;
            bool firstMember = true;
            bytes.AppendChar('{');

            ClassMirror mirror = writer.InstanceLoad(classMapper, obj);
            PropField[] fields = classMapper.GetPropFields().fields;
            for (int n = 0; n < fields.Length; n++) {
                if (firstMember)
                    firstMember = false;
                else
                    bytes.AppendChar(',');
                PropField field = fields[n];
                WriteUtils.WriteKey(writer, field);
                // check for JSON value: null is done in WriteValueIL() struct's requires different handling than reference types
                if (field.fieldType.isValueType) {
                    field.fieldType.WriteValueIL(writer, mirror, field.primIndex, field.objIndex);
                } else {
                    object fieldObj = mirror.LoadObj(field.objIndex);
                    field.fieldType.WriteObject(writer, fieldObj);
                }
            }
            writer.InstancePop();
            bytes.AppendChar('}');
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
            
            ClassMirror mirror = reader.InstanceLoad(classType, obj);
            if (!ReadClassMirror(reader, mirror, classType, 0, 0))
                return default;
            reader.InstanceStore(mirror, obj);
            return obj;
        }

        internal static bool ReadClassMirror(JsonReader reader, ClassMirror mirror, TypeMapper classType, int primPos, int objPos) {
            ref var parser = ref reader.parser;
            JsonEvent ev = parser.Event;

            while (true) {
                bool success;
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        PropField field;
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        TypeMapper fieldType = field.fieldType;
                        if (fieldType.isValueType) {
                            if (!fieldType.ReadValueIL(reader, mirror, primPos + field.primIndex, objPos + field.objIndex))
                                return default;
                        } else {
                            object sub = mirror.LoadObj(field.objIndex);
                            sub = fieldType.ReadObject(reader, sub, out success);
                            if (!success)
                                return false;
                            mirror.StoreObj(field.objIndex, sub);
                            if (!fieldType.isNullable && sub == null) {
                                ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, fieldType, ref parser, out success);
                                return false;
                            }
                        }
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        if (!field.fieldType.isNullable) {
                            ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, field.fieldType, ref parser, out success);
                            return default;
                        }
                        if (field.fieldType.isValueType)
                            mirror.StorePrimitiveNull(field.primIndex);
                        else
                            mirror.StoreObj(field.objIndex, null);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        fieldType = field.fieldType;
                        if (fieldType.isValueType) {
                            if (!fieldType.ReadValueIL(reader, mirror, primPos + field.primIndex, objPos + field.objIndex))
                                return false;
                        } else {
                            object sub = mirror.LoadObj(field.objIndex);
                            object subRet = fieldType.ReadObject(reader, sub, out success);
                            if (!success)
                                return false;
                            if (!fieldType.isNullable && subRet == null) {
                                ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, fieldType, ref parser, out success);
                                return false;
                            }
                            mirror.StoreObj(field.objIndex, subRet);
                        }
                        break;
                    case JsonEvent.ObjectEnd:
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

#endif
