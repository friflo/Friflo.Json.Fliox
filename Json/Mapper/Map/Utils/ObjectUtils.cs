// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Reflect;

using Friflo.Json.Mapper.MapIL.Obj;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper.Map
{
    public partial struct Reader
    {
        public bool StartObject<T>(TypeMapper<T> mapper, out bool success) {
            var ev = parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (mapper.isNullable) {
                        success = true;
                        return false;
                    }
                    ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out success);
                    success = false;
                    return false;
                case JsonEvent.ObjectStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    ErrorIncompatible<T>(mapper.DataTypeName(), mapper, out success);
                    // reader.ErrorNull("Expect { or null. Got Event: ", ev);
                    return false;
            }
        }

        public T ReadElement<T>(TypeMapper<T> mapper, ref T value, out bool success) {
#if !UNITY_5_3_OR_NEWER
            if (mapper.useIL) {
                TypeMapper typeMapper = mapper;
                ClassMirror mirror = InstanceLoad(mapper, ref typeMapper, ref value);
                success = mapper.ReadValueIL(ref this, mirror, 0, 0);  
                if (!success)
                    return default;
                InstanceStore(mirror, ref value);
                return value;
            }
#endif
            return mapper.Read(ref this, value, out success);
        }
        
        public TVal ErrorIncompatible<TVal>(TypeMapper objectMapper, PropField field, out bool success) {
            ErrorIncompatible<bool>(objectMapper.DataTypeName(), $" field: {field.name}", field.fieldType, out success);
            return default;
        }
      
        public PropField GetField(PropertyFields propFields) {
            PropField field = propFields.GetField(ref this.parser.key);
            if (field != null)
                return field;
            parser.SkipEvent();
            return null;
        }
        
        public PropField GetField32(PropertyFields propFields) {
            searchKey.FromBytes(ref parser.key);
            for (int n = 0; n < propFields.num; n++) {
                if (searchKey.IsEqual(ref propFields.names32[n]))
                    return propFields.fields[n];
            }
            parser.SkipEvent();
            return null;
        }
        
    }
}