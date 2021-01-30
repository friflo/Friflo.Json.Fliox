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

        private readonly    ClassLayout<T>                    layout;

        public override ClassLayout GetClassLayout() { return layout; }
        
        public ClassILMapper (Type type, ConstructorInfo constructor) :
            base (type, constructor)
        {
            layout = new ClassLayout<T>(propFields);
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout.InitClassLayout(type, propFields, typeStore.typeResolver.GetConfig());
        }
        
        public override void WriteFieldIL(JsonWriter writer, ClassMirror mirror, PropField field, int primPos, int objPos) {
            if (!isValueType) {
                object obj = mirror.LoadObj(objPos + field.objIndex);
                if (obj == null)
                    WriteUtils.AppendNull(writer);
                else
                    Write(writer, (T) obj);
            } else {
                throw new NotImplementedException();
            }
        }

        public override bool ReadFieldIL(JsonReader reader, ClassMirror mirror, PropField field, int primPos, int objPos) {
            T src = (T) mirror.LoadObj(objPos + field.objIndex);
            T value = Read(reader, src, out bool success);
            mirror.StoreObj(objPos + field.objIndex, value);
            return success;
        }
        
        // ----------------------------------- Write / Read -----------------------------------
    
        public override T Read(JsonReader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!ObjectUtils.StartObject(reader, this, out success))
                return default;
                
            T obj = slot;
            TypeMapper classType = this;
            classType = GetPolymorphType(reader, classType, ref obj, out success);
            if (!success)
                return default;
            
            ref var parser = ref reader.parser;
            JsonEvent ev = parser.Event;

            ClassMirror mirror = reader.InstanceLoad(classType, obj);

            while (true) {
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        PropField field;
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        TypeMapper fieldType = field.fieldType;
                        if (fieldType.isValueType) {
                            if (!fieldType.ReadFieldIL(reader, mirror, field, 0, 0))
                                return default;
                        } else {
                            object subRet = mirror.LoadObj(field.objIndex);
                            if (!fieldType.isNullable && subRet == null) {
                                ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, fieldType, ref parser, out success);
                                return default;
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
                        mirror.StoreObj(field.objIndex, null);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        fieldType = field.fieldType;
                        if (fieldType.isValueType) {
                            if (!fieldType.ReadFieldIL(reader, mirror, field, field.primIndex, field.objIndex))
                                return default;
                        } else {
                            object sub = mirror.LoadObj(field.objIndex);
                            object subRet = fieldType.ReadObject(reader, sub, out success);
                            if (!success)
                                return default;
                            if (!fieldType.isNullable && subRet == null) {
                                ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, fieldType, ref parser, out success);
                                return default;
                            }
                            mirror.StoreObj(field.objIndex, subRet);
                        }
                        break;
                    case JsonEvent.ObjectEnd:
                        reader.InstanceStore(mirror, obj);
                        success = true;
                        return obj;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(reader, "unexpected state: ", ev, out success);
                }
                ev = parser.NextEvent();
            }
        }

    }
}

#endif
