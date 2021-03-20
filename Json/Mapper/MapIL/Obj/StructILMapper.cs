// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Reflect;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.MapIL.Obj
{
    // only used for IL
    internal class StructILMapper<T> : ClassILMapper<T>
    {
        public StructILMapper(StoreConfig config, Type type, ConstructorInfo constructor, InstanceFactory instanceFactory, bool isValueType) :
            base(config, type, constructor, instanceFactory, isValueType)
        {
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout = new ClassLayout<T>(this, typeStore.config);
        }
        
        public override bool IsValueNullIL(ClassMirror mirror, int primPos, int objPos) {
            return isNullable && !mirror.LoadPrimitiveHasValue(primPos);
        }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
#if DEBUG
            if (isNullable && !mirror.LoadPrimitiveHasValue(primPos))
                throw new InvalidOperationException("Expect non null struct. Type: " + typeof(T));
#endif
            int startLevel = writer.IncLevel();
            
            PropField[] fields = propFields.fields;
            bool firstMember = true;

            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                if (field.fieldType.IsValueNullIL(mirror, primPos + field.primIndex, objPos + field.objIndex)) {
                    if (writer.writeNullMembers) {
                        writer.WriteFieldKey(field, ref firstMember);
                        writer.AppendNull();
                    }
                } else {
                    writer.WriteFieldKey(field, ref firstMember);
                    field.fieldType.WriteValueIL(ref writer, mirror, primPos + field.primIndex, objPos + field.objIndex);
                    writer.FlushFilledBuffer();
                }
            }
            writer.WriteObjectEnd(firstMember);
            writer.DecLevel(startLevel);
        }
        
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            if (reader.parser.Event == JsonEvent.ValueNull) {
                if (!isNullable)
                    return false;
                mirror.StorePrimitiveNull(primPos);
                return true;
            }
            reader.parser.NextEvent();
            if (isNullable)
                mirror.StoreStructNonNull(primPos);
            return ReadClassMirror(ref reader, mirror, this, primPos, objPos);
        }
    }
}

#endif
