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
            TypeMapper classMapper = this;
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
            reader.parser.NextEvent();
            return ClassILMapper<T>.ReadClassMirror(reader, mirror, this, primPos, objPos);
        }
    }
}

#endif
