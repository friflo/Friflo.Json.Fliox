// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.MapIL.Obj;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class ObjectUtils
    {
        public static bool StartObject<T>(JsonReader reader, TypeMapper<T> mapper, out bool success) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (mapper.isNullable) {
                        success = true;
                        return false;
                    }
                    ReadUtils.ErrorIncompatible<T>(reader, mapper.DataTypeName(), mapper, out success);
                    success = false;
                    return false;
                case JsonEvent.ObjectStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    ReadUtils.ErrorIncompatible<T>(reader, mapper.DataTypeName(), mapper, out success);
                    // reader.ErrorNull("Expect { or null. Got Event: ", ev);
                    return false;
            }
        }

        public static T Read<T>(JsonReader reader, TypeMapper<T> mapper, ref T value, out bool success) {
#if !UNITY_5_3_OR_NEWER
            if (mapper.useIL) {
                TypeMapper typeMapper = mapper;
                ClassMirror mirror = reader.InstanceLoad(ref typeMapper, value);
                success = mapper.ReadValueIL(reader, mirror, 0, 0);  
                if (!success)
                    return default;
                reader.InstanceStore(mirror, value);
                return value;
            }
#endif
            return mapper.Read(reader, value, out success);
        }
        
        public static void Write<T>(JsonWriter writer, TypeMapper<T> mapper, ref T value) {
#if !UNITY_5_3_OR_NEWER
            if (mapper.useIL) {
                TypeMapper typeMapper = mapper;
                ClassMirror mirror = writer.InstanceLoad(ref typeMapper, value);
                mapper.WriteValueIL(writer, mirror, 0, 0);
                return;
            }
#endif
            mapper.Write(writer, value);
            
        }

        public static TVal ErrorIncompatible<TVal>(JsonReader reader, TypeMapper objectMapper, PropField field, out bool success) {
            ReadUtils.ErrorIncompatible<bool>(reader, objectMapper.DataTypeName(), $" field: {field.name}", field.fieldType, out success);
            return default;
        }
      
        public static PropField GetField(JsonReader reader, PropertyFields propFields) {
            PropField field = propFields.GetField(ref reader.parser.key);
            if (field != null)
                return field;
            reader.parser.SkipEvent();
            return null;
        }
        
        public static PropField GetField32(JsonReader reader, PropertyFields propFields) {
            reader.intern.searchKey.FromBytes(ref reader.parser.key);
            for (int n = 0; n < propFields.num; n++) {
                if (reader.intern.searchKey.IsEqual(ref propFields.names32[n]))
                    return propFields.fields[n];
            }
            reader.parser.SkipEvent();
            return null;
        }
        
    }
}