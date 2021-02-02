// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Map.Utils;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.MapIL.Obj
{
    // only used for IL
    public class StructILMapper<T> : ClassILMapper<T>
    {
        private     ClassLayout<T>                    layout;

        public override ClassLayout GetClassLayout() { return layout; }

        public StructILMapper(Type type, ConstructorInfo constructor, bool isValueType) :
            base(type, constructor, isValueType)
        {
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout = new ClassLayout<T>(this, typeStore.config);
        }
        
        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            // write JSON value: null, if it is a Nullable<struct>
            if (isNullable && !mirror.LoadPrimitiveHasValue(primPos)) {
                WriteUtils.AppendNull(writer);
                return;
            }
            int startLevel = WriteUtils.IncLevel(writer);
            ref var bytes = ref writer.bytes;
            PropField[] fields = GetPropFields().fields;
            bool firstMember = true;
            bytes.AppendChar('{');

            for (int n = 0; n < fields.Length; n++) {
                if (firstMember)
                    firstMember = false;
                else
                    bytes.AppendChar(',');
                PropField field = fields[n];
                WriteUtils.WriteKey(writer, field);
                
                field.fieldType.WriteValueIL(writer, mirror, primPos + field.primIndex, objPos + field.objIndex);
            }
            bytes.AppendChar('}');
            WriteUtils.DecLevel(writer, startLevel);
        }
        
        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            reader.parser.NextEvent();
            return ClassILMapper<T>.ReadClassMirror(reader, mirror, this, primPos, objPos);
        }
    }
}

#endif
