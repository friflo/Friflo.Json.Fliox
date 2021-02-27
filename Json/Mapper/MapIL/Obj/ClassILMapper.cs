// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj;
using Friflo.Json.Mapper.Map.Obj.Reflect;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.MapIL.Obj
{
   
    internal class ClassILMapper<T> : ClassMapper<T> {
        
        public ClassILMapper (StoreConfig config, Type type, ConstructorInfo constructor, InstanceFactory instanceFactory, bool isValueType) :
            base (config, type, constructor, instanceFactory, isValueType)
        {
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout = new ClassLayout<T>(this, typeStore.config);
        }
        
        public override bool IsValueNullIL(ClassMirror mirror, int primPos, int objPos) {
            return mirror.LoadObj(objPos) == null;
        }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            object obj = mirror.LoadObj(objPos);
#if DEBUG
            if (obj == null)
                throw new InvalidOperationException("Expect non null object. Type: " + typeof(T));
#endif
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
            int startLevel = writer.IncLevel();
            T obj = slot;
            TypeMapper classMapper = this;
            bool firstMember = true;
            
            ClassMirror mirror = writer.InstanceLoad(this, ref classMapper, ref obj);

            if (this != classMapper)
                writer.WriteDiscriminator(this, classMapper, ref firstMember);
            
            PropField[] fields = classMapper.propFields.fields;
            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];

                // check for JSON value: null is done in WriteValueIL() struct's requires different handling than reference types
                if (field.fieldType.isValueType) {
                    if (field.fieldType.IsValueNullIL(mirror, field.primIndex, field.objIndex)) {
                        if (writer.writeNullMembers) {
                            writer.WriteFieldKey(field, ref firstMember);
                            writer.AppendNull();  
                        }
                    } else {
                        writer.WriteFieldKey(field, ref firstMember);
                        field.fieldType.WriteValueIL(ref writer, mirror, field.primIndex, field.objIndex);
                    }
                } else {
                    object fieldObj = mirror.LoadObj(field.objIndex);
                    if (fieldObj == null) {
                        if (writer.writeNullMembers) {
                            writer.WriteFieldKey(field, ref firstMember);
                            writer.AppendNull();
                        }
                    } else {
                        writer.WriteFieldKey(field, ref firstMember);
                        field.fieldType.WriteObject(ref writer, fieldObj);
                    }
                    writer.FlushFilledBuffer();
                }
            }
            writer.InstancePop();
            writer.WriteObjectEnd(firstMember);

            writer.DecLevel(startLevel);
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!reader.StartObject(this, out success))
                return default;

            T obj = slot;
            TypeMapper classType = this;
            classType = GetPolymorphType(ref reader, classType, ref obj, out success);
            if (!success)
                return default;
            
            ClassMirror mirror = reader.InstanceLoad(this, ref classType, ref obj);
            if (!ReadClassMirror(ref reader, mirror, classType, 0, 0))
                return default;
            reader.InstanceStore(mirror, ref obj);
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
                        if ((field = reader.GetField32(propFields)) == null)
                            break;
                        TypeMapper fieldType = field.fieldType;
                        if (fieldType.isValueType) {
                            if (!fieldType.ReadValueIL(ref reader, mirror, primPos + field.primIndex, objPos + field.objIndex))
                                return default;
                        } else {
                            object fieldVal = mirror.LoadObj(objPos + field.objIndex);
                            fieldVal = fieldType.ReadObject(ref reader, fieldVal, out success);
                            if (!success)
                                return false;
                            mirror.StoreObj(objPos + field.objIndex, fieldVal);
                            if (!fieldType.isNullable && fieldVal == null)
                                return reader.ErrorIncompatible<bool>(classType, field, out success);
                        }
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = reader.GetField32(propFields)) == null)
                            break;
                        if (!field.fieldType.isNullable)
                            return reader.ErrorIncompatible<bool>(classType, field, out success);
                        
                        if (field.fieldType.isValueType)
                            mirror.StorePrimitiveNull(primPos + field.primIndex);
                        else
                            mirror.StoreObj(objPos + field.objIndex, null);
                        break;
                    case JsonEvent.ObjectEnd:
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return reader.ErrorMsg<bool>("unexpected state: ", ev, out success);
                }
                ev = reader.parser.NextEvent();
            }
        }

    }
}

#endif
