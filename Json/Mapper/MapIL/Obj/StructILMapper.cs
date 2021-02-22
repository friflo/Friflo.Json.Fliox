// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Map.Utils;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.MapIL.Obj
{
    // only used for IL
    public class StructILMapper<T> : ClassILMapper<T>
    {


        public StructILMapper(StoreConfig config, Type type, ConstructorInfo constructor, bool isValueType) :
            base(config, type, constructor, isValueType)
        {
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout = new ClassLayout<T>(this, typeStore.config);
        }
        
        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            // write JSON value: null, if it is a Nullable<struct>
            if (isNullable && !mirror.LoadPrimitiveHasValue(primPos)) {
                writer.AppendNull();
                return;
            }
            int startLevel = writer.IncLevel();
            
            PropField[] fields = propFields.fields;
            bool firstMember = true;

            for (int n = 0; n < fields.Length; n++) {
                PropField field = fields[n];
                writer.WriteMemberKey(field, ref firstMember);
                
                field.fieldType.WriteValueIL(ref writer, mirror, primPos + field.primIndex, objPos + field.objIndex);
                writer.FlushFilledBuffer();
            }
            writer.WriteObjectEnd(firstMember);
            writer.DecLevel(startLevel);
        }
        
        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            reader.parser.NextEvent();
            if (isNullable)
                mirror.StoreStructNonNull(primPos);
            return ReadClassMirror(ref reader, mirror, this, primPos, objPos);
        }
    }
}

#endif
